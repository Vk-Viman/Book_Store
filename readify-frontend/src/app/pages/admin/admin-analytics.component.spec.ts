import { TestBed } from '@angular/core/testing';
import { AdminDashboardService } from '../../services/admin-dashboard.service';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

describe('AdminDashboardService', () => {
  let service: AdminDashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [AdminDashboardService] });
    service = TestBed.inject(AdminDashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should fetch summary', (done) => {
    service.getSummary().subscribe(res => {
      expect(res).toBeTruthy();
      expect(res.totalUsers).toBe(2);
      done();
    });

    const req = httpMock.expectOne('/api/admin/analytics/summary');
    expect(req.request.method).toBe('GET');
    req.flush({ totalUsers: 2, totalOrders: 1, totalRevenue: 100 });
  });

  it('should refresh summary', (done) => {
    service.refreshSummary().subscribe(res => {
      expect(res).toBeTruthy();
      expect(res.refreshed).toBe(true);
      done();
    });

    const req = httpMock.expectOne('/api/admin/analytics/refresh');
    expect(req.request.method).toBe('POST');
    req.flush({ refreshed: true });
  });
});
