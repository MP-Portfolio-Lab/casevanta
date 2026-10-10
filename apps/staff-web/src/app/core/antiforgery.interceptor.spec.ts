import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ANTIFORGERY_HEADER, antiforgeryInterceptor } from './antiforgery.interceptor';

describe('antiforgeryInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    document.cookie = 'XSRF-RequestToken=request%2Btoken; path=/';
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([antiforgeryInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    backend.verify();
    document.cookie = 'XSRF-RequestToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
  });

  it('adds the request token to relative /api/ calls', () => {
    http.get('/api/DirectApi').subscribe();

    expect(backend.expectOne('/api/DirectApi').request.headers.get(ANTIFORGERY_HEADER)).toBe(
      'request+token',
    );
  });

  it('adds the request token to absolute same-origin /api/ calls', () => {
    const url = `${globalThis.location.origin}/api/DirectApi`;
    http.get(url).subscribe();

    expect(backend.expectOne(url).request.headers.get(ANTIFORGERY_HEADER)).toBe('request+token');
  });

  it('never sends the request token to another origin', () => {
    http.get('https://evil.example/api/DirectApi').subscribe();

    expect(
      backend
        .expectOne('https://evil.example/api/DirectApi')
        .request.headers.has(ANTIFORGERY_HEADER),
    ).toBe(false);
  });

  it('does not add the request token to non-API calls', () => {
    http.get('/assets/config.json').subscribe();

    expect(backend.expectOne('/assets/config.json').request.headers.has(ANTIFORGERY_HEADER)).toBe(
      false,
    );
  });
});
