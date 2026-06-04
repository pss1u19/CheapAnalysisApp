// Base (production) environment. `ng build` uses this; the development serve swaps
// in environment.development.ts via the angular.json fileReplacements (T-017).
export const environment = {
  production: true,
  sentry: {
    // Frontend Sentry DSNs are public — they ship in client JS by design. Set the
    // real value per deployment (build-time replacement). Empty disables Sentry.
    dsn: '',
    environment: 'production',
  },
};
