import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { CourseService } from './course.service';
import { Course, CourseRequest } from '@core/models/course.model';

describe('CourseService', () => {
  let service: CourseService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/courses`;

  const sampleCourse = { pkid: 1, title: 'Azure 基礎', courseId: 'AZ-900' } as Course;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CourseService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll issues GET to the list endpoint', () => {
    service.getAll().subscribe((courses) => expect(courses.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sampleCourse]);
  });

  it('query POSTs the filter body', () => {
    service.query({ keyword: 'Azure', partnerPkid: 1 }).subscribe();
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'Azure', partnerPkid: 1 });
    req.flush([sampleCourse]);
  });

  it('getById puts the numeric pkid in the URL (no encoding)', () => {
    service.getById(3).subscribe();
    const req = httpMock.expectOne(`${base}/3`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleCourse);
  });

  it('create POSTs the request to the base endpoint', () => {
    const request = { pkid: 0, title: 'Kubernetes' } as CourseRequest;
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sampleCourse);
  });

  it('update PUTs the request to the base endpoint', () => {
    const request = { pkid: 1, title: 'Azure 基礎' } as CourseRequest;
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    req.flush(sampleCourse);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(1).subscribe();
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
