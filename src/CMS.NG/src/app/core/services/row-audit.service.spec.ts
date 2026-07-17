import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { RowAuditService } from './row-audit.service';
import { RowAuditEntry } from '@core/models/row-audit.model';

describe('RowAuditService', () => {
  let service: RowAuditService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/rowaudit`;

  const sample: RowAuditEntry = {
    dateTime: '2026-06-04T14:30:00',
    userName: 'alice',
    actionType: 'Update',
    actionDesc: 'Title',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RowAuditService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RowAuditService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getForRecord GETs the endpoint with tableName + pkid query params', () => {
    service.getForRecord('Course', 123).subscribe((entries) => expect(entries.length).toBe(1));

    const req = httpMock.expectOne((r) => r.url === base);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('tableName')).toBe('Course');
    expect(req.request.params.get('pkid')).toBe('123');
    req.flush([sample]);
  });
});
