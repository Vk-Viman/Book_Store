import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { BookDetailComponent } from './book-detail.component';
import { ActivatedRoute } from '@angular/router';
import { Component } from '@angular/core';

@Component({ template: '<app-book-detail></app-book-detail>', standalone: true, imports: [BookDetailComponent] })
class HostComp {}

describe('BookDetailComponent renders', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComp, HttpClientTestingModule], providers: [{ provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (k: string) => '1' } } } }] }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('shows detail and back button after data loads', (done) => {
    const fixture = TestBed.createComponent(HostComp);
    fixture.detectChanges();

    // expect product fetch
    const prodReq = httpMock.expectOne('/api/products/1');
    expect(prodReq.request.method).toBe('GET');
    prodReq.flush({ id: 1, title: 'Book Details', authors: 'A', price: 9.99, stockQty: 5, imageUrl: '', description: 'd', categoryName: 'C', avgRating: 4.5 });

    // expect reviews fetch
    const revReq = httpMock.expectOne('/api/reviews/product/1');
    expect(revReq.request.method).toBe('GET');
    revReq.flush([]);

    // allow change detection to apply
    setTimeout(() => {
      fixture.detectChanges();
      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Back to Books');
      done();
    }, 20);
  });

  it('loads recommendations when user is logged in', (done) => {
    // simulate logged in by setting token
    localStorage.setItem('token', 'fake.jwt.token');
    const fixture = TestBed.createComponent(HostComp);
    fixture.detectChanges();

    // product request
    const prodReq = httpMock.expectOne('/api/products/1');
    prodReq.flush({ id: 1, title: 'Book Details', authors: 'A', price: 9.99, stockQty: 5, imageUrl: '', description: 'd', categoryName: 'C', avgRating: 4.5 });

    // reviews request
    const revReq = httpMock.expectOne('/api/reviews/product/1');
    revReq.flush([]);

    // recommendations request should be made when logged in
    const recReq = httpMock.expectOne('/api/recommendations/me');
    expect(recReq.request.method).toBe('GET');
    recReq.flush({ items: [{ id: 2, title: 'Rec Book', price: 7.5 }] });

    setTimeout(() => {
      fixture.detectChanges();
      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('You might also like');
      // cleanup
      localStorage.removeItem('token');
      done();
    }, 20);
  });
});
