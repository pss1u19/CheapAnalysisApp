import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { BrnSeparator } from '@spartan-ng/brain/separator';

import { hlm } from './shared/utils/hlm';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MatButtonModule, BrnSeparator],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  protected readonly title = 'CheapAnalysis';

  // Demonstrates the spartan-ng `hlm` class-merge helper composing Tailwind tokens.
  protected readonly cardClass = hlm(
    'rounded-lg border border-border bg-card p-6 text-card-foreground shadow-xs',
  );
}
