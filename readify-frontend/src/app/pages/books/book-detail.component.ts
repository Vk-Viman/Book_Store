import { Component, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductService } from '../../services/product.service';
import { CartService } from '../../services/cart.service';
import { NotificationService } from '../../services/notification.service';
import { LocalDatePipe } from '../../pipes/local-date.pipe';
import { WishlistService } from '../../services/wishlist.service';
import { ReviewService } from '../../services/review.service';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { RecommendationService } from '../../services/recommendation.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-book-detail',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    LocalDatePipe,
    FormsModule
  ],
  template: `
  <div class="container mt-4">
    <!-- Breadcrumbs -->
    <nav aria-label="breadcrumb" class="mb-3">
      <ol class="breadcrumb">
        <li class="breadcrumb-item"><a routerLink="/books">Books</a></li>
        <li class="breadcrumb-item active" aria-current="page">{{ product?.title || 'Book Details' }}</li>
      </ol>
    </nav>

    <!-- Loading State -->
    <div *ngIf="loading" class="text-center py-5">
      <mat-spinner></mat-spinner>
      <p class="mt-3 text-muted">Loading book details...</p>
    </div>

    <!-- Product Details -->
    <mat-card *ngIf="!loading && product" class="product-detail-card">
      <div class="row">
        <div class="col-md-4">
          <img 
            [src]="product.imageUrl || 'assets/book-placeholder.svg'" 
            (error)="onImgError($event)" 
            class="img-fluid rounded shadow-sm product-image" 
            [alt]="product.title"
            loading="lazy">
        </div>
        <div class="col-md-8">
          <mat-card-header class="mb-3">
            <mat-card-title class="product-title">
              <h1 class="mb-0">{{ product.title }}</h1>
            </mat-card-title>
            <mat-card-subtitle class="product-authors">
              <mat-icon class="me-1">person</mat-icon>
              {{ product.authors }}
            </mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            <div class="price-section mb-4">
              <h2 class="price-tag mb-2">{{ product.price | currency }}</h2>

              <div class="rating mb-2" *ngIf="product.avgRating || product.avgRating === 0">
                <ng-container *ngFor="let i of [1,2,3,4,5]; let idx = index">
                  <mat-icon color="accent">{{ (product.avgRating >= (idx+1) ? 'star' : (product.avgRating >= (idx+0.5) ? 'star_half' : 'star_border')) }}</mat-icon>
                </ng-container>
                <span class="ms-2 small text-muted">{{ product.avgRating | number:'1.1-2' }}</span>
              </div>

              <mat-chip-set>
                <mat-chip *ngIf="product.stockQty > 0" class="stock-chip in-stock">
                  <mat-icon>check_circle</mat-icon>
                  In Stock: {{ product.stockQty }} available
                  <span *ngIf="isLowStock(product)" class="ms-2" style="background:#fff3e0;color:#ff9800;padding:2px 6px;border-radius:4px;font-weight:600;">Low stock ({{ lowStockPercent(product) | number:'1.0-0' }}%)</span>
                </mat-chip>
                <mat-chip *ngIf="product.stockQty === 0" class="stock-chip out-of-stock">
                  <mat-icon>cancel</mat-icon>
                  Out of Stock
                </mat-chip>
                <mat-chip *ngIf="product.categoryName">
                  <mat-icon>category</mat-icon>
                  {{ product.categoryName }}
                </mat-chip>
              </mat-chip-set>

              <div *ngIf="isLowStock(product)" class="alert alert-warning mt-2">Only {{ product.stockQty }} left — running low, order soon.</div>
            </div>

            <div class="description-section mb-4">
              <h3 class="section-title">
                <mat-icon>description</mat-icon>
                Description
              </h3>
              <p class="product-description">{{ product.description || 'No description available.' }}</p>
            </div>

            <div class="details-section mb-4">
              <h3 class="section-title">
                <mat-icon>info</mat-icon>
                Details
              </h3>
              <div class="row">
                <div class="col-md-6">
                  <p><strong>Price:</strong> {{ product.price | currency }}</p>
                  <p><strong>Stock:</strong> {{ product.stockQty }} units <span *ngIf="product.initialStock">(Initial: {{ product.initialStock }})</span></p>
                </div>
                <div class="col-md-6">
                  <p><strong>Category:</strong> {{ product.categoryName || 'N/A' }}</p>
                  <p><strong>Authors:</strong> {{ product.authors }}</p>
                </div>
              </div>
            </div>

            <!-- Inline Recommendations (compact strip) -->
            <div *ngIf="recItems?.length" class="recommendations-inline mb-4">
              <div class="d-flex align-items-center mb-2">
                <h5 class="mb-0">You might also like</h5>
                <button class="btn btn-sm btn-link ms-auto" (click)="refreshRecommendations()"><mat-icon>refresh</mat-icon> Refresh</button>
              </div>
              <div class="d-flex gap-2 overflow-auto">
                <mat-card *ngFor="let it of recItems | slice:0:6" class="rec-inline-card" (click)="gotoProduct(it.id)">
                  <img [src]="it.imageUrl || 'assets/book-placeholder.svg'" class="rec-inline-image" alt="{{it.title}}" />
                  <mat-card-content>
                    <div class="rec-inline-title">{{ it.title }}</div>
                    <div class="rec-inline-price text-muted">{{ it.price | currency }}</div>
                  </mat-card-content>
                </mat-card>
              </div>
            </div>

            <!-- Reviews -->
            <div class="reviews-section mt-4">
              <h4>Reviews</h4>

              <div *ngIf="reviews?.length === 0" class="text-muted mb-3">No reviews yet.</div>

              <div *ngFor="let r of reviews" class="mb-3">
                <div class="d-flex align-items-center gap-2">
                  <ng-container *ngFor="let i of [1,2,3,4,5]; let idx = index">
                    <mat-icon color="accent">{{ (r.rating >= (idx+1) ? 'star' : (r.rating >= (idx+0.5) ? 'star_half' : 'star_border')) }}</mat-icon>
                  </ng-container>
                  <div class="small text-muted ms-2">{{ r.createdAt | localDate:'short' }}</div>
                </div>
                <div class="mt-1">{{ r.comment }}</div>
                <hr />
              </div>

              <div *ngIf="isLoggedIn">
                <h5 class="mt-3">Leave a review</h5>
                <div class="d-flex align-items-center mb-2">
                  <label class="me-2 small">Rating:</label>
                  <ng-container *ngFor="let v of [1,2,3,4,5]">
                    <button class="btn btn-link p-0 me-1" (click)="reviewRating = v" [attr.aria-pressed]="reviewRating===v" title="{{v}} stars">
                      <mat-icon color="accent">{{ reviewRating >= v ? 'star' : 'star_border' }}</mat-icon>
                    </button>
                  </ng-container>
                </div>
                <div class="mb-2">
                  <textarea [(ngModel)]="reviewComment" class="form-control" rows="3" placeholder="Write your review..."></textarea>
                </div>
                <div class="d-flex gap-2">
                  <button class="btn btn-primary" (click)="submitReview()" [disabled]="submitting">Submit Review</button>
                  <button class="btn btn-outline-secondary" (click)="reviewRating=5; reviewComment=''">Reset</button>
                </div>
              </div>
              <div *ngIf="!isLoggedIn" class="text-muted">Please login to leave a review.</div>
            </div>

          </mat-card-content>

          <mat-card-actions class="action-buttons">
            <button mat-raised-button color="primary" [disabled]="product.stockQty === 0" class="me-2" (click)="addToCart()">
              <mat-icon>shopping_cart</mat-icon>
              Add to Cart
            </button>
            <button mat-stroked-button color="accent" [disabled]="product.stockQty === 0" (click)="toggleWishlist()">
              <mat-icon>{{ inWishlist ? 'favorite' : 'favorite_border' }}</mat-icon>
              {{ inWishlist ? 'Saved' : 'Add to Wishlist' }}
            </button>
            <button mat-button routerLink="/books" class="ms-auto">
              <mat-icon>arrow_back</mat-icon>
              Back to Books
            </button>
          </mat-card-actions>
        </div>
      </div>
    </mat-card>

    <!-- Error State -->
    <mat-card *ngIf="!loading && error" class="error-card">
      <mat-card-content class="text-center py-5">
        <mat-icon class="error-icon">error_outline</mat-icon>
        <h3 class="mt-3">Oops! Something went wrong</h3>
        <p class="text-muted">{{ error }}</p>
        <button mat-raised-button color="primary" routerLink="/books" class="mt-3">
          <mat-icon>home</mat-icon>
          Back to Books
        </button>
      </mat-card-content>
    </mat-card>
  </div>
  `,
  styles: [`
    .product-detail-card {
      padding: 24px;
    }
    .product-image {
      width: 100%;
      max-height: 500px;
      object-fit: contain;
    }
    .product-title h1 {
      font-size: 2rem;
      font-weight: 500;
      color: var(--text-primary);
    }
    .product-authors {
      display: flex;
      align-items: center;
      font-size: 1.1rem;
      margin-top: 8px;
    }
    .price-section {
      border-bottom: 1px solid var(--border-color);
      padding-bottom: 16px;
    }
    .price-tag {
      font-size: 2.5rem;
      font-weight: 600;
      color: var(--primary-color);
    }
    .stock-chip {
      display: inline-flex;
      align-items: center;
      gap: 4px;
    }
    .stock-chip.in-stock {
      background-color: #e8f5e9;
      color: #2e7d32;
    }
    .stock-chip.out-of-stock {
      background-color: #ffebee;
      color: #c62828;
    }
    .section-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 1.3rem;
      font-weight: 500;
      margin-bottom: 12px;
      color: var(--text-primary);
    }
    .product-description {
      font-size: 1rem;
      line-height: 1.6;
      color: var(--text-secondary);
    }
    .details-section p {
      margin-bottom: 8px;
    }
    .action-buttons {
      display: flex;
      gap: 8px;
      padding-top: 16px;
      border-top: 1px solid var(--border-color);
    }
    .error-card {
      margin-top: 40px;
    }
    .error-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: var(--error-color);
    }
    .breadcrumb {
      background: transparent;
      padding: 0;
    }
    .breadcrumb-item a {
      color: var(--primary-color);
      text-decoration: none;
    }
    .breadcrumb-item a:hover {
      text-decoration: underline;
    }
    .reviews-section {
      border-top: 1px solid var(--border-color);
      padding-top: 16px;
    }
    .reviews-section h4 {
      font-size: 1.5rem;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 16px;
    }
    .reviews-section .mat-icon {
      font-size: 1.2rem;
    }
    .reviews-section .small {
      font-size: 0.9rem;
    }
    .reviews-section textarea {
      resize: none;
    }

    /* inline recommendations */
    .recommendations-inline { border-top: 1px solid var(--border-color); padding-top:12px; }
    .rec-inline-card { width: 140px; padding:8px; display:flex; flex-direction:column; align-items:center; gap:6px; flex: 0 0 auto; cursor:pointer; border-radius:8px; }
    .rec-inline-image { width:80px; height:110px; object-fit:cover; border-radius:4px; }
    .rec-inline-title { font-weight:600; font-size:0.85rem; text-align:center; }
    .rec-inline-price { font-size:0.8rem; }

    @media (max-width: 768px) {
      .product-title h1 {
        font-size: 1.5rem;
      }
      .price-tag {
        font-size: 2rem;
      }
      .action-buttons {
        flex-direction: column;
      }
      .action-buttons .ms-auto {
        margin-left: 0 !important;
        margin-top: 8px;
      }
    }
  `]
})
export class BookDetailComponent implements OnDestroy {
  product: any = null;
  id: number | null = null;
  error = '';
  loading = false;
  inWishlist = false;

