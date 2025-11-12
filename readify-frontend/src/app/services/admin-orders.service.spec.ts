import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdminOrdersService } from './admin-orders.service';

describe('AdminOrdersService', () => {
  let service: AdminOrdersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [AdminOrdersService] });
    service = TestBed.inject(AdminOrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should call list endpoint', (done) => {
    service.list(1, 20, 'Processing', 'q').subscribe(res => { expect(res).toBeTruthy(); done(); });
    const req = httpMock.expectOne(r => r.url === '/api/admin/orders' && r.params.get('status') === 'Processing' && r.params.get('q') === 'q');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0 });
  });

  it('should call updateStatus endpoint', (done) => {
    service.updateStatus(5, { orderStatus: 'Shipped' }).subscribe(res => { done(); });
    const req = httpMock.expectOne('/api/admin/orders/update-status/5');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });
});
