import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard, guestGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  let loggedIn: boolean;

  const run = (guard: typeof authGuard) =>
    TestBed.runInInjectionContext(() =>
      guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );

  const urlOf = (result: unknown) =>
    TestBed.inject(Router).serializeUrl(result as UrlTree);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: { isLoggedIn: () => loggedIn } }],
    });
  });

  it('lets a logged-in user into the catalog', () => {
    loggedIn = true;
    expect(run(authGuard)).toBe(true);
  });

  it('sends an anonymous user to the login page', () => {
    loggedIn = false;
    expect(urlOf(run(authGuard))).toBe('/login');
  });

  it('keeps a logged-in user away from login and register', () => {
    loggedIn = true;
    expect(urlOf(run(guestGuard))).toBe('/releases');
  });

  it('lets an anonymous user see login and register', () => {
    loggedIn = false;
    expect(run(guestGuard)).toBe(true);
  });
});
