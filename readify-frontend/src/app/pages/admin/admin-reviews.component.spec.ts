import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdminReviewsComponent } from './admin-reviews.component';
import { NotificationService } from '../../services/notification.service';
import { of } from 'rxjs';

class MockNotify { success(msg: string) {} error(msg: string) {} }

describe('AdminReviewsComponent (bulk)', () => {
  let fixture: any;
  let comp: AdminReviewsComponent;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AdminReviewsComponent, HttpClientTestingModule], providers: [{ provide: NotificationService, useClass: MockNotify }] }).compileComponents();
    const fixtureRef = TestBed.createComponent(AdminReviewsComponent);
    fixture = fixtureRef;
    comp = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('calls bulk endpoint and refreshes list', (done) => {
    // simulate initial load response
    const req = httpMock.expectOne('/api/admin/reviews?page=1&pageSize=10');
    req.flush({ items: [{ id: 1, productId: 1, userId: 2, rating: 5, comment: 'x' }], total: 1, page: 1, pageSize: 10 });

    // select an id and call bulkApprove
    comp.selectedIds.add(1);
    comp.bulkApprove();

    // expect bulk post
    const post = httpMock.expectOne('/api/admin/reviews/bulk');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({ ids: [1], approve: true });
    post.flush({ updated: 1 });

    // after success, a reload call is made
    const reload = httpMock.expectOne('/api/admin/reviews?page=1&pageSize=10');
    reload.flush({ items: [], total: 0, page: 1, pageSize: 10 });

    setTimeout(() => { expect(comp.selectedIds.size).toBe(0); done(); }, 10);
  });
});
