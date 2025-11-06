import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdminDashboardService } from './admin-dashboard.service';

describe('AdminDashboardService', () => {
  let service: AdminDashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [AdminDashboardService] });
    service = TestBed.inject(AdminDashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch stats', (done) => {
    service.getStats().subscribe((data: any) => { expect(data.totalUsers).toBe(2); done(); });
    const req = httpMock.expectOne('/api/admin/stats');
    expect(req.request.method).toBe('GET');
    req.flush({ totalUsers: 2, totalOrders: 1, totalSales: 10 });
  });

  it('should fetch top products', (done) => {
    service.getTopProducts().subscribe((data: any) => { expect(data.length).toBe(1); expect(data[0].productName).toBe('P'); done(); });
    const req = httpMock.expectOne('/api/admin/top-products');
    expect(req.request.method).toBe('GET');
    req.flush([{ productName: 'P', quantitySold: 5 }]);
  });
});
