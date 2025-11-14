import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ReviewService } from './review.service';

describe('ReviewService', () => {
  let service: ReviewService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [ReviewService] });
    service = TestBed.inject(ReviewService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should post review', (done) => {
    const payload = { productId: 1, rating: 5, comment: 'Great' };
    service.postReview(payload).subscribe(() => { done(); });
    const req = httpMock.expectOne('/api/reviews');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('should fetch approved reviews', (done) => {
    service.getApprovedForProduct(1).subscribe(list => { expect(Array.isArray(list)).toBeTruthy(); done(); });
    const req = httpMock.expectOne('/api/reviews/product/1');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
