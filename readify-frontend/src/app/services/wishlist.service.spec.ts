import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { WishlistService } from './wishlist.service';

describe('WishlistService', () => {
  let service: WishlistService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [WishlistService] });
    service = TestBed.inject(WishlistService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should get wishlist', (done) => {
    service.getMyWishlist().subscribe(data => { expect(Array.isArray(data)).toBeTruthy(); done(); });
    const req = httpMock.expectOne('/api/wishlist');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should add and remove', (done) => {
    service.addToWishlist(1).subscribe(() => {
      service.removeFromWishlist(1).subscribe(() => done());
    });
    const req = httpMock.expectOne('/api/wishlist/1');
    expect(req.request.method).toBe('POST');
    req.flush({});
    const del = httpMock.expectOne('/api/wishlist/1');
    expect(del.request.method).toBe('DELETE');
    del.flush({});
  });
});
