import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { CourseGroupService } from './course-group.service';
import { CourseGroup, CourseGroupRequest } from '@core/models/course-group.model';

describe('CourseGroupService', () => {
  let service: CourseGroupService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/course-groups`;

  const sampleCourseGroup: CourseGroup = {
    pkid: 1,
    description: '資料庫',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CourseGroupService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseGroupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll issues GET to the list endpoint', () => {
    service.getAll().subscribe((courseGroups) => expect(courseGroups.length).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sampleCourseGroup]);
  });

  it('query POSTs the filter body', () => {
    service.query({ keyword: '資料庫' }).subscribe((courseGroups) => expect(courseGroups.length).toBe(1));

    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '資料庫' });
    req.flush([sampleCourseGroup]);
  });

  it('getById puts the numeric pkid in the URL (no encoding)', () => {
    service.getById(3).subscribe((courseGroup) => expect(courseGroup.pkid).toBe(1));

    const req = httpMock.expectOne(`${base}/3`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleCourseGroup);
  });

  it('create POSTs the request to the base endpoint', () => {
    const request: CourseGroupRequest = {
      pkid: 0,
      description: '資訊安全',
    };
    service.create(request).subscribe((courseGroup) => expect(courseGroup.pkid).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sampleCourseGroup);
  });

  it('update PUTs the request to the base endpoint', () => {
    const request: CourseGroupRequest = {
      pkid: 1,
      description: '資料庫',
    };
    service.update(request).subscribe((courseGroup) => expect(courseGroup.pkid).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sampleCourseGroup);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(1).subscribe();

    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
