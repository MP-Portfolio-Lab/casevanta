import { HttpInterceptorFn } from '@angular/common/http';
import { readCookie } from './cookies';

/** Readable cookie in which the BFF hands the antiforgery request token to the SPA. */
export const ANTIFORGERY_REQUEST_TOKEN_COOKIE = 'XSRF-RequestToken';

/** Header in which the BFF expects the request token back. */
export const ANTIFORGERY_HEADER = 'X-XSRF-TOKEN';

/**
 * Echoes the antiforgery request token on every same-origin /api/ call, whether the URL is
 * relative or absolute. The BFF validates it against its HttpOnly __Host- cookie. Requests to
 * any other origin never receive the token.
 */
export const antiforgeryInterceptor: HttpInterceptorFn = (request, next) => {
  const url = new URL(request.url, globalThis.location.origin);
  const isApiCall = url.origin === globalThis.location.origin && url.pathname.startsWith('/api/');

  if (!isApiCall) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: { [ANTIFORGERY_HEADER]: readCookie(ANTIFORGERY_REQUEST_TOKEN_COOKIE) },
    }),
  );
};
