import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: { token: () => string | null; logout: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    auth = { token: () => 'jwt', logout: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('adds the bearer token to API requests', () => {
    http.get(`${environment.apiUrl}/releases`).subscribe();

    const request = backend.expectOne(`${environment.apiUrl}/releases`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt');
    request.flush({});
  });

  it('does not send the token to other hosts', () => {
    http.get('https://example.com/data').subscribe();

    const request = backend.expectOne('https://example.com/data');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('sends no header when logged out', () => {
    auth.token = () => null;
    http.get(`${environment.apiUrl}/releases`).subscribe();

    const request = backend.expectOne(`${environment.apiUrl}/releases`);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('logs out when the API rejects the token', () => {
    http.get(`${environment.apiUrl}/releases`).subscribe({ error: () => {} });

    backend
      .expectOne(`${environment.apiUrl}/releases`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).toHaveBeenCalled();
  });

  it('does not log out on other errors', () => {
    http.get(`${environment.apiUrl}/releases`).subscribe({ error: () => {} });

    backend
      .expectOne(`${environment.apiUrl}/releases`)
      .flush(null, { status: 500, statusText: 'Server Error' });

    expect(auth.logout).not.toHaveBeenCalled();
  });
});
