import { Component } from '@angular/core';
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
    MatProgressSpinnerModule
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
              <mat-chip-set>
                <mat-chip *ngIf="product.stockQty > 0" class="stock-chip in-stock">
                  <mat-icon>check_circle</mat-icon>
                  In Stock: {{ product.stockQty }} available
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
                  <p><strong>Stock:</strong> {{ product.stockQty }} units</p>
                </div>
                <div class="col-md-6">
                  <p><strong>Category:</strong> {{ product.categoryName || 'N/A' }}</p>
                  <p><strong>Authors:</strong> {{ product.authors }}</p>
                </div>
              </div>
            </div>
          </mat-card-content>

          <mat-card-actions class="action-buttons">
            <button mat-raised-button color="primary" [disabled]="product.stockQty === 0" class="me-2" (click)="addToCart()">
              <mat-icon>shopping_cart</mat-icon>
              Add to Cart
            </button>
            <button mat-stroked-button color="accent" [disabled]="product.stockQty === 0">
              <mat-icon>favorite</mat-icon>
              Add to Wishlist
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
export class BookDetailComponent {
  product: any = null;
  id: number | null = null;
  error = '';
  loading = false;

  constructor(
    private route: ActivatedRoute, 
    private router: Router,
    private productService: ProductService,
    private cart: CartService,
    private notify: NotificationService
  ) {
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    if (this.id) this.load();
  }

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }

  load() {
    this.error = '';
    this.loading = true;
    this.productService.getProduct(this.id!).subscribe({
      next: (res: any) => {
        this.product = res;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load product', err);
        this.error = 'Failed to load book details. Please try again later.';
        this.loading = false;
      }
    });
  }

  addToCart() {
    if (!this.product) return;
    this.cart.addToCart(this.product.id).subscribe({
      next: () => {
        this.notify.success(`${this.product.title} added to cart`);
      },
      error: () => {
        this.notify.error('Failed to add to cart');
      }
    });
  }
}
