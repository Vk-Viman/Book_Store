import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { UsersService } from './users.service';

describe('UsersService', () => {
  let service: UsersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [UsersService] });
    service = TestBed.inject(UsersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should get profile', (done) => {
    service.getProfile().subscribe(res => { expect(res).toBeTruthy(); done(); });
    const req = httpMock.expectOne('/api/users/me');
    expect(req.request.method).toBe('GET');
    req.flush({ fullName: 'X' });
  });

  it('should change password', (done) => {
    service.changePassword({ currentPassword: 'a', newPassword: 'b' }).subscribe(() => done());
    const req = httpMock.expectOne('/api/users/change-password');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });
});