  reviews: any[] = [];
  reviewRating = 5;
  reviewComment = '';
  submitting = false;
  isLoggedIn = false;

  // recommendations
  recItems: any[] = [];

  private _subs: Subscription[] = [];

  constructor(
    private route: ActivatedRoute, 
    private router: Router,
    private productService: ProductService,
    private cart: CartService,
    private notify: NotificationService,
    private wishlistSvc: WishlistService,
    private reviewSvc: ReviewService,
    private auth: AuthService,
    private recSvc: RecommendationService
  ) {
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    this.isLoggedIn = !!this.auth.getToken();
    if (this.id) this.load();

    // refresh product when an order completes to show updated stock
    this._subs.push(this.cart.orderCompleted$.subscribe({ next: () => { if (this.id) this.refreshProduct(); } }));

    // refresh product when admin creates/updates/deletes products
    this._subs.push(this.productService.productChanged$.subscribe(() => { if (this.id) this.refreshProduct(); }));
  }

  ngOnDestroy(): void {
    this._subs.forEach(s => s.unsubscribe());
  }

  load() {
    this.error = '';
    this.loading = true;
    this.productService.getProduct(this.id!).subscribe({ next: (res: any) => { this.product = res; this.loading = false; this.checkWishlist(); this.loadReviews(); this.loadRecommendations(); }, error: (err: any) => { console.error('Failed to load product', err); this.error = 'Failed to load book details. Please try again later.'; this.loading = false; } });
  }

