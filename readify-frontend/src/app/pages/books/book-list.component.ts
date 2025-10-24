import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-book-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
  <div class="container mt-4">
    <div class="d-flex mb-3">
      <input class="form-control me-2" placeholder="Search books..." [(ngModel)]="q" (keyup.enter)="search()" />
      <button class="btn btn-outline-secondary" (click)="search()">Search</button>
    </div>

    <div class="row">
      <div class="col-md-3">
        <h5>Categories</h5>
        <ul class="list-group">
          <li (click)="selectCategory(null)" [class.active]="!selectedCategoryId" class="list-group-item list-group-item-action">All</li>
          <li *ngFor="let c of categories" (click)="selectCategory(c.id)" [class.active]="c.id===selectedCategoryId" class="list-group-item list-group-item-action">{{ c.name }}</li>
        </ul>
      </div>
      <div class="col-md-9">
        <div class="row">
          <div *ngFor="let p of products" class="col-md-4 mb-3">
            <div class="card h-100">
              <img [src]="p.imageUrl || 'assets/book-placeholder.svg'" (error)="onImgError($event)" class="card-img-top" alt="{{p.title}}">
              <div class="card-body d-flex flex-column">
                <h5 class="card-title">{{ p.title }}</h5>
                <p class="card-text small text-muted">{{ p.authors }}</p>
                <div class="mt-auto">
                  <strong>{{ p.price | currency }}</strong>
                  <a [routerLink]="['/books', p.id]" class="btn btn-sm btn-primary float-end">View</a>
                </div>
              </div>
            </div>
          </div>
        </div>
        <nav>
          <ul class="pagination">
            <li class="page-item" [class.disabled]="page<=1"><a class="page-link" (click)="goto(page-1)">Prev</a></li>
            <li *ngFor="let p of pages" class="page-item" [class.active]="p===page"><a class="page-link" (click)="goto(p)">{{p}}</a></li>
            <li class="page-item" [class.disabled]="page>=totalPages"><a class="page-link" (click)="goto(page+1)">Next</a></li>
          </ul>
          <div class="text-muted mt-2">Page {{page}} of {{totalPages}}</div>
        </nav>
      </div>
    </div>
  </div>
  `
})
export class BookListComponent {
  products: any[] = [];
  categories: any[] = [];
  q = '';
  page = 1;
  pageSize = 12;
  totalPages = 1;
  selectedCategoryId: number | null = null;

  constructor(private productService: ProductService, private route: ActivatedRoute, private router: Router) {
    // react to route param changes (e.g., /categories/:id)
    this.route.paramMap.subscribe(pm => {
      const cat = pm.get('id');
      this.selectedCategoryId = cat ? Number(cat) : null;
      this.page = 1; // reset page when category changes
      this.load();
    });

    this.loadCategories();
  }

  get pages(): number[] {
    return Array.from({ length: Math.max(1, this.totalPages) }, (_, i) => i + 1);
  }

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }

  search() {
    this.page = 1;
    // clear category when searching
    this.router.navigate(['/books'], { queryParams: { q: this.q } });
    this.selectedCategoryId = null;
    this.load();
  }

  load() {
    this.productService.getProducts({ q: this.q, categoryId: this.selectedCategoryId ?? undefined, page: this.page, pageSize: this.pageSize }).subscribe({
      next: (res: any) => {
        this.products = res.items;
        this.totalPages = res.totalPages;
      },
      error: (err) => {
        console.error('Failed to load products', err);
        this.products = [];
        this.totalPages = 1;
      }
    });
  }

  loadCategories() {
    this.productService.getCategories().subscribe({ next: (res: any) => { this.categories = res; }, error: (err) => { console.error('Failed to load categories', err); this.categories = []; } });
  }

  selectCategory(id: number | null) {
    this.selectedCategoryId = id;
    this.page = 1;
    if (id) {
      // navigate to categories route so URL is shareable
      this.router.navigate(['/categories', id]);
    } else {
      this.router.navigate(['/books']);
    }
    this.load();
  }

  goto(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.load();
  }
}
