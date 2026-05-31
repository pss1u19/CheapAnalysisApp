import { setupZoneTestEnv } from 'jest-preset-angular/setup-env/zone';

// Initialises the Angular TestBed environment with zone.js for every test file.
setupZoneTestEnv();

// jsdom (bundled with jest-environment-jsdom 29) predates the ARIAMixin spec, so it
// does not expose the reflected `role` property that real browsers do. spartan-ng's
// BrnSeparator binds to `[role]` on its host, which trips Angular's NG0303
// "unknown property" check under jsdom. Reflect `role` to its attribute to match
// browser behaviour and keep the test output clean.
if (!('role' in Element.prototype)) {
  Object.defineProperty(Element.prototype, 'role', {
    configurable: true,
    get(): string | null {
      return this.getAttribute('role');
    },
    set(value: string | null) {
      if (value === null) {
        this.removeAttribute('role');
      } else {
        this.setAttribute('role', value);
      }
    },
  });
}
