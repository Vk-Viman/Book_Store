import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdminUsersService } from './admin-users.service';

describe('AdminUsersService', () => {
  let service: AdminUsersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [AdminUsersService] });
    service = TestBed.inject(AdminUsersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should call list endpoint', (done) => {
    service.list(1, 10, 'q').subscribe(res => { expect(res).toBeTruthy(); done(); });
    const req = httpMock.expectOne(r => r.url === '/api/admin/users' && r.params.get('q') === 'q');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0 });
  });

  it('should call update endpoint', (done) => {
    service.update(1, { fullName: 'X' }).subscribe(res => { done(); });
    const req = httpMock.expectOne('/api/admin/users/1');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });
});
