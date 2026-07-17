import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { AppUserService } from './app-user.service';
import { AppUser, AppUserRequest } from '@core/models/app-user.model';

describe('AppUserService', () => {
  let service: AppUserService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/app-users`;

  const sampleUser: AppUser = {
    pkid: 1,
    userId: 'helen',
    userName: 'Helen Wang',
    isActive: true,
    passwordUpdatedTime: '2026-01-01T09:00:00',
    roleCount: 2,
    roleIds: ['Admin', 'User'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AppUserService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppUserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll issues GET to the list endpoint', () => {
    service.getAll().subscribe((users) => expect(users.length).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sampleUser]);
  });

  it('query POSTs the filter body', () => {
    service.query({ keyword: 'hel' }).subscribe((users) => expect(users.length).toBe(1));

    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'hel' });
    req.flush([sampleUser]);
  });

  it('getById encodes the user id in the URL', () => {
    service.getById('a/b').subscribe((user) => expect(user.userId).toBe('helen'));

    const req = httpMock.expectOne(`${base}/a%2Fb`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleUser);
  });

  it('create POSTs the request to the base endpoint', () => {
    const request: AppUserRequest = {
      userId: 'jenny',
      userName: 'Jenny Tsao',
      isActive: true,
      roleIds: ['User'],
    };
    service.create(request).subscribe((user) => expect(user.userId).toBe('helen'));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sampleUser);
  });

  it('update PUTs the request to the base endpoint', () => {
    const request: AppUserRequest = {
      userId: 'helen',
      userName: 'Helen Wang',
      isActive: true,
      roleIds: ['Admin'],
    };
    service.update(request).subscribe((user) => expect(user.userId).toBe('helen'));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sampleUser);
  });

  it('delete issues DELETE with an encoded id', () => {
    service.delete('a b').subscribe();

    const req = httpMock.expectOne(`${base}/a%20b`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('resetPassword POSTs the target userId to the Auth reset-password endpoint', () => {
    service.resetPassword('a/b').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Auth/reset-password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'a/b' });
    req.flush(null);
  });

  it('getAppRoles GETs the lookup endpoint', () => {
    service.getAppRoles().subscribe((roles) => expect(roles.length).toBe(1));

    const req = httpMock.expectOne(`${environment.apiUrl}/lookups/app-roles`);
    expect(req.request.method).toBe('GET');
    req.flush([{ roleId: 'Admin', roleName: 'Administrator' }]);
  });
});