  isLowStock(p: any): boolean {
    if (!p) return false;
    const initial = p.initialStock ?? p.InitialStock ?? 0;
    if (!initial || initial <= 0) return false;
    const threshold = Math.max(1, Math.ceil(initial * 0.1));
    return (p.stockQty ?? p.Stock ?? p.stock ?? 0) <= threshold;
  }

  lowStockPercent(p: any): number {
    const initial = p.initialStock ?? p.InitialStock ?? 0;
    if (!initial || initial <= 0) return 0;
    const cur = (p.stockQty ?? p.Stock ?? p.stock ?? 0);
    return Math.max(0, Math.min(100, Math.round((cur / initial) * 100)));
  }

  async checkWishlist() {
    try {
      const list: any = await this.wishlistSvc.getMyWishlist().toPromise();
      this.inWishlist = (list || []).some((i: any) => i.productId === this.product.id);
    } catch { this.inWishlist = false; }
  }

  toggleWishlist() {
    if (!this.product) return;
    if (this.inWishlist) {
      this.wishlistSvc.removeFromWishlist(this.product.id).subscribe({ next: () => { this.inWishlist = false; this.notify.success('Removed from wishlist'); }, error: () => this.notify.error('Failed to remove from wishlist') });
    } else {
      this.wishlistSvc.addToWishlist(this.product.id).subscribe({ next: () => { this.inWishlist = true; this.notify.success('Added to wishlist'); }, error: () => this.notify.error('Failed to add to wishlist') });
    }
  }

