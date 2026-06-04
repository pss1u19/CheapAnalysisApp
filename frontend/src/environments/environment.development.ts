// Development overrides, swapped in for environment.ts during `ng serve` (T-017).
export const environment = {
  production: false,
  sentry: {
    // Leave empty to keep local dev quiet; set a DSN to exercise Sentry locally.
    dsn: '',
    environment: 'development',
  },
};
