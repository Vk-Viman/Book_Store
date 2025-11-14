import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AdminOrderDetailDialogComponent } from './admin-order-detail-dialog.component';

describe('AdminOrderDetailDialogComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, AdminOrderDetailDialogComponent],
      providers: [
        { provide: MatDialogRef, useValue: { close: () => {} } },
        { provide: MAT_DIALOG_DATA, useValue: { id: 1, userId: 2 } }
      ]
    }).compileComponents();
  });

  beforeEach(() => { httpMock = TestBed.inject(HttpTestingController); });

  afterEach(() => httpMock.verify());

  it('loads user info on init', (done) => {
    const fixture = TestBed.createComponent(AdminOrderDetailDialogComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/admin/users/2');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 2, email: 'u@test', fullName: 'User Test', role: 'User' });

    setTimeout(() => { const comp = fixture.componentInstance; expect(comp.user).toBeTruthy(); expect(comp.user.id).toBe(2); done(); }, 10);
  });
});
