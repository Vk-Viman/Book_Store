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
  template: `
  <div class="container mt-4">
    <!-- Filters Section -->
    <mat-card class="filter-card mb-4">
      <div class="row g-3 align-items-end">
        <div class="col-md-4">
          <mat-form-field appearance="outline" class="w-100">
            <mat-label>Search books</mat-label>
            <input matInput [(ngModel)]="q" (keyup.enter)="applyFilters()" placeholder="Search by title, author..." />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>
        </div>
        <div class="col-md-4">
          <label class="form-label">Price Range: ${{minPrice || 0}} - ${{maxPrice || 100}}</label>
          <mat-slider min="0" max="100" step="1" class="w-100">
            <input matSliderStartThumb [(ngModel)]="minPrice" (ngModelChange)="onPriceChange()" />
            <input matSliderEndThumb [(ngModel)]="maxPrice" (ngModelChange)="onPriceChange()" />
          </mat-slider>
        </div>
        <div class="col-md-3">
          <mat-form-field appearance="outline" class="w-100">
            <mat-label>Sort by</mat-label>
            <mat-select [(ngModel)]="sort" (selectionChange)="applyFilters()">
              <mat-option value="">Title (A → Z)</mat-option>
              <mat-option value="title_desc">Title (Z → A)</mat-option>
              <mat-option value="price_asc">Price: Low to High</mat-option>
              <mat-option value="price_desc">Price: High to Low</mat-option>
              <mat-option value="newest">Newest First</mat-option>
            </mat-select>
          </mat-form-field>
        </div>
        <div class="col-md-1">
          <button mat-raised-button color="warn" (click)="clearFilters()" *ngIf="hasActiveFilters()">
            <mat-icon>clear</mat-icon>
          </button>
        </div>
      </div>

      <!-- Active Filters Chips -->
      <div class="active-filters mt-2" *ngIf="hasActiveFilters()">
        <mat-chip-set>
          <mat-chip *ngIf="q" (removed)="q=''; applyFilters()">
            Search: {{q}}
            <button matChipRemove><mat-icon>cancel</mat-icon></button>
          </mat-chip>
          <mat-chip *ngIf="selectedCategoryId" (removed)="selectCategory(null)">
            Category: {{getCategoryName(selectedCategoryId)}}
            <button matChipRemove><mat-icon>cancel</mat-icon></button>
          </mat-chip>
          <mat-chip *ngIf="minPrice !== null || maxPrice !== null" (removed)="minPrice=null; maxPrice=null; applyFilters()">
            Price: ${{minPrice || 0}} - ${{maxPrice || 100}}
            <button matChipRemove><mat-icon>cancel</mat-icon></button>
          </mat-chip>
        </mat-chip-set>
      </div>
    </mat-card>

    <div class="row">
      <!-- Categories Sidebar -->
      <div class="col-md-3">
        <mat-card>
          <h5 class="mb-3">Categories</h5>
          <div class="list-group list-group-flush">
            <button 
              mat-button 
              class="list-group-item list-group-item-action text-start" 
              [class.active]="!selectedCategoryId" 
              (click)="selectCategory(null)"
              [attr.aria-pressed]="!selectedCategoryId">
              All Books
            </button>
            <button 
              *ngFor="let c of categories" 
              mat-button
              class="list-group-item list-group-item-action text-start" 
              [class.active]="c.id===selectedCategoryId" 
              (click)="selectCategory(c.id)"
              [attr.aria-pressed]="c.id===selectedCategoryId">
              {{ c.name }}
            </button>
          </div>
        </mat-card>
      </div>

      <!-- Products Grid -->
      <div class="col-md-9">
        <!-- Loading State -->
        <div *ngIf="loading" class="row">
          <div class="col-md-4 mb-3" *ngFor="let i of [1,2,3,4,5,6]">
            <app-loading-skeleton type="card"></app-loading-skeleton>
          </div>
        </div>

        <!-- Products -->
        <div *ngIf="!loading && products.length > 0" class="row">
          <div *ngFor="let p of products" class="col-md-4 mb-4">
            <mat-card class="h-100 product-card">
              <img 
                mat-card-image 
                [src]="p.imageUrl || 'assets/book-placeholder.svg'" 
                (error)="onImgError($event)" 
                [alt]="p.title"
                loading="lazy"
                class="product-image">
              <mat-card-header>
                <mat-card-title>{{ p.title }}</mat-card-title>
                <mat-card-subtitle>{{ p.authors }}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <p class="price-tag">{{ p.price | currency }}</p>
                <p class="stock-info" *ngIf="p.stockQty > 0">In Stock: {{ p.stockQty }}</p>
                <p class="stock-info text-danger" *ngIf="p.stockQty === 0">Out of Stock</p>
              </mat-card-content>
              <mat-card-actions align="end">
                <a mat-raised-button color="primary" [routerLink]="['/books', p.id]" [attr.aria-label]="'View details for ' + p.title">
                  View Details
                </a>
              </mat-card-actions>
            </mat-card>
          </div>
        </div>

        <!-- Empty State -->
        <div *ngIf="!loading && products.length === 0" class="text-center py-5">
          <mat-icon style="font-size: 64px; height: 64px; width: 64px; color: #ccc;">book</mat-icon>
          <h3 class="mt-3">No books found</h3>
          <p class="text-muted">Try adjusting your filters or search terms</p>
          <button mat-raised-button color="primary" (click)="clearFilters()">Clear Filters</button>
        </div>

        <!-- Pagination -->
        <nav *ngIf="!loading && products.length > 0" aria-label="Product pagination">
          <ul class="pagination justify-content-center">
            <li class="page-item" [class.disabled]="page<=1">
              <button class="page-link" (click)="goto(page-1)" [disabled]="page<=1" aria-label="Previous page">Previous</button>
            </li>
            <li *ngFor="let p of visiblePages" class="page-item" [class.active]="p===page">
              <button class="page-link" (click)="goto(p)" [attr.aria-current]="p===page ? 'page' : null">{{p}}</button>
            </li>
            <li class="page-item" [class.disabled]="page>=totalPages">
              <button class="page-link" (click)="goto(page+1)" [disabled]="page>=totalPages" aria-label="Next page">Next</button>
            </li>
          </ul>
          <div class="text-center text-muted mt-2" role="status" aria-live="polite">
            Page {{page}} of {{totalPages}} ({{total}} books total)
          </div>
        </nav>
      </div>
    </div>
  </div>
  `,
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
  minPrice: number | null = null;
  maxPrice: number | null = null;
  sort: string = '';
  loading = false;
  private priceDebounce: any;

  constructor(private bookService: BookService, private productService: ProductService, private route: ActivatedRoute, private router: Router) {
    this.route.paramMap.subscribe(pm => {
      const cat = pm.get('id');
      this.selectedCategoryId = cat ? Number(cat) : null;
      this.page = 1;
    });
    this.route.queryParamMap.subscribe(qp => {
      this.q = qp.get('q') ?? '';
      this.page = Number(qp.get('page') ?? 1);
      this.minPrice = qp.get('minPrice') ? Number(qp.get('minPrice')) : null;
      this.maxPrice = qp.get('maxPrice') ? Number(qp.get('maxPrice')) : null;
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

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }

  hasActiveFilters(): boolean {
    return !!(this.q || this.selectedCategoryId || this.minPrice !== null || this.maxPrice !== null);
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
    this.minPrice = null;
    this.maxPrice = null;
    this.sort = '';
    this.selectCategory(null);
  }

  applyFilters() {
    const query: any = { 
      q: this.q || undefined, 
      page: 1, 
      minPrice: this.minPrice || undefined, 
      maxPrice: this.maxPrice || undefined, 
      sort: this.sort || undefined 
    };
    if (this.selectedCategoryId) {
      this.router.navigate(['/categories', this.selectedCategoryId], { queryParams: query });
    } else {
      this.router.navigate(['/books'], { queryParams: query });
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
    const query: any = { 
      q: this.q || undefined, 
      page: 1, 
      minPrice: this.minPrice || undefined, 
      maxPrice: this.maxPrice || undefined, 
      sort: this.sort || undefined 
    };
    if (id) {
      this.router.navigate(['/categories', id], { queryParams: query });
    } else {
      this.router.navigate(['/books'], { queryParams: query });
    }
  }

  goto(p: number) {
    if (p < 1 || p > this.totalPages) return;
    const query: any = { 
      q: this.q || undefined, 
      page: p, 
      minPrice: this.minPrice || undefined, 
      maxPrice: this.maxPrice || undefined, 
      sort: this.sort || undefined 
    };
    if (this.selectedCategoryId) {
      this.router.navigate(['/categories', this.selectedCategoryId], { queryParams: query });
    } else {
      this.router.navigate(['/books'], { queryParams: query });
    }
  }
}
