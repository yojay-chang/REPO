import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { environment } from '@environments/environment';
import { Login } from './login';

describe('Login', () => {
  let router: { navigate: jasmine.Spy };
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    router = { navigate: jasmine.createSpy('navigate') };

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        { provide: Router, useValue: router },
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('posts credentials and navigates home on success', () => {
    const fixture = TestBed.createComponent(Login);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ userId: 'helen', password: 'secret123' });
    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/Auth/login`);
    expect(req.request.body).toEqual({ userId: 'helen', password: 'secret123' });
    req.flush({ userId: 'helen', userName: 'Helen Wang', accessToken: 'a.b.c' });

    expect(router.navigate).toHaveBeenCalledWith(['/']);
    expect(sessionStorage.getItem('cms.auth')).not.toBeNull();
  });

  it('shows an error and does not navigate on 401', () => {
    const fixture = TestBed.createComponent(Login);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ userId: 'helen', password: 'wrong' });
    component.submit();

    httpMock
      .expectOne(`${environment.apiUrl}/Auth/login`)
      .flush('invalid credentials', { status: 401, statusText: 'Unauthorized' });

    expect(component.errorMessage()).toContain('invalid credentials');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not submit when the form is empty', () => {
    const fixture = TestBed.createComponent(Login);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.submit();

    httpMock.expectNone(`${environment.apiUrl}/Auth/login`);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
