import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { FeaturedPromoItemService } from './featured-promo-item.service';
import {
  FeaturedPromoItem,
  FeaturedPromoItemRequest,
} from '@core/models/featured-promo-item.model';

describe('FeaturedPromoItemService', () => {
  let service: FeaturedPromoItemService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/featured-promo-items`;

  const sample = { pkid: 1, slot: 1, promoCode: '20251204_SkillTrainAI' } as FeaturedPromoItem;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FeaturedPromoItemService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FeaturedPromoItemService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll issues GET to the list endpoint', () => {
    service.getAll().subscribe((items) => expect(items.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query POSTs the TrainingCenter + week filter body', () => {
    const filter = {
      trainingCenterPkid: 1,
      scheduleOnFrom: '2026-03-16',
      scheduleOnTo: '2026-03-22',
    };
    service.query(filter).subscribe();
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sample]);
  });

  it('getById puts the numeric pkid in the URL (no encoding)', () => {
    service.getById(3).subscribe();
    const req = httpMock.expectOne(`${base}/3`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create POSTs the request to the base endpoint', () => {
    const request = { pkid: 0, slot: 1, promotionPkid: 1 } as FeaturedPromoItemRequest;
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sample);
  });

  it('update PUTs the request to the base endpoint', () => {
    const request = { pkid: 1, slot: 2, promotionPkid: 3 } as FeaturedPromoItemRequest;
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sample);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(1).subscribe();
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
