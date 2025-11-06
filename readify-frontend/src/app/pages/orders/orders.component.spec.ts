import { TestBed, ComponentFixture } from '@angular/core/testing';
import { OrdersComponent } from './orders.component';
import { of } from 'rxjs';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Component } from '@angular/core';
import { CartService } from '../../services/cart.service';

// create a host component to pass mocked orders via Input if needed
@Component({ template: '<app-orders></app-orders>', standalone: true, imports: [OrdersComponent] })
class TestHostComponent {}

class MockCartService {
  getOrders() {
    return of([
      { id: 1, orderDate: new Date().toISOString(), totalAmount: 10.5, status: 'Pending' },
      { id: 2, orderDate: new Date().toISOString(), totalAmount: 20.0, status: 'Pending' }
    ]);
  }
}

describe('OrdersComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, HttpClientTestingModule, RouterTestingModule.withRoutes([])],
      providers: [{ provide: CartService, useClass: MockCartService }]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('should render list of orders', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('mat-list-item').length).toBeGreaterThan(0);
    expect(el.textContent).toContain('Order #1');
  });
});
