import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ANTIFORGERY_REQUEST_TOKEN_COOKIE } from './antiforgery.interceptor';
import { readCookie } from './cookies';

export interface ClaimValue {
  readonly type: string;
  readonly value: string;
}

/** The contract of the BFF's GET /api/User. */
export interface UserProfile {
  readonly isAuthenticated: boolean;
  readonly nameClaimType: string;
  readonly roleClaimType: string;
  readonly claims: readonly ClaimValue[];
}

/**
 * The signed-in user as the BFF reports it. The SPA never handles tokens: the session lives in
 * the BFF's HttpOnly cookie.
 */
@Injectable({ providedIn: 'root' })
export class Session {
  private readonly http = inject(HttpClient);

  /** Undefined until the BFF answers. */
  readonly profile = toSignal(this.http.get<UserProfile>('/api/User'));

  readonly isAuthenticated = computed(() => this.profile()?.isAuthenticated === true);

  readonly displayName = computed(() => {
    const profile = this.profile();
    return profile?.claims.find((claim) => claim.type === profile.nameClaimType)?.value ?? '';
  });

  /** The antiforgery request token that the logout form posts back. */
  requestToken(): string {
    return readCookie(ANTIFORGERY_REQUEST_TOKEN_COOKIE);
  }
}
