import { TestBed, ComponentFixture } from '@angular/core/testing';
import { OrderDetailComponent } from './order-detail.component';
import { of } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { CartService } from '../../services/cart.service';

@Component({ template: '<app-order-detail></app-order-detail>', standalone: true, imports: [OrderDetailComponent] })
class HostDetail {}

class MockCartService {
  getOrders() {
    return of([
      { id: 1, orderDate: new Date().toISOString(), totalAmount: 10.5, status: 'Pending', items: [{ product: { title: 'Book' }, quantity: 1, unitPrice: 10.5 }] }
    ]);
  }
}

describe('OrderDetailComponent', () => {
  let fixture: ComponentFixture<HostDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostDetail, HttpClientTestingModule],
      providers: [
        { provide: CartService, useClass: MockCartService },
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
