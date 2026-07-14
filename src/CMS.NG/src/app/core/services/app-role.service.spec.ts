import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { AppRoleService } from './app-role.service';
import { AppRole, AppRoleRequest } from '@core/models/app-role.model';

describe('AppRoleService', () => {
  let service: AppRoleService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/app-roles`;

  const sampleRole: AppRole = {
    pkid: 1,
    roleId: 'Admin',
    roleName: 'Administrator',
    permissionLevel: 1,
    description: '系統管理員',
    userCount: 3,
    userIds: ['helen', 'miles'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AppRoleService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppRoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll issues GET to the list endpoint', () => {
    service.getAll().subscribe((roles) => expect(roles.length).toBe(1));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sampleRole]);
  });

  it('query POSTs the filter body', () => {
    service.query({ keyword: 'adm' }).subscribe((roles) => expect(roles.length).toBe(1));

    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'adm' });
    req.flush([sampleRole]);
  });

  it('getById encodes the role id in the URL', () => {
    service.getById('A/B').subscribe((role) => expect(role.roleId).toBe('Admin'));

    const req = httpMock.expectOne(`${base}/A%2FB`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleRole);
  });

  it('create POSTs the request to the base endpoint', () => {
    const request: AppRoleRequest = {
      roleId: 'Editor',
      roleName: 'Content Editor',
      permissionLevel: 50,
      description: null,
      userIds: [],
    };
    service.create(request).subscribe((role) => expect(role.roleId).toBe('Admin'));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(sampleRole);
  });

  it('update PUTs the request to the base endpoint', () => {
    const request: AppRoleRequest = {
      roleId: 'Admin',
      roleName: 'Administrator',
      permissionLevel: 1,
      description: '系統管理員',
      userIds: ['helen'],
    };
    service.update(request).subscribe((role) => expect(role.roleId).toBe('Admin'));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sampleRole);
  });

  it('delete issues DELETE with an encoded id', () => {
    service.delete('Admin').subscribe();

    const req = httpMock.expectOne(`${base}/Admin`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('getAppUsers GETs the lookup endpoint', () => {
    service.getAppUsers().subscribe((users) => expect(users.length).toBe(1));

    const req = httpMock.expectOne(`${environment.apiUrl}/lookups/app-users`);
    expect(req.request.method).toBe('GET');
    req.flush([{ userId: 'helen', userName: 'helen' }]);
  });
});
