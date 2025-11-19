import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ConfirmService } from '../../shared/confirm.service';

@Component({
  selector: 'app-admin-po-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
  <div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h3>Purchase Order #{{po?.id || po?.Id}}</h3>
      <div>
        <button class="btn btn-sm btn-secondary me-2" (click)="back()">Back</button>
        <button class="btn btn-sm btn-success" (click)="receiveAll()" [disabled]="!canReceiveAll()">Receive All</button>
      </div>
    </div>

    <div *ngIf="po">
      <div class="mb-2"><strong>Supplier:</strong> {{ po.supplier?.name || po.supplierName || (po.supplierId || po.SupplierId) }}</div>
      <div class="mb-2"><strong>Status:</strong> {{ po.status || po.Status }}</div>

      <table class="table table-sm">
        <thead><tr><th>Product</th><th>Ordered</th><th>Unit Price</th><th>Subtotal</th><th>Received</th><th>Receive Now</th></tr></thead>
        <tbody>
          <tr *ngFor="let it of po.items || po.Items">
            <td>
              <a *ngIf="(it.product?.id || it.product?.Id || it.productId || it.ProductId)" [routerLink]="['/admin/products', it.product?.id || it.product?.Id || it.productId || it.ProductId]">
                {{ it.product?.title || it.productName || ('#' + (it.productId || it.ProductId)) }}
              </a>
              <span *ngIf="!(it.product?.id || it.product?.Id || it.productId || it.ProductId)">{{ it.product?.title || it.productName || ('#' + (it.productId || it.ProductId)) }}</span>
            </td>
            <td>{{ it.quantity || it.Quantity }}</td>
            <td>{{ (it.unitPrice ?? it.UnitPrice) | currency }}</td>
            <td>{{ ((it.unitPrice ?? it.UnitPrice) * (it.quantity || it.Quantity)) | currency }}</td>
            <td>{{ it.receivedQuantity ?? it.ReceivedQuantity || 0 }}</td>
            <td>
              <input type="number" min="0" [max]="maxReceivable(it)" [(ngModel)]="it._receiveNow" class="form-control form-control-sm" style="width:120px;" [disabled]="!canAcceptReceives() || maxReceivable(it) <= 0" />
            </td>
          </tr>
        </tbody>
      </table>

      <div class="d-flex justify-content-between align-items-center mt-3">
        <div>
          <strong>Total:</strong> {{ (po.totalAmount ?? po.TotalAmount) | currency }}
        </div>
        <div>
          <button class="btn btn-primary me-2" (click)="submitPartialReceive()" [disabled]="!hasReceiveQty() || !canAcceptReceives()">Submit Partial Receive</button>
        </div>
      </div>
    </div>

    <div *ngIf="!po && !loading" class="text-muted">Not found</div>
    <div *ngIf="loading">Loading...</div>
  `
})
export class AdminPoDetailComponent implements OnInit{
  po: any = null;
  loading = false;
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router, private confirm: ConfirmService){}

  ngOnInit(): void{ this.load(); }

  back(){ this.router.navigate(['/admin/purchase-orders']); }

  load(){
    this.loading = true;
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.http.get(`/api/admin/purchase-orders/${id}`).subscribe({ next: (d:any) => { this.po = d || null; if(this.po){ (this.po.items || this.po.Items || []).forEach((it:any)=> it._receiveNow = 0); } this.loading = false; }, error: ()=> { this.po = null; this.loading = false; } });
  }

  canReceiveAll(){ if(!this.po) return false; return (this.po.status || this.po.Status) === 'Pending'; }

  canAcceptReceives(){ if(!this.po) return false; const s = (this.po.status || this.po.Status) || ''; return s === 'Pending' || s === 'Processing'; }

  maxReceivable(it:any){ const ordered = (it.quantity || it.Quantity) || 0; const received = (it.receivedQuantity ?? it.ReceivedQuantity) || 0; return Math.max(0, ordered - received); }

  async receiveAll(){ if(!(await this.confirm.confirm('Receive all items for this PO?', 'Receive PO'))) return; const id = this.po.id || this.po.Id; this.http.post(`/api/admin/purchase-orders/${id}/receive`, {}).subscribe({ next: ()=> this.load(), error: ()=> this.confirm.confirm('Failed to receive').then(()=>{}) }); }

  hasReceiveQty(){ if(!this.po) return false; if(!this.canAcceptReceives()) return false; const any = (this.po.items || this.po.Items || []).some((it:any)=> (it._receiveNow || 0) > 0); return any; }

  async submitPartialReceive(){
    if(!this.canAcceptReceives()) return this.confirm.confirm('PO cannot accept receives in its current status', 'Receive PO');
    if(!(await this.confirm.confirm('Submit partial receive?','Partial Receive'))) return;
    const id = this.po.id || this.po.Id;
    const items = (this.po.items || this.po.Items || []).map((it:any)=> ({ PurchaseOrderItemId: it.id || it.Id, ReceivedQuantity: Number(it._receiveNow) || 0 })).filter((i:any)=> i.ReceivedQuantity > 0);
    if(items.length === 0) return this.confirm.confirm('No quantities entered');
    this.http.post(`/api/admin/purchase-orders/${id}/receive-partial`, items).subscribe({ next: ()=> { this.load(); }, error: ()=> this.confirm.confirm('Failed to submit partial receive').then(()=>{}) });
  }
}
