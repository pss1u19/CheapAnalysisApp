import { ApplicationConfig, ErrorHandler, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import * as Sentry from '@sentry/angular';

import { routes } from './app.routes';
import { API_BASE_URL } from './api/cheap-analysis.client';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    // Angular Material relies on the animations module; the async variant lazy-loads it.
    provideAnimationsAsync(),
    // Base URL for the NSwag-generated API clients (T-010). Empty string keeps
    // requests same-origin/relative so they go through the dev proxy or gateway;
    // a concrete origin can be injected per environment later.
    { provide: API_BASE_URL, useValue: '' },
    // Routes uncaught Angular errors to Sentry (T-017). Harmless no-op until a DSN
    // is configured, since Sentry.init is skipped in main.ts when the DSN is empty.
    { provide: ErrorHandler, useValue: Sentry.createErrorHandler() },
  ],
};
