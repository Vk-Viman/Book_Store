import { TestBed, ComponentFixture } from '@angular/core/testing';
import { OrderDetailComponent } from './order-detail.component';
import { of } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { OrderService } from '../../services/order.service';

@Component({ template: '<app-order-detail></app-order-detail>', standalone: true, imports: [OrderDetailComponent] })
class HostDetail {}

class MockOrderService {
  getOrderById(_id: number) {
    return of({ id: 1, createdAt: new Date().toISOString(), status: 'Processing', total: 12.34, items: [{ productName: 'Book', quantity: 1, unitPrice: 12.34 }] });
  }
  cancelOrder(id: number) { return of({}); }
}

describe('OrderDetailComponent', () => {
  let fixture: ComponentFixture<HostDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostDetail, HttpClientTestingModule],
      providers: [
        { provide: OrderService, useClass: MockOrderService },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (key: string) => '1' } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HostDetail);
    fixture.detectChanges();
  });

  it('should display order item', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Book');
  });
});
