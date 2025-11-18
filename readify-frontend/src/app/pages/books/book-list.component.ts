import { Component, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatSliderModule } from '@angular/material/slider';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BookService } from '../../services/book.service';
import { ProductService } from '../../services/product.service';
import { LoadingSkeletonComponent } from '../../components/loading-skeleton.component';
import { CartService } from '../../services/cart.service';
import { NotificationService } from '../../services/notification.service';
import { WishlistService } from '../../services/wishlist.service';
import { RecommendationService } from '../../services/recommendation.service';
import { Subscription } from 'rxjs';
import { HighlightPipe } from '../../pipes/highlight.pipe';

@Component({
  selector: 'app-book-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatSliderModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    LoadingSkeletonComponent,
    HighlightPipe
  ],
  templateUrl: './book-list.component.html',
  styles: [`
    .filter-card {
      position: sticky;
      top: 16px;
      z-index: 10;
      background: white;
    }
    .product-card {
      cursor: pointer;
      height: 100%;
      display: flex;
      flex-direction: column;
      position: relative;
    }
    .product-card.low-stock {
      border: 2px solid #ff9800; /* highlight */
      box-shadow: 0 4px 8px rgba(255, 152, 0, 0.08);
    }
    .product-card mat-card-content {
      flex: 1;
    }
    .product-image {
      width: 100%;
      height: 250px;
      object-fit: cover;
    }
    .price-tag {
      font-size: 1.5rem;
      font-weight: 500;
      color: var(--primary-color);
      margin: 8px 0;
    }
    .stock-info {
      font-size: 0.875rem;
      margin: 4px 0;
    }
    .stock-low-badge {
      background: #fff3e0;
      color: #ff9800;
      padding: 2px 6px;
      border-radius: 4px;
      font-weight: 600;
      margin-left: 8px;
    }
    .active-filters {
      border-top: 1px solid var(--border-color);
      padding-top: 12px;
    }
    .list-group-item.active {
      background-color: var(--primary-color);
      color: white;
    }
    .wishlist-btn {
      position: absolute;
      top: 8px;
      right: 8px;
      z-index: 20;
    }
    .rating {
      display: flex;
      gap: 2px;
      align-items: center;
      margin-top: 6px;
    }

    /* recommendations row styles (basic) */
    .rec-card { width: 180px; padding: 12px; display:flex; flex-direction:column; align-items:center; gap:8px; flex:0 0 auto; border-radius:8px; box-shadow: 0 6px 18px rgba(0,0,0,0.06); transition: transform .12s ease; }
    .rec-card:hover { transform: translateY(-6px); }
    .rec-image { width: 120px; height: 160px; object-fit:cover; border-radius:6px; }
    .rec-row-wrapper { position: relative; }
    .rec-nav { position: absolute; top: 40%; transform: translateY(-50%); z-index: 30; background: rgba(255,255,255,0.9); border: 1px solid rgba(0,0,0,0.06); width:36px; height:36px; border-radius:50%; display:flex; align-items:center; justify-content:center; cursor:pointer; }
    .rec-nav.left { left: -18px; }
    .rec-nav.right { right: -18px; }
    @media (max-width: 768px) {
      .rec-card { width: 140px; padding:10px; }
      .rec-image { width: 100px; height:140px; }
      .rec-nav.left { left: 4px; }
      .rec-nav.right { right: 4px; }
    }
  `]
})
export class BookListComponent implements OnDestroy, AfterViewInit {
  @ViewChild('recRow', { read: ElementRef }) recRow!: ElementRef<HTMLDivElement>;

  products: any[] = [];
  recommendations: any[] = [];
  trending: any[] = [];
  categories: any[] = [];
  authors: { name: string; count: number }[] = [];
  q = '';
  page = 1;
  pageSize = 12;
  totalPages = 1;
  total = 0;
  selectedCategoryId: number | null = null;
  selectedCategoryIds: number[] = [];
  author: string | null = null;
  minPrice: number = 0;
  maxPrice: number = 100;
  sort: string = '';
  loading = false;
  private priceDebounce: any;
  private _subs: Subscription[] = [];

  wishlist = new Set<number>();

  // new filters
  minRatingFilter: number | null = null; // e.g. 4 => 4+
  inStockOnly = false;

  private autoplayTimer: any = null;
  private isAutoplayPaused = false;
  private readonly autoplayIntervalMs = 3500;

