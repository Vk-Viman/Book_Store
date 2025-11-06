import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';
import { environment } from '../../../environments/environment';
import { NotificationService } from '../../services/notification.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ValidationSummaryComponent } from '../../components/validation-summary.component';

@Component({
  selector: 'app-admin-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, ValidationSummaryComponent],
  template: `
  <div class="container mt-4">
    <h3>{{ isNew ? 'Create' : 'Edit' }} Product</h3>

    <div class="mb-3">
      <div style="position:relative; display:inline-block">
        <img [src]="previewUrl || 'assets/book-placeholder.svg'" (error)="onImgError($event)" class="mb-3 img-thumbnail" style="max-width:200px; display:block;" />
        <div *ngIf="validating" style="position:absolute; top:8px; left:8px">
          <div class="spinner-border spinner-border-sm text-secondary" role="status"><span class="visually-hidden">Validating...</span></div>
        </div>
      </div>
      <small class="text-muted">Image preview</small>
      <div *ngIf="errorMsg" class="alert alert-danger mt-2">{{ errorMsg }}</div>
      <div *ngIf="validationInfo" class="alert alert-info mt-2">{{ validationInfo }}</div>
    </div>

    <form [formGroup]="form" (ngSubmit)="save()">
      <app-validation-summary [errors]="errors"></app-validation-summary>

      <mat-form-field appearance="outline" class="w-100 mb-3">
        <mat-label>Title</mat-label>
        <input matInput formControlName="title">
        <mat-error *ngIf="form.get('title')?.hasError('required')">Title is required</mat-error>
      </mat-form-field>

      <div class="mb-3">
        <label>Authors</label>
        <input class="form-control" formControlName="authors" />
      </div>
      <div class="mb-3">
        <label>Price</label>
        <input type="number" class="form-control" formControlName="price" />
      </div>
      <div class="mb-3">
        <label>Stock</label>
        <input type="number" class="form-control" formControlName="stockQty" />
      </div>

      <div class="mb-3">
        <label>Category</label>
        <select class="form-select" [value]="form.value.categoryId" (change)="onCategoryChange($any($event.target).value)">
          <option [value]="0">Select category</option>
          <option *ngFor="let c of categories" [value]="c.id">{{ c.name }}</option>
          <option value="add">+ Add new category...</option>
        </select>
      </div>

      <div *ngIf="showNewCategoryInput" class="mb-3">
        <label>New category name</label>
        <div class="input-group">
          <input class="form-control" [(ngModel)]="newCategoryName" name="newCategoryName" [ngModelOptions]="{standalone: true}" />
          <button type="button" class="btn btn-outline-secondary" (click)="createCategory()" [disabled]="!newCategoryName">Create</button>
        </div>
      </div>

      <div class="mb-3">
        <label>Image URL</label>
        <input class="form-control" formControlName="imageUrl" (input)="onImageUrlChange()" />
      </div>

      <div class="mb-3">
        <label>Or upload image</label>
        <input type="file" class="form-control" (change)="onFileSelected($event)" accept="image/*" />
      </div>

      <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || saving || validating">{{ saving ? 'Saving...' : 'Save' }}</button>
    </form>
  </div>

  <!-- global saving overlay -->
  <div *ngIf="saving" style="position:fixed; inset:0; background:rgba(255,255,255,0.6); display:flex; align-items:center; justify-content:center; z-index:1050;">
    <div class="text-center">
      <div class="spinner-border text-primary" role="status" style="width:3rem;height:3rem"><span class="visually-hidden">Saving...</span></div>
      <div class="mt-2">Saving product...</div>
    </div>
  </div>
  `
})
export class AdminProductFormComponent {
  form: any;
  previewUrl: string | null = null;
  errorMsg: string = '';
  validationInfo: string = '';
  categories: any[] = [];
  showNewCategoryInput = false;
  newCategoryName = '';
  saving = false;
  validating = false;

  isNew = true;

  // new property for template validation summary
  errors: string[] = [];

  constructor(private fb: FormBuilder, private prodSvc: ProductService, private http: HttpClient, private route: ActivatedRoute, private router: Router, private notify: NotificationService) {
    this.form = this.fb.group({
      id: [0],
      title: ['', Validators.required],
      authors: [''],
      description: [''],
      price: [0, Validators.required],
      stockQty: [0, Validators.required],
      categoryId: [0, Validators.required],
      imageUrl: ['']
    });

    // update preview as the user types
    const imgControl = this.form.get('imageUrl');
    if (imgControl) {
      imgControl.valueChanges.subscribe((val: string) => {
        this.previewUrl = (val || '').trim() || null;
        this.errorMsg = '';
        this.validationInfo = '';
      });
    }

    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') { this.isNew = false; this.load(Number(id)); }

    this.loadCategories();
  }

