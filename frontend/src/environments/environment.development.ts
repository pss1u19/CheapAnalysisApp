// Development overrides, swapped in for environment.ts during `ng serve` (T-017).
export const environment = {
  production: false,
  sentry: {
    // Same project as production but tagged 'development', so local errors are
    // separable in Sentry. Blank this out if you'd rather keep local dev quiet.
    dsn: 'https://850b1538ea4bc2946a3cf76028f87720@o4511509151809537.ingest.de.sentry.io/4511531059839056',
    environment: 'development',
  },
};
