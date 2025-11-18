import { Component, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../components/confirm-dialog.component';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-admin-promo-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
  <div class="container mt-4">
    <h2>Promo Codes</h2>
    <div class="row mb-3">
      <div class="col-md-6">
        <input class="form-control" [(ngModel)]="q" placeholder="Search by code" (keyup.enter)="load()" />
      </div>
      <div class="col-md-6 text-end">
        <a routerLink="/admin/promos/new" class="btn btn-primary">Create Promo</a>
      </div>
    </div>
    <table class="table table-striped">
      <thead>
        <tr><th>Code</th><th>Type</th><th>Percent</th><th>Fixed</th><th>Active</th><th>Used</th><th>Usage%</th><th>Actions</th></tr>
      </thead>
      <tbody>
        <tr *ngFor="let p of promos">
          <td>{{ p.code }}</td>
          <td>{{ p.type }}</td>
          <td>{{ p.discountPercent }}</td>
          <td>{{ p.fixedAmount }}</td>
          <td>
            <div class="form-check form-switch m-0">
              <input class="form-check-input" type="checkbox" [checked]="p.isActive" (change)="toggleActive(p)" />
            </div>
          </td>
          <td>{{ statsMap[p.id]?.totalUsed ?? 0 }}</td>
          <td>{{ statsMap[p.id]?.usagePercent ?? '-' }}</td>
          <td>
            <a class="btn btn-sm btn-outline-primary me-2" [routerLink]="['/admin/promos', p.id]">Edit</a>
            <button class="btn btn-danger btn-sm" (click)="confirmDelete(p.id)">Delete</button>
          </td>
        </tr>
      </tbody>
    </table>

    <nav *ngIf="totalPages > 1">
      <ul class="pagination">
        <li class="page-item" [class.disabled]="page === 1"><button class="page-link" (click)="goto(page-1)">Previous</button></li>
        <li class="page-item" *ngFor="let p of pages" [class.active]="p===page"><button class="page-link" (click)="goto(p)">{{p}}</button></li>
        <li class="page-item" [class.disabled]="page === totalPages"><button class="page-link" (click)="goto(page+1)">Next</button></li>
      </ul>
    </nav>

    <div class="mt-4">
      <h4>Usage Stats</h4>
      <button class="btn btn-sm btn-outline-secondary mb-2" (click)="loadStats()">Refresh Stats</button>
      <div *ngIf="stats?.length === 0" class="text-muted">No stats.</div>
      <table *ngIf="stats?.length" class="table table-bordered table-sm">
        <thead><tr><th>Code</th><th>Type</th><th>Used</th><th>Remaining</th><th>Usage</th><th>Limits</th><th>Adjust</th></tr></thead>
        <tbody>
          <tr *ngFor="let s of stats" [class.table-warning]="(s.usagePercent||0) >= 80">
            <td>{{s.code}}</td>
            <td>{{s.type}}</td>
            <td>{{s.totalUsed}}</td>
            <td>{{s.remainingUses ?? '∞'}}</td>
            <td>
              <div class="usage-bar"><div class="usage-fill" [style.width.%]="s.usagePercent || 0" [ngClass]="{'bg-danger': (s.usagePercent||0)>=80, 'bg-success': (s.usagePercent||0)<80}"></div></div>
              <div class="small mt-1">{{s.usagePercent ?? '-'}}%</div>
            </td>
            <td>
              <div class="small">Global: {{s.globalUsageLimit ?? '∞'}} / PerUser: {{s.perUserLimit ?? '-'}} / Min: {{s.minPurchase ?? '-'}} / Exp: {{ s.expiryDate | date:'yyyy-MM-dd' }}</div>
            </td>
            <td>
              <div class="input-group input-group-sm" *ngIf="s.globalUsageLimit">
                <input type="number" class="form-control" [(ngModel)]="s._newRemaining" placeholder="Set remaining" />
                <button class="btn btn-outline-primary" (click)="applyRemaining(s)" [disabled]="s._saving || s._newRemaining == null">Set</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="mt-3">
      <h5>Usage Chart</h5>
      <canvas #usageChart></canvas>
    </div>
  </div>
  `,
  styles: [`
    .usage-bar{height:8px;background:#eee;border-radius:4px;overflow:hidden;}
    .usage-fill{height:8px;transition:width .3s ease;}
  `]
})
export class AdminPromoListComponent implements AfterViewInit {
  promos: any[] = [];
  q = '';
  page = 1;
  pageSize = 10;
  totalPages = 0;
  pages: number[] = [];
  stats: any[] = [];
  statsMap: Record<number, any> = {};
  @ViewChild('usageChart') usageChart?: ElementRef<HTMLCanvasElement>;

  constructor(private http: HttpClient, private dialog: MatDialog, private notify: NotificationService) { this.load(); }

  ngAfterViewInit() { if (this.stats?.length) this.drawChart(); }

  load() {
    let params = new HttpParams().set('page', String(this.page)).set('pageSize', String(this.pageSize));
    if (this.q) params = params.set('q', this.q);
    this.http.get<any>('/api/admin/promos', { params }).subscribe({ next: (res: any) => {
      this.promos = res.items || res;
      this.totalPages = res.totalPages || 1;
      this.pages = Array.from({ length: this.totalPages }, (_, i) => i + 1);
      this.loadStats();
    }, error: (err) => { this.notify.error(err?.error?.message || 'Failed to load promos'); } });
  }

  goto(p: number) { if (p < 1 || p > this.totalPages) return; this.page = p; this.load(); }

  toggleActive(p: any) {
    const newVal = !p.isActive;
    // optimistic toggle
    p.isActive = newVal;
    this.http.patch(`/api/admin/promos/${p.id}/active`, newVal).subscribe({
      next: () => this.notify.success(`Promo ${newVal ? 'activated' : 'deactivated'}`),
      error: (err) => { p.isActive = !newVal; this.notify.error(err?.error?.message || 'Failed to change active state'); }
    });
  }

  confirmDelete(id: number) {
    const ref = this.dialog.open(ConfirmDialogComponent, { data: { title: 'Delete promo', message: 'Delete this promo? This action cannot be undone.' } });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.http.delete(`/api/admin/promos/${id}`).subscribe({ next: () => { this.notify.success('Promo deleted'); this.load(); }, error: (err) => { this.notify.error(err?.error?.message || 'Failed to delete promo'); } });
    });
  }

  loadStats() {
    this.http.get<any[]>('/api/admin/promos/stats').subscribe({ next: d => { this.stats = (d||[]).map(x => ({ ...x, _newRemaining: null, _saving:false })); this.statsMap = {}; this.stats.forEach(s => { this.statsMap[s.id] = s; }); setTimeout(() => this.drawChart(), 0); }, error: err => { this.notify.error(err?.error?.message || 'Failed to load stats'); } });
  }

  applyRemaining(row: any) {
    if (!row || row._newRemaining == null) return;
    const val = Number(row._newRemaining);
    if (Number.isNaN(val) || val < 0) { this.notify.error('Invalid remaining value'); return; }
    row._saving = true;
    this.http.patch(`/api/admin/promos/${row.id}/remaining`, val).subscribe({
      next: (res: any) => { this.notify.success('Remaining updated'); row.remainingUses = res.remainingUses; row._saving = false; this.loadStats(); },
      error: err => { this.notify.error(err?.error?.message || 'Failed to update remaining'); row._saving = false; }
    });
  }

  private drawChart() {
    const canvas = this.usageChart?.nativeElement; if (!canvas) return;
    const ctx = canvas.getContext('2d'); if (!ctx) return;
    const items = this.stats || []; if (!items.length) { canvas.width = 600; canvas.height = 100; ctx.clearRect(0,0,canvas.width,canvas.height); ctx.font='14px sans-serif'; ctx.fillStyle='#777'; ctx.fillText('No usage data', 20, 50); return; }
    const padding = 40; const barWidth = 50; const gap = 25; const axisBottom = 300; const axisTop = 20;
    const maxUsed = Math.max(...items.map(i => i.totalUsed || 0), 1);
    const width = padding * 2 + items.length * barWidth + (items.length - 1) * gap; const height = axisBottom + padding;
    canvas.width = width; canvas.height = height;
    ctx.clearRect(0,0,width,height);
    ctx.font='12px sans-serif'; ctx.textBaseline='middle'; ctx.fillStyle='#222';
    // grid & labels
    const ticks = 5; for (let t=0; t<=ticks; t++) { const value = Math.round(maxUsed * t / ticks); const y = axisBottom - (axisBottom - axisTop) * t / ticks; ctx.strokeStyle='#eee'; ctx.beginPath(); ctx.moveTo(padding-10,y); ctx.lineTo(width-padding+10,y); ctx.stroke(); ctx.fillStyle='#555'; ctx.fillText(String(value),5,y); }
    // bars
    items.forEach((it, idx) => {
      const used = it.totalUsed || 0; const pct = it.usagePercent || 0; const barH = (used / maxUsed) * (axisBottom - axisTop);
      const x = padding + idx * (barWidth + gap); const y = axisBottom - barH;
      ctx.fillStyle = pct >= 80 ? '#d32f2f' : '#2e7d32'; ctx.fillRect(x,y,barWidth,barH);
      ctx.fillStyle = '#000'; ctx.fillText(it.code, x, axisBottom + 15); ctx.fillText(used.toString(), x, y - 10);
    });
    // axes
    ctx.strokeStyle='#000'; ctx.beginPath(); ctx.moveTo(padding-10, axisTop); ctx.lineTo(padding-10, axisBottom); ctx.lineTo(width-padding+10, axisBottom); ctx.stroke();
  }
}