  loadReviews() {
    if (!this.product) return;
    this.reviewSvc.getApprovedForProduct(this.product.id).subscribe({ next: (list) => { this.reviews = list || []; }, error: () => { this.reviews = []; } });
  }

  loadRecommendations() {
    if (!this.isLoggedIn) { this.recItems = []; return; }
    this.recSvc.getForMe().subscribe({ next: (res: any) => { this.recItems = res.items ?? []; }, error: () => { this.recItems = []; } });
  }

  refreshRecommendations() {
    if (!this.isLoggedIn) return;
    this.recSvc.refreshForMe().subscribe({ next: (res: any) => { this.recItems = res.items ?? []; this.notify.success('Recommendations refreshed'); }, error: () => { this.notify.error('Failed to refresh recommendations'); } });
  }

  submitReview() {
    if (!this.product) return;
    if (!this.isLoggedIn) { this.notify.error('Please login to post reviews'); return; }
    if (!this.reviewComment || this.reviewComment.trim().length < 3) { this.notify.error('Please enter a short comment'); return; }
    this.submitting = true;
    const payload = { productId: this.product.id, rating: this.reviewRating, comment: this.reviewComment };
    this.reviewSvc.postReview(payload).subscribe({ next: () => { this.notify.success('Review submitted for moderation'); this.reviewComment = ''; this.reviewRating = 5; this.submitting = false; this.loadReviews(); }, error: () => { this.submitting = false; this.notify.error('Failed to submit review'); } });
  }

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }

  refreshProduct() {
    if (!this.id) return;
    this.productService.getProduct(this.id).subscribe({ next: (res:any) => { this.product = res; }, error: () => { } });
  }

  addToCart() {
    if (!this.product) return;
    this.cart.addToCart(this.product.id).subscribe({ next: () => { this.notify.success(`${this.product.title} added to cart`); }, error: (err: any) => { const msg = err?.error?.message || err?.message || 'Failed to add to cart'; this.notify.error(msg); } });
  }

  gotoProduct(id: number) {
    if (!id) return;
    this.router.navigate(['/books', id]);
  }
}
