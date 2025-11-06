import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { OrderService } from './order.service';

describe('OrderService', () => {
  let service: OrderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [OrderService] });
    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should call /api/orders/me', (done) => {
    service.getMyOrders().subscribe(data => { expect(Array.isArray(data)).toBeTruthy(); done(); });
    const req = httpMock.expectOne('/api/orders/me');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should call get order by id', (done) => {
    service.getOrderById(1).subscribe(data => { expect(data.id).toBe(1); done(); });
    const req = httpMock.expectOne('/api/orders/1');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 1, createdAt: new Date().toISOString(), status: 'Processing', total: 10, items: [] });
  });
});
