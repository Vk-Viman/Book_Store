import { TestBed, ComponentFixture } from '@angular/core/testing';
import { OrderDetailComponent } from './order-detail.component';
import { ActivatedRoute } from '@angular/router';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

@Component({ template: '<app-order-detail></app-order-detail>', standalone: true, imports: [OrderDetailComponent] })
class HostDetail {}

describe('OrderDetailComponent', () => {
  let fixture: ComponentFixture<HostDetail>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostDetail, HttpClientTestingModule, NoopAnimationsModule],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (key: string) => '1' } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HostDetail);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should display order item', () => {
    fixture.detectChanges();

    // flush order response
    const req = httpMock.expectOne('/api/orders/1');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 1, createdAt: new Date().toISOString(), status: 'Processing', total: 12.34, items: [{ productName: 'Book', quantity: 1, unitPrice: 12.34 }] });

    // flush history response
    const h = httpMock.expectOne('/api/orders/1/history');
    expect(h.request.method).toBe('GET');
    h.flush([{ orderId:1, oldStatus: 'Pending', newStatus: 'Processing', timestamp: new Date().toISOString() }]);

    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Book');
  });

  it('loads order and timeline', (done) => {
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/orders/1');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 1, createdAt: new Date().toISOString(), status: 'Processing', total: 10, items: [] });

    const h = httpMock.expectOne('/api/orders/1/history');
    expect(h.request.method).toBe('GET');
    h.flush([{ orderId: 1, oldStatus: 'Pending', newStatus: 'Processing', timestamp: new Date().toISOString() }]);

    setTimeout(() => { done(); }, 20);
  });
});
