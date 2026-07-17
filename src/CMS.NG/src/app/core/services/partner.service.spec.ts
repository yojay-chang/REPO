import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { PartnerService } from './partner.service';
import { Partner, PartnerRequest } from '@core/models/partner.model';

describe('PartnerService', () => {
  let service: PartnerService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/partners`;

  const samplePartner: Partner = {
    pkid: 1,
    name: '微軟',
    appKey: 'MS',
    nameOnPartnerMenu: '微軟原廠課程',
    nameOnCourseDetailPage: '微軟',
    displayOrder: 1,
    imageFilename: 'ms.png',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PartnerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll issues GET to the list endpoint', () => {
    service.getAll().subscribe((partners) => expect(partners.length).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([samplePartner]);
  });

  it('query POSTs the filter body', () => {
    service.query({ keyword: '微軟' }).subscribe((partners) => expect(partners.length).toBe(1));

    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '微軟' });
    req.flush([samplePartner]);
  });

  it('getById puts the numeric pkid in the URL (no encoding)', () => {
    service.getById(3).subscribe((partner) => expect(partner.pkid).toBe(1));

    const req = httpMock.expectOne(`${base}/3`);
    expect(req.request.method).toBe('GET');
    req.flush(samplePartner);
  });

  it('create POSTs the request to the base endpoint', () => {
    const request: PartnerRequest = {
      pkid: 0,
      name: '甲骨文',
      appKey: 'ORCL',
      nameOnPartnerMenu: '甲骨文資料庫課程',
      nameOnCourseDetailPage: '甲骨文',
      displayOrder: 10,
      imageFilename: null,
    };
    service.create(request).subscribe((partner) => expect(partner.pkid).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(samplePartner);
  });

  it('update PUTs the request to the base endpoint', () => {
    const request: PartnerRequest = {
      pkid: 1,
      name: '微軟',
      appKey: 'MS',
      nameOnPartnerMenu: '微軟原廠課程',
      nameOnCourseDetailPage: '微軟',
      displayOrder: 1,
      imageFilename: 'ms.png',
    };
    service.update(request).subscribe((partner) => expect(partner.pkid).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(samplePartner);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(1).subscribe();

    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
