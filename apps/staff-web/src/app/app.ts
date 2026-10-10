import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLogIn, lucideLogOut } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { Session } from './core/session';

@Component({
  selector: 'cv-root',
  imports: [RouterOutlet, RouterLink, NgIcon, HlmButtonImports],
  providers: [provideIcons({ lucideLogIn, lucideLogOut })],
  templateUrl: './app.html',
})
export class App {
  protected readonly session = inject(Session);
}
