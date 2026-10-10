import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Home } from './home';

describe('Home', () => {
  let backend: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Home],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('loads protected data on request once the user is signed in', async () => {
    const fixture = TestBed.createComponent(Home);
    backend.expectOne('/api/User').flush({
      isAuthenticated: true,
      nameClaimType: 'name',
      roleClaimType: 'roles',
      claims: [{ type: 'name', value: 'Ada Employee' }],
    });
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>('[data-testid="direct-api"]')?.click();
    backend.expectOne('/api/DirectApi').flush(['some data', 'more data']);
    await fixture.whenStable();

    expect(element.querySelector('[data-testid="direct-api-data"]')?.textContent).toContain(
      'more data',
    );
  });

  it('hides the protected call from anonymous visitors', async () => {
    const fixture = TestBed.createComponent(Home);
    backend
      .expectOne('/api/User')
      .flush({ isAuthenticated: false, nameClaimType: '', roleClaimType: '', claims: [] });
    await fixture.whenStable();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="direct-api"]'),
    ).toBeNull();
  });
});
