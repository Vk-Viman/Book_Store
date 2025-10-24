import { Component } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-book-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
  <div class="container mt-4" *ngIf="product">
    <div class="row">
      <div class="col-md-4">
        <img [src]="product.imageUrl || 'assets/book-placeholder.svg'" (error)="onImgError($event)" class="img-fluid" alt="{{product.title}}">
      </div>
      <div class="col-md-8">
        <h2>{{ product.title }}</h2>
        <p class="text-muted">{{ product.authors }}</p>
        <h4>{{ product.price | currency }}</h4>
        <p>{{ product.description }}</p>
        <button class="btn btn-primary" [disabled]="true">Add to Cart</button>
      </div>
    </div>
  </div>
  <div *ngIf="error" class="container mt-4"><div class="alert alert-danger">{{ error }}</div></div>
  `
})
export class BookDetailComponent {
  product: any = null;
  id: number | null = null;
  error = '';

  constructor(private route: ActivatedRoute, private productService: ProductService) {
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    if (this.id) this.load();
  }

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }

  load() {
    this.error = '';
    this.productService.getProduct(this.id!).subscribe({
      next: (res: any) => this.product = res,
      error: (err) => {
        console.error('Failed to load product', err);
        this.error = 'Failed to load product details. Is the API running?';
      }
    });
  }
}
