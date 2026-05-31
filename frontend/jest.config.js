const presets = require('jest-preset-angular/presets');

/**
 * Jest replaces the default Karma/Jasmine runner. `createCjsPreset` wires the
 * jest-preset-angular transformer (ngtsc + ts-jest) for a CommonJS test build.
 *
 * @type {import('jest').Config}
 */
module.exports = {
  ...presets.createCjsPreset({ tsconfig: '<rootDir>/tsconfig.spec.json' }),
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: ['<rootDir>/dist/', '<rootDir>/node_modules/', '<rootDir>/.angular/'],
  coverageDirectory: '<rootDir>/coverage',
  collectCoverageFrom: ['src/app/**/*.ts', '!src/app/**/*.spec.ts'],
};
