import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdminOrdersComponent } from './admin-orders.component';
import { NotificationService } from '../../services/notification.service';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

class MockNotify { success(msg: string) {} error(msg: string) {} }

describe('AdminOrdersComponent', () => {
  let httpMock: HttpTestingController;
  let fixture: any;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HttpClientTestingModule, AdminOrdersComponent, NoopAnimationsModule], providers: [{ provide: NotificationService, useClass: MockNotify }] }).compileComponents();
    fixture = TestBed.createComponent(AdminOrdersComponent);
    httpMock = TestBed.inject(HttpTestingController);

    // prime and flush any initial GET requests made by the component
    fixture.detectChanges();
    const initReqs = httpMock.match((req) => req.url.startsWith('/api/admin/orders'));
    initReqs.forEach(r => r.flush({ items: [{ id: 1, userId: 2, orderDate: new Date().toISOString(), totalAmount: 10, orderStatus: 'Pending', items: [{ productId: 1, quantity: 1, unitPrice: 10 }] }], total: 1, page: 1, pageSize: 10 }));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('opens detail dialog when view clicked', (done) => {
    // replace the dialog with a simple mock to avoid opening the real dialog
    (fixture.componentInstance as any).dialog = ({ open: () => ({ afterClosed: () => ({ subscribe: () => {} }) }) } as unknown) as MatDialog;

    const btn = fixture.nativeElement.querySelector('button[title="View"]') as HTMLButtonElement;
    expect(btn).toBeTruthy();
    btn.click();

    // no further http expected but component should handle dialog open
    setTimeout(() => { done(); }, 10);
  });
});