  constructor(private bookService: BookService, private productService: ProductService, private route: ActivatedRoute, private router: Router, private cart: CartService, private notify: NotificationService, private wishlistSvc: WishlistService, private recSvc: RecommendationService) {
    this.route.paramMap.subscribe(pm => {
      const cat = pm.get('id');
      this.selectedCategoryId = cat ? Number(cat) : null;
      this.page = 1;
      this.load();
    });
    this.route.queryParamMap.subscribe(qp => {
      this.q = qp.get('q') ?? '';
      this.page = Number(qp.get('page') ?? 1);
      this.minPrice = 0;
      this.maxPrice = 100;
      if (qp.has('minPrice')) {
        this.minPrice = Number(qp.get('minPrice'));
      }
      if (qp.has('maxPrice')) {
        this.maxPrice = Number(qp.get('maxPrice'));
      }
      this.sort = qp.get('sort') ?? '';

      // read multi-select categoryIds
      const cats = qp.getAll('categoryIds');
      this.selectedCategoryIds = cats.map(c => Number(c)).filter(n => !isNaN(n));

      // author
      this.author = qp.get('author') ?? null;

      // rating
      this.minRatingFilter = qp.has('minRating') ? Number(qp.get('minRating')) : null;

      // inStock
      this.inStockOnly = qp.has('inStock') ? (qp.get('inStock') === 'true' || qp.get('inStock') === '1') : false;

      this.load();
    });

    this.loadCategories();
    this.loadWishlist();

    // proactively load frontend-based recommendations (newest/popular)
    this.loadRecommendations();

    // subscribe to product changes to refresh listings
    this._subs.push(this.productService.productChanged$.subscribe(() => { this.load(); }));

    // also refresh on order completed events
    this._subs.push(this.cart.orderCompleted$.subscribe(() => { this.load(); }));
  }

  ngAfterViewInit(): void {
    // start autoplay after view is initialized
    this.startAutoplay();
  }

  ngOnDestroy(): void {
    this._subs.forEach(s => s.unsubscribe());
    this.stopAutoplay();
  }

  gotoProduct(id: number) {
    this.router.navigate(['/books', id]);
  }

  isLowStock(p: any): boolean {
    if (!p) return false;
    const initial = p.initialStock ?? p.InitialStock ?? p.Initialqty ?? 0;
    if (!initial || initial <= 0) return false;
    const threshold = Math.max(1, Math.ceil(initial * 0.1));
    return (p.stock ?? p.stockQty ?? p.StockQty ?? 0) <= threshold;
  }

  async loadWishlist() {
    try {
      const res: any = await this.wishlistSvc.getMyWishlist().toPromise();
      this.wishlist.clear();
      (res || []).forEach((i: any) => this.wishlist.add(i.productId));
    } catch { /* ignore for anonymous users */ }
  }

  toggleWishlist(p: any, e?: Event) {
    if (e) e.stopPropagation();
    if (this.wishlist.has(p.id)) {
      this.wishlistSvc.removeFromWishlist(p.id).subscribe({ next: () => { this.wishlist.delete(p.id); this.notify.success('Removed from wishlist'); }, error: () => this.notify.error('Failed to remove from wishlist') });
    } else {
      this.wishlistSvc.addToWishlist(p.id).subscribe({ next: () => { this.wishlist.add(p.id); this.notify.success('Added to wishlist'); }, error: () => this.notify.error('Failed to add to wishlist') });
    }
  }

  get visiblePages(): number[] {
    const maxVisible = 5;
    const half = Math.floor(maxVisible / 2);
    let start = Math.max(1, this.page - half);
    let end = Math.min(this.totalPages, start + maxVisible - 1);

    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }

    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  get priceMin(): number {
    return this.minPrice;
  }

  get priceMax(): number {
    return this.maxPrice;
  }

  get priceRangeLabel(): string {
    return `Price Range: $${this.priceMin} - $${this.priceMax}`;
  }

