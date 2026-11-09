import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './auth/auth.guards';
import { Shell } from './shell';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Log in · Record Label Catalog',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Create account · Record Label Catalog',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/register').then((m) => m.Register),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'releases' },
      {
        path: 'releases',
        title: 'Releases · Record Label Catalog',
        loadComponent: () => import('./releases/release-list').then((m) => m.ReleaseList),
      },
      {
        path: 'releases/new',
        title: 'New release · Record Label Catalog',
        loadComponent: () => import('./releases/release-form').then((m) => m.ReleaseForm),
      },
      {
        path: 'releases/:id/edit',
        title: 'Edit release · Record Label Catalog',
        loadComponent: () => import('./releases/release-form').then((m) => m.ReleaseForm),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
