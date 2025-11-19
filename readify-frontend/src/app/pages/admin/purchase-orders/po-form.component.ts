import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { ConfirmService } from '../../../shared/confirm.service';
import { CurrencyInputDirective } from '../../../shared/currency-input.directive';

@Component({
  selector: 'app-admin-po-form',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyInputDirective, RouterModule],
  template: `
  <div class="container mt-4">
    <h3>Create Purchase Order</h3>
    <form #f="ngForm" (ngSubmit)="submit(f)">
      <div class="mb-3">
        <label>Supplier</label>
        <select class="form-select" [(ngModel)]="model.supplierId" name="supplierId" required>
          <option [ngValue]="0">Select supplier</option>
          <option *ngFor="let s of suppliers" [ngValue]="s.id">{{s.name}}</option>
        </select>
        <div class="text-danger" *ngIf="f.submitted && (!model.supplierId || model.supplierId==0)">Supplier is required</div>
      </div>
      <div class="mb-3">
        <label>Items</label>
        <div *ngFor="let it of model.items; let i = index" class="input-group mb-2">
          <select class="form-select" [(ngModel)]="model.items[i].productId" name="product{{i}}" (change)="onProductChange(i)" required>
            <option [ngValue]="0">Select product</option>
            <option *ngFor="let p of products" [ngValue]="p.id">{{p.title}}</option>
          </select>
          <input class="form-control" type="number" [(ngModel)]="model.items[i].quantity" name="qty{{i}}" min="1" required />
          <input class="form-control" currencyInput type="text" step="0.01" [(ngModel)]="model.items[i].unitPrice" name="price{{i}}" placeholder="Unit price" required />
          <button type="button" class="btn btn-outline-danger" (click)="removeItem(i)">Remove</button>
        </div>
        <button type="button" class="btn btn-sm btn-outline-secondary" (click)="addItem()">Add item</button>
      </div>

      <div class="mb-3">
        <strong>Total: </strong> {{ total() | currency }}
      </div>

      <button class="btn btn-primary" [disabled]="!canSubmit(f)">Create PO</button>
    </form>
  </div>
  `
})
export class AdminPoFormComponent implements OnInit{
  suppliers: any[] = [];
  products: any[] = [];
  model: any = { supplierId:0, items: [{ productId:0, quantity:1, unitPrice:0 }] };
  constructor(private http: HttpClient, private router: Router, private route: ActivatedRoute, private confirm: ConfirmService){ }
  ngOnInit(): void{ this.http.get('/api/admin/suppliers').subscribe({ next: (d:any)=> this.suppliers = d || [] }); this.http.get('/api/products').subscribe({ next: (d:any)=> { this.products = d.items || []; } }); }
  addItem(){ this.model.items.push({ productId:0, quantity:1, unitPrice:0 }); }
  removeItem(i:number){ this.model.items.splice(i,1); }
  onProductChange(i:number){ const pid = this.model.items[i].productId; const p = this.products.find((x:any)=>x.id==pid || x.Id==pid); if(p) this.model.items[i].unitPrice = p.price ?? p.Price ?? 0; }
  total(){ return (this.model.items || []).reduce((s:any,it:any)=> s + ((Number(it.unitPrice) || Number(it.UnitPrice) || 0) * (Number(it.quantity) || Number(it.Quantity) || 0)), 0); }
  canSubmit(f:NgForm){ if(!f) return false; if(!f.form.valid) return false; if(!this.model.items || this.model.items.length==0) return false; for(const it of this.model.items){ if(!it.productId || it.productId==0) return false; if(!(Number(it.quantity)>0)) return false; if(!(Number(it.unitPrice)>=0)) return false; } return true; }
  async submit(f:NgForm){ if(!this.canSubmit(f)) { alert('Fix validation errors'); return; }
    if(!(await this.confirm.confirm('Create this purchase order?', 'Create PO'))) return;
    const payload = { supplierId: this.model.supplierId, items: this.model.items.map((it:any)=> ({ productId: Number(it.productId), quantity: Number(it.quantity), unitPrice: Number(it.unitPrice) })) };
    this.http.post('/api/admin/purchase-orders', payload).subscribe({ next: ()=> this.router.navigate(['/admin/purchase-orders']), error: ()=> this.confirm.confirm('Failed to create PO') }); }
}
