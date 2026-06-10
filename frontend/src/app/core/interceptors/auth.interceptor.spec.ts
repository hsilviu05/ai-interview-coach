import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { authInterceptor } from './auth.interceptor';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
  });

  it('attaches withCredentials to requests targeting the API base URL', () => {
    http.get(`${environment.apiBaseUrl}/auth/me`).subscribe();

    const req = controller.expectOne(`${environment.apiBaseUrl}/auth/me`);

    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('attaches withCredentials to any sub-path under the API base URL', () => {
    http.post(`${environment.apiBaseUrl}/submissions`, {}).subscribe();

    const req = controller.expectOne(`${environment.apiBaseUrl}/submissions`);

    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('does not attach withCredentials to requests outside the API base URL', () => {
    http.get('https://external.example.com/data').subscribe();

    const req = controller.expectOne('https://external.example.com/data');

    expect(req.request.withCredentials).toBe(false);
    req.flush({});
  });

  it('does not attach withCredentials to relative URLs that do not start with the API base', () => {
    http.get('/some/local/path').subscribe();

    const req = controller.expectOne('/some/local/path');

    expect(req.request.withCredentials).toBe(false);
    req.flush({});
  });
});
