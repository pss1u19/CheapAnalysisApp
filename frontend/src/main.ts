import { bootstrapApplication } from '@angular/platform-browser';
import * as Sentry from '@sentry/angular';

import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { environment } from './environments/environment';

// Initialise Sentry before bootstrap so even early startup errors are captured (T-017).
// An empty DSN (the default) leaves the SDK uninitialised — a graceful no-op locally.
if (environment.sentry.dsn) {
  Sentry.init({
    dsn: environment.sentry.dsn,
    environment: environment.sentry.environment,
    // Error reporting only for now; T-601 tunes performance tracing + PII scrubbing.
    tracesSampleRate: 0,
  });
}

bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err));