  get priceChipLabel(): string {
    return `Price: $${this.minPrice} - $${this.maxPrice}`;
  }

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }

  hasActiveFilters(): boolean {
    return !!(this.q || this.selectedCategoryId || this.selectedCategoryIds.length > 0 || this.minPrice !== 0 || this.maxPrice !== 100 || this.minRatingFilter != null || this.inStockOnly);
  }

  getCategoryName(id: number): string {
    return this.categories.find(c => c.id === id)?.name || '';
  }

  onSliderChange(value: number, which: 'min' | 'max') {
    if (which === 'min') {
      this.minPrice = Math.min(value, this.maxPrice - 1);
    } else {
      this.maxPrice = Math.max(value, this.minPrice + 1);
    }

    clearTimeout(this.priceDebounce);
    this.priceDebounce = setTimeout(() => this.applyFilters(), 500);
  }

  onRangeInput(event: Event, which: 'min' | 'max') {
    const input = event.target as HTMLInputElement;
    const val = Number(input.value);

    if (which === 'min') {
      this.minPrice = Math.min(val, this.maxPrice - 1);
    } else {
      this.maxPrice = Math.max(val, this.minPrice + 1);
    }

    clearTimeout(this.priceDebounce);
    this.priceDebounce = setTimeout(() => this.applyFilters(), 500);
  }

  clearFilters() {
    this.q = '';
    this.minPrice = 0;
    this.maxPrice = 100;
    this.sort = '';
    this.selectCategory(null);
  }

  selectCategory(id: number | null) {
    this.selectedCategoryId = id;
    // if single selection used, sync multi-select
    this.selectedCategoryIds = id ? [id] : [];
    this.page = 1;
    this.applyFilters();
  }

  toggleCategorySelection(id: number, e?: Event) {
    if (e) e.stopPropagation();
    const idx = this.selectedCategoryIds.indexOf(id);
    if (idx >= 0) this.selectedCategoryIds.splice(idx, 1);
    else this.selectedCategoryIds.push(id);
    this.page = 1;
    this.applyFilters();
  }

  setRatingFilter(r: number | null) {
    this.minRatingFilter = r;
    this.page = 1;
    this.applyFilters();
  }

  setInStockOnly(val: boolean) {
    this.inStockOnly = val;
    this.page = 1;
    this.applyFilters();
  }

  private refreshAuthors() {
    this.bookService.getAuthors({
      q: this.q || undefined,
      categoryId: this.selectedCategoryId || undefined,
      categoryIds: this.selectedCategoryIds.length ? this.selectedCategoryIds : undefined,
      minPrice: this.minPrice !== 0 ? this.minPrice : undefined,
      maxPrice: this.maxPrice !== 100 ? this.maxPrice : undefined,
      minRating: this.minRatingFilter || undefined,
      inStock: this.inStockOnly || undefined
    }).subscribe({
      next: list => { this.authors = (list || []).map(a => ({ name: a.name, count: a.count })); },
      error: () => { this.authors = []; }
    });
  }

  applyFilters() {
    const query: any = {
      q: this.q || undefined,
      page: 1,
      sort: this.sort || undefined
    };

    if (this.minPrice !== 0) {
      query.minPrice = this.minPrice;
    }
    if (this.maxPrice !== 100) {
      query.maxPrice = this.maxPrice;
    }

    if (this.selectedCategoryIds && this.selectedCategoryIds.length > 0) {
      // Angular router will encode array params as repeated keys
      query.categoryIds = this.selectedCategoryIds;
    } else if (this.selectedCategoryId) {
      query.categoryId = this.selectedCategoryId;
    }

    if (this.author) query.author = this.author;

    if (this.minRatingFilter != null) {
      query.minRating = this.minRatingFilter;
    }

    if (this.inStockOnly) {
      query.inStock = true;
    }

    if (this.selectedCategoryId) {
      this.router.navigate(['/categories', this.selectedCategoryId], { queryParams: query });
    } else {
      this.router.navigate(['/books'], { queryParams: query });
    }

    // After navigating, also refresh authors list
    // (global authors list independent of page results)
    setTimeout(() => this.refreshAuthors(), 0);
  }

  load() {
    this.loading = true;
    this.bookService.getBooks({
      q: this.q,
      categoryId: this.selectedCategoryId ?? undefined,
      categoryIds: this.selectedCategoryIds && this.selectedCategoryIds.length ? this.selectedCategoryIds : undefined,
      author: this.author ?? undefined,
      page: this.page,
      pageSize: this.pageSize,
      minPrice: this.minPrice !== 0 ? this.minPrice : undefined,
      maxPrice: this.maxPrice !== 100 ? this.maxPrice : undefined,
      sort: this.sort,
      minRating: this.minRatingFilter ?? undefined,
      inStock: this.inStockOnly || undefined
    }).subscribe({
      next: (res: any) => {
        // normalize avg rating property to lower-case for templates
        this.products = (res.items || []).map((it: any) => ({
          ...it,
          avgRating: (it.avgRating ?? it.AvgRating ?? null),
          stock: (it.stockQty ?? it.StockQty ?? 0),
          initialStock: (it.initialStock ?? it.InitialStock ?? it.initialstock ?? 0)
        }));
        this.totalPages = res.totalPages;
        this.total = res.total || 0;
        this.loading = false;

        // ensure recommendations visible: fallback to current page if still empty
        if (!this.recommendations || this.recommendations.length === 0) {
          this.recommendations = this.products.slice(0, Math.min(8, this.products.length));
          console.debug('Recommendations (fallback from products):', this.recommendations);
        }

        // Refresh authors after load (in case initial page load)
        this.refreshAuthors();
      },
      error: (err) => {
        console.error('Failed to load products', err);
        this.products = [];
        this.totalPages = 1;
        this.total = 0;
        this.loading = false;

        // still attempt to show recommendations from cache if available
        if (!this.recommendations) this.recommendations = [];

        // Refresh authors on error as well
        this.refreshAuthors();
      }
    });
  }

  loadCategories() {
    this.productService.getCategories().subscribe({ next: (res: any) => { this.categories = res; }, error: (err) => { console.error('Failed to load categories', err); this.categories = []; } });
  }

  loadRecommendations() {
    const finalizeTrending = () => { this.loadTrending(); };
    this.recSvc.getForMe().subscribe({
      next: (res: any) => {
        this.recommendations = (res?.items || []);
        if (!this.recommendations || this.recommendations.length === 0) {
          // fallback to public recommendations
            this.recSvc.getPublic().subscribe({
              next: (pub: any) => { this.recommendations = (pub?.items || []); finalizeTrending(); },
              error: () => { finalizeTrending(); }
            });
        } else {
          finalizeTrending();
        }
      },
      error: (_err: any) => {
        // user not logged in or error: use public endpoint
        this.recSvc.getPublic().subscribe({
          next: (pub: any) => { this.recommendations = (pub?.items || []); finalizeTrending(); },
          error: () => { // fallback to newest page
            this.bookService.getBooks({ pageSize: 8, sort: 'newest' }).subscribe({
              next: (r: any) => { this.recommendations = (r.items || []).map((it: any) => ({ id: it.id, title: it.title, imageUrl: it.imageUrl, price: it.price })); finalizeTrending(); },
              error: () => { this.recommendations = []; finalizeTrending(); }
            });
          }
        });
      }
    });
  }

  private loadTrending() {
    this.productService.getTrending(16).subscribe({ next: (t: any) => { this.trending = t?.items || []; console.debug('Trending items loaded:', this.trending); }, error: (err:any) => { console.warn('Trending load failed', err); this.trending = []; } });
  }

  private startAutoplay() {
    this.stopAutoplay();
    this.autoplayTimer = setInterval(() => {
      if (!this.isAutoplayPaused) this.autoScroll();
    }, this.autoplayIntervalMs);
  }

  private stopAutoplay() {
    if (this.autoplayTimer) {
      clearInterval(this.autoplayTimer);
      this.autoplayTimer = null;
    }
  }

  pauseAutoplay() {
    this.isAutoplayPaused = true;
    // resume after short delay so user can interact
    setTimeout(() => this.isAutoplayPaused = false, 5000);
  }

  autoScroll() {
    try {
      const el = this.recRow?.nativeElement;
      if (!el) return;
      const max = el.scrollWidth - el.clientWidth;
      // if at or near end, go to start (infinite)
      if (el.scrollLeft + 10 >= max) {
        el.scrollTo({ left: 0, behavior: 'smooth' });
      } else {
        el.scrollBy({ left: 320, behavior: 'smooth' });
      }
    } catch { }
  }

  scrollRecsLeft() {
    this.pauseAutoplay();
    try { this.recRow?.nativeElement?.scrollBy({ left: -320, behavior: 'smooth' }); } catch { }
  }
  scrollRecsRight() {
    this.pauseAutoplay();
    try { this.recRow?.nativeElement?.scrollBy({ left: 320, behavior: 'smooth' }); } catch { }
  }

  goto(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    const query: any = {
      q: this.q || undefined,
      page: p,
      sort: this.sort || undefined
    };

    if (this.minPrice !== 0) query.minPrice = this.minPrice;
    if (this.maxPrice !== 100) query.maxPrice = this.maxPrice;

    if (this.selectedCategoryId) {
      this.router.navigate(['/categories', this.selectedCategoryId], { queryParams: query });
    } else {
      this.router.navigate(['/books'], { queryParams: query });
    }
  }

  addToCart(product: any) {
    if (!product) return;
    this.cart.addToCart(product.id).subscribe({
      next: () => {
        this.notify.success(`${product.title} added to cart`);
      },
      error: () => {
        this.notify.error('Failed to add to cart');
      }
    });
  }
}
