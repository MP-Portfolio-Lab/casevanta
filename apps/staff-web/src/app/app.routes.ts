import { Route } from '@angular/router';
import { Home } from './features/home/home';

export const appRoutes: Route[] = [
  { path: '', component: Home, title: 'Casevanta Staff' },
  { path: '**', redirectTo: '' },
];
