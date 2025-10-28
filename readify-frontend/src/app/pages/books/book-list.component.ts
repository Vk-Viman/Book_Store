import { Component } from '@angular/core';
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
    LoadingSkeletonComponent
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
    .active-filters {
      border-top: 1px solid var(--border-color);
      padding-top: 12px;
    }
    .list-group-item.active {
      background-color: var(--primary-color);
      color: white;
    }
  `]
})
export class BookListComponent {
  products: any[] = [];
  categories: any[] = [];
  q = '';
  page = 1;
  pageSize = 12;
  totalPages = 1;
  total = 0;
  selectedCategoryId: number | null = null;
  // default slider bounds (0-100)
  minPrice: number = 0;
  maxPrice: number = 100;
  sort: string = '';
  loading = false;
  private priceDebounce: any;

  constructor(private bookService: BookService, private productService: ProductService, private route: ActivatedRoute, private router: Router) {
    this.route.paramMap.subscribe(pm => {
      const cat = pm.get('id');
      this.selectedCategoryId = cat ? Number(cat) : null;
      this.page = 1;
      // ensure we reload when category route param changes
      this.load();
    });
    this.route.queryParamMap.subscribe(qp => {
      this.q = qp.get('q') ?? '';
      this.page = Number(qp.get('page') ?? 1);
      // read numeric params; if missing, keep defaults
      this.minPrice = qp.has('minPrice') ? Number(qp.get('minPrice')) : 0;
      this.maxPrice = qp.has('maxPrice') ? Number(qp.get('maxPrice')) : 100;
      this.sort = qp.get('sort') ?? '';
      this.load();
    });

    this.loadCategories();
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

  hasActiveFilters(): boolean {
    // treat default range as no filter
    return !!(this.q || this.selectedCategoryId || this.minPrice !== 0 || this.maxPrice !== 100);
  }

  getCategoryName(id: number): string {
    return this.categories.find(c => c.id === id)?.name || '';
  }

  onPriceChange() {
    clearTimeout(this.priceDebounce);
    this.priceDebounce = setTimeout(() => {
      this.applyFilters();
    }, 500);
  }

  clearFilters() {
    this.q = '';
    this.minPrice = 0;
    this.maxPrice = 100;
    this.sort = '';
    this.selectCategory(null);
  }

  applyFilters() {
    const query: any = { 
      q: this.q || undefined, 
      page: 1, 
      sort: this.sort || undefined
    };
    if (this.minPrice !== 0) query.minPrice = this.minPrice;
    if (this.maxPrice !== 100) query.maxPrice = this.maxPrice;

    if (this.selectedCategoryId) {
      this.router.navigate(['/categories', this.selectedCategoryId], { queryParams: query }).then(() => this.load());
    } else {
      this.router.navigate(['/books'], { queryParams: query }).then(() => this.load());
    }
  }

  load() {
    this.loading = true;
    this.bookService.getBooks({
      q: this.q,
      categoryId: this.selectedCategoryId ?? undefined,
      page: this.page,
      pageSize: this.pageSize,
      minPrice: this.minPrice,
      maxPrice: this.maxPrice,
      sort: this.sort
    }).subscribe({
      next: (res: any) => {
        this.products = res.items;
        this.totalPages = res.totalPages;
        this.total = res.total || 0;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load products', err);
        this.products = [];
        this.totalPages = 1;
        this.total = 0;
        this.loading = false;
      }
    });
  }

  loadCategories() {
    this.productService.getCategories().subscribe({ 
      next: (res: any) => { this.categories = res; }, 
      error: (err) => { console.error('Failed to load categories', err); this.categories = []; } 
    });
  }

  selectCategory(id: number | null) {
    this.selectedCategoryId = id;
    this.page = 1;
    const query: any = { 
      q: this.q || undefined, 
      page: 1, 
      minPrice: this.minPrice || undefined, 
      maxPrice: this.maxPrice || undefined, 
      sort: this.sort || undefined 
    };
    if (id) {
      this.router.navigate(['/categories', id], { queryParams: query }).then(() => this.load());
    } else {
      this.router.navigate(['/books'], { queryParams: query }).then(() => this.load());
    }
  }

  goto(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    const query: any = { 
      q: this.q || undefined, 
      page: p, 
      minPrice: this.minPrice || undefined, 
      maxPrice: this.maxPrice || undefined, 
      sort: this.sort || undefined 
    };
    if (this.selectedCategoryId) {
      this.router.navigate(['/categories', this.selectedCategoryId], { queryParams: query }).then(() => this.load());
    } else {
      this.router.navigate(['/books'], { queryParams: query }).then(() => this.load());
    }
  }

  // Add new handler in component
  onRangeInput(event: Event, which: 'min' | 'max') {
    const input = event.target as HTMLInputElement;
    const val = Number(input.value);
    if (which === 'min') {
      this.minPrice = Math.min(val, this.maxPrice);
    } else {
      this.maxPrice = Math.max(val, this.minPrice);
    }
    // debounce apply
    clearTimeout(this.priceDebounce);
    this.priceDebounce = setTimeout(() => this.applyFilters(), 300);
  }

  get priceMin(): number {
    return this.minPrice ?? 0;
  }

  get priceMax(): number {
    return this.maxPrice ?? 100;
  }

  get priceRangeLabel(): string {
    return `Price Range: $${this.priceMin} - $${this.priceMax}`;
  }

  get priceChipLabel(): string {
    return `Price: $${this.priceMin} - $${this.priceMax}`;
  }

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }
}
