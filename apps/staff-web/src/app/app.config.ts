import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, CSP_NONCE, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideSpartanHlm } from '@spartan-ng/helm/utils';
import { appRoutes } from './app.routes';
import { antiforgeryInterceptor } from './core/antiforgery.interceptor';

/** Angular applies this nonce to the styles it injects, which keeps them inside the BFF's CSP. */
const cspNonce = document.querySelector<HTMLMetaElement>('meta[name="CSP_NONCE"]')?.content;

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(appRoutes),
    provideHttpClient(withFetch(), withInterceptors([antiforgeryInterceptor])),
    provideSpartanHlm(),
    { provide: CSP_NONCE, useValue: cspNonce },
  ],
};
