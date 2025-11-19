import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { ConfirmService } from '../../shared/confirm.service';

@Component({
  selector: 'app-admin-po-list',
  standalone: true,
  imports: [CommonModule],
  providers: [ConfirmService],
  template: `
  <div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h3>Purchase Orders</h3>
      <button class="btn btn-primary" (click)="createNew()">Create PO</button>
    </div>
    <table class="table table-striped">
      <thead><tr><th>ID</th><th>Supplier</th><th>Date</th><th>Status</th><th>Items</th><th>Total</th><th>Actions</th></tr></thead>
      <tbody>
        <ng-container *ngFor="let po of pos">
          <tr>
            <td>{{po.id || po.Id}}</td>
            <td>{{getSupplierName(po.supplierId || po.SupplierId)}}</td>
            <td>{{(po.orderDate || po.OrderDate) | date:'yyyy-MM-dd'}}</td>
            <td>{{po.status || po.Status}}</td>
            <td>{{(po.items || po.Items)?.length}}</td>
            <td>{{(po.totalAmount ?? po.TotalAmount) | currency}}</td>
            <td>
              <a class="btn btn-sm btn-outline-primary me-2" [routerLink]="['/admin/purchase-orders', po.id || po.Id]">View</a>
              <button class="btn btn-sm btn-secondary me-2" (click)="toggleDetails(po)">{{ isExpanded(po) ? 'Hide' : 'Details' }}</button>
              <button class="btn btn-sm btn-success me-2" (click)="receive(po)" [disabled]="(po.status || po.Status) !== 'Pending'">Receive</button>
              <button class="btn btn-sm btn-danger" (click)="cancel(po)" [disabled]="(po.status || po.Status) !== 'Pending'">Cancel</button>
            </td>
          </tr>
          <tr *ngIf="isExpanded(po)">
            <td colspan="7">
              <div *ngIf="(po.items || po.Items)?.length; else noItems">
                <table class="table table-sm mb-0">
                  <thead><tr><th>Product</th><th>Qty</th><th>Unit Price</th><th>Received</th></tr></thead>
                  <tbody>
                    <tr *ngFor="let it of (po.items || po.Items)">
                      <td>{{ it.product?.title || it.productName || ('#' + (it.productId || it.ProductId)) }}</td>
                      <td>{{ it.quantity || it.Quantity }}</td>
                      <td>{{ (it.unitPrice ?? it.UnitPrice) | currency }}</td>
                      <td>{{ it.receivedQuantity ?? it.ReceivedQuantity || 0 }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <ng-template #noItems>
                <div class="text-muted">No items</div>
              </ng-template>
            </td>
          </tr>
        </ng-container>
      </tbody>
    </table>
  </div>
  `
})
export class AdminPoListComponent {
  pos: any[] = [];
  suppliersMap: Record<number,string> = {};
  expanded = new Set<number>();
  constructor(private http: HttpClient, private router: Router, private confirm: ConfirmService){ this.load(); this.loadSuppliers(); }
  load(){ this.http.get<any[]>('/api/admin/purchase-orders').subscribe({ next: d => this.pos = d || [], error: () => this.pos = [] }); }
  loadSuppliers(){ this.http.get<any[]>('/api/admin/suppliers').subscribe({ next: d => { (d||[]).forEach(s => this.suppliersMap[s.id] = s.name); }, error: () => {} }); }
  getSupplierName(id:number){ return this.suppliersMap[id] || String(id); }
  createNew(){ this.router.navigate(['/admin/purchase-orders/new']); }
  async receive(po:any){ if(!(await this.confirm.confirm('Mark PO as received?', 'Receive PO'))) return; this.http.post(`/api/admin/purchase-orders/${po.id || po.Id}/receive`, {}).subscribe({ next: () => this.load(), error: () => alert('Failed to receive') }); }
  async cancel(po:any){ if(!(await this.confirm.confirm('Cancel this purchase order?', 'Cancel PO'))) return; this.http.post(`/api/admin/purchase-orders/${po.id || po.Id}/cancel`, {}).subscribe({ next: () => this.load(), error: () => alert('Failed to cancel') }); }
  toggleDetails(po:any){ const id = po.id || po.Id; if(this.expanded.has(id)) this.expanded.delete(id); else this.expanded.add(id); }
  isExpanded(po:any){ return this.expanded.has(po.id || po.Id); }
}
