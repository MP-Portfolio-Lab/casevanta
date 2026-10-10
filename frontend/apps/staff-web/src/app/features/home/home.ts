import { JsonPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideDatabase } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { Session } from '../../core/session';

@Component({
  selector: 'cv-home',
  imports: [JsonPipe, NgIcon, HlmButtonImports, HlmCardImports],
  providers: [provideIcons({ lucideDatabase })],
  templateUrl: './home.html',
})
export class Home {
  private readonly http = inject(HttpClient);
  protected readonly session = inject(Session);
  protected readonly directApiData = signal<readonly string[] | undefined>(undefined);

  protected loadDirectApiData(): void {
    this.http.get<string[]>('/api/DirectApi').subscribe((data) => this.directApiData.set(data));
  }
}
