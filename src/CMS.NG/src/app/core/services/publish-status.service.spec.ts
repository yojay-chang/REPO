import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { PublishStatusService } from './publish-status.service';
import { PublishStatus, PublishStatusRequest } from '@core/models/publish-status.model';

describe('PublishStatusService', () => {
  let service: PublishStatusService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/publish-statuses`;

  const sampleStatus: PublishStatus = {
    pkid: 1,
    description: '草稿',
    isDraft: true,
    isPublished: false,
    isDiscontinued: false,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PublishStatusService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PublishStatusService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll issues GET to the list endpoint', () => {
    service.getAll().subscribe((statuses) => expect(statuses.length).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sampleStatus]);
  });

  it('query POSTs the filter body', () => {
    service.query({ isPublished: true }).subscribe((statuses) => expect(statuses.length).toBe(1));

    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ isPublished: true });
    req.flush([sampleStatus]);
  });

  it('getById puts the numeric pkid in the URL', () => {
    service.getById(3).subscribe((status) => expect(status.pkid).toBe(1));

    const req = httpMock.expectOne(`${base}/3`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleStatus);
  });

  it('create POSTs the request to the base endpoint', () => {
    const request: PublishStatusRequest = {
      pkid: 10,
      description: '審核中',
      isDraft: true,
      isPublished: false,
      isDiscontinued: false,
    };
    service.create(request).subscribe((status) => expect(status.pkid).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sampleStatus);
  });

  it('update PUTs the request to the base endpoint', () => {
    const request: PublishStatusRequest = {
      pkid: 1,
      description: '草稿',
      isDraft: true,
      isPublished: false,
      isDiscontinued: false,
    };
    service.update(request).subscribe((status) => expect(status.pkid).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sampleStatus);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(1).subscribe();

    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
