import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { UserProfile } from './core/session';

const anonymous: UserProfile = {
  isAuthenticated: false,
  nameClaimType: '',
  roleClaimType: '',
  claims: [],
};

const signedIn: UserProfile = {
  isAuthenticated: true,
  nameClaimType: 'name',
  roleClaimType: 'roles',
  claims: [{ type: 'name', value: 'Ada Employee' }],
};

describe('App shell', () => {
  let backend: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  async function render(profile: UserProfile): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(App);
    backend.expectOne('/api/User').flush(profile);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('offers the BFF login link to an anonymous visitor', async () => {
    const element = await render(anonymous);

    expect(element.querySelector('a[href="api/Account/Login"]')).not.toBeNull();
    expect(element.querySelector('form[action="api/Account/Logout"]')).toBeNull();
  });

  it('shows the signed-in user and a logout form that posts the antiforgery token', async () => {
    document.cookie = 'XSRF-RequestToken=request-token; path=/';
    try {
      const element = await render(signedIn);

      expect(element.querySelector('[data-testid="user-name"]')?.textContent?.trim()).toBe(
        'Ada Employee',
      );
      const form = element.querySelector<HTMLFormElement>('form[action="api/Account/Logout"]');
      expect(form?.method).toBe('post');
      expect(
        form?.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value,
      ).toBe('request-token');
      expect(element.querySelector('a[href="api/Account/Login"]')).toBeNull();
    } finally {
      document.cookie = 'XSRF-RequestToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
    }
  });
});
