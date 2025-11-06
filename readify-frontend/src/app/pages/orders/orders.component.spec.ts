import { TestBed, ComponentFixture } from '@angular/core/testing';
import { OrdersComponent } from './orders.component';
import { of } from 'rxjs';
import { OrderService } from '../../services/order.service';
import { NotificationService } from '../../services/notification.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

class MockOrderService {
  getMyOrders() {
    return of([{ id: 1, createdAt: new Date().toISOString(), status: 'Processing', total: 12.34 }]);
  }
}
class MockNotificationService {
  success(_m: string) {}
  error(_m: string) {}
}

describe('OrdersComponent', () => {
  let fixture: ComponentFixture<OrdersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrdersComponent, HttpClientTestingModule, RouterTestingModule.withRoutes([])],
      providers: [
        { provide: OrderService, useClass: MockOrderService },
        { provide: NotificationService, useClass: MockNotificationService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OrdersComponent);
    fixture.detectChanges();
  });

  it('should render list of orders', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const items = el.querySelectorAll('mat-list-item');
    expect(items.length).toBeGreaterThan(0);
    expect(el.textContent).toContain('Order #1');
  });
});