  private ensureAbsolute(url: string): string {
    if (!url) return url;
    if (/^https?:\/\//i.test(url)) return url;
    const apiBase = environment.apiUrl.replace(/\/?api\/?$/i, '');
    return apiBase + (url.startsWith('/') ? url : '/' + url);
  }

  onImageUrlChange() {
    const url = (this.form.value.imageUrl || '').trim();
    this.previewUrl = this.ensureAbsolute(url) || null;
  }

  load(id: number) {
    this.prodSvc.getProduct(id).subscribe((res: any) => {
      this.form.patchValue(res);
      // set preview to loaded image url
      this.previewUrl = this.ensureAbsolute(res?.imageUrl || '');
    });
  }

  loadCategories() {
    this.prodSvc.getCategories().subscribe({ next: (res: any) => { this.categories = res; }, error: (err) => { console.error('Failed to load categories', err); this.categories = []; } });
  }

  onImgError(event: Event) {
    const img = event?.target as HTMLImageElement | null;
    if (img) img.src = 'assets/book-placeholder.svg';
  }

  onCategoryChange(value: any) {
    if (value === 'add') {
      this.showNewCategoryInput = true;
      this.form.patchValue({ categoryId: 0 });
    } else {
      this.showNewCategoryInput = false;
      const id = Number(value) || 0;
      this.form.patchValue({ categoryId: id });
    }
  }

  createCategory() {
    const name = (this.newCategoryName || '').trim();
    if (!name) return;
    this.prodSvc.createCategory(name).subscribe({
      next: (res: any) => {
        // add to local list and select
        this.categories.push(res);
        this.form.patchValue({ categoryId: res.id });
        this.previewUrl = null;
        this.newCategoryName = '';
        this.showNewCategoryInput = false;
        this.notify.success('Category created');
      },
      error: (err) => {
        this.notify.error(err?.error?.message || 'Failed to create category');
      }
    });
  }

  onFileSelected(event: any) {
    const file: File | undefined = event?.target?.files?.[0];
    if (!file) return;

    // local preview
    const reader = new FileReader();
    reader.onload = () => {
      this.previewUrl = reader.result as string;
    };
    reader.readAsDataURL(file);

    const form = new FormData();
    form.append('file', file);
    this.validating = true;
    this.http.post('/api/upload/image', form).subscribe({
      next: (res: any) => {
        this.validating = false;
        let url = res?.url as string;
        url = this.ensureAbsolute(url);
        if (url) {
          this.form.patchValue({ imageUrl: url });
          this.previewUrl = url;
          this.notify.success('Image uploaded');
        }
      },
      error: (err) => {
        this.validating = false;
        const msg = err?.error?.message || 'Image upload failed';
        this.errorMsg = msg;
        this.notify.error(msg);
      }
    });
  }

  validateImageUrl(url: string): Promise<{ ok: boolean; message?: string }> {
    return new Promise(async (resolve) => {
      if (!url) return resolve({ ok: true });

      this.validating = true;
      const abs = this.ensureAbsolute(url);
      try {
        // call backend validation endpoint first
        const resp: any = await this.http.post('/api/admin/image/validate', { url: abs }).toPromise();
        if (resp && resp.ok) {
          this.validating = false;
          return resolve({ ok: true });
        }
        if (resp && resp.message) {
          this.validating = false;
          return resolve({ ok: false, message: resp.message });
        }
      } catch (e: any) {
        // fall back to client probe if backend validation fails or returns non-image
      }

      const img = new Image();
      let settled = false;
      img.onload = () => { if (!settled) { settled = true; this.validating = false; resolve({ ok: true }); } };
      img.onerror = () => { if (!settled) { settled = true; this.validating = false; resolve({ ok: false, message: 'Image did not load in browser' }); } };
      setTimeout(() => { if (!settled) { settled = true; this.validating = false; resolve({ ok: false, message: 'Image validation timed out' }); } }, 5000);
      img.src = abs;
    });
  }

  async save() {
    this.saving = true;
    // trim image url before sending
    const imageUrl = (this.form.value.imageUrl || '').trim();
    this.form.patchValue({ imageUrl: this.ensureAbsolute(imageUrl) });

    // Validate category selected
    const catId = Number(this.form.value.categoryId) || 0;
    if (catId <= 0) {
      this.errorMsg = 'Please select a valid category before saving.';
      this.saving = false;
      this.notify.info(this.errorMsg);
      return;
    }

    if (imageUrl) {
      this.validationInfo = 'Validating image URL...';
      const res = await this.validateImageUrl(imageUrl);
      this.validationInfo = '';
      if (!res.ok) {
        this.errorMsg = res.message || 'Image URL did not load as an image.';
        this.saving = false;
        this.notify.error(this.errorMsg);
        return;
      }
    }

    this.errorMsg = '';

    if (this.isNew) {
      this.prodSvc.createProduct(this.form.value).subscribe(() => { this.notify.success('Product created'); this.router.navigate(['/admin/products']); }, (err) => { this.saving = false; const msg = err?.error?.message ?? 'Failed to create product'; this.errorMsg = msg; this.notify.error(msg); });
    } else {
      const id = this.form.value.id;
      this.prodSvc.updateProduct(id, this.form.value).subscribe(() => { this.notify.success('Product updated'); this.router.navigate(['/admin/products']); }, (err) => { this.saving = false; const msg = err?.error?.message ?? 'Failed to update product'; this.errorMsg = msg; this.notify.error(msg); });
    }
  }
}
