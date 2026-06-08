// Base (production) environment. `ng build` uses this; the development serve swaps
// in environment.development.ts via the angular.json fileReplacements (T-017).
export const environment = {
  production: true,
  sentry: {
    // Frontend Sentry DSNs are public — they ship in client JS by design, so this
    // is committed intentionally. Empty would disable Sentry.
    dsn: 'https://850b1538ea4bc2946a3cf76028f87720@o4511509151809537.ingest.de.sentry.io/4511531059839056',
    environment: 'production',
  },
};
