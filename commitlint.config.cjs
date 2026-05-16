// commitlint.config.cjs
// Enforces Conventional Commits: https://www.conventionalcommits.org/
// Run manually: npx commitlint --from HEAD~1
// Hooked via Husky in T-016.

/** @type {import('@commitlint/types').UserConfig} */
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    // Allowed types — keep in sync with CHANGELOG tooling
    'type-enum': [
      2,
      'always',
      [
        'feat',     // new feature
        'fix',      // bug fix
        'docs',     // documentation only
        'style',    // formatting, no logic change
        'refactor', // neither feat nor fix
        'perf',     // performance improvement
        'test',     // adding or correcting tests
        'build',    // build system / external deps
        'ci',       // CI configuration
        'chore',    // other housekeeping
        'revert',   // revert a previous commit
      ],
    ],
    // Scopes map to project areas
    'scope-enum': [
      1, // warn — don't block, but nudge toward consistency
      'always',
      [
        'backend',
        'frontend',
        'sidecar',
        'proto',
        'db',
        'ci',
        'docker',
        'docs',
        'deps',
        'auth',
        'bank',
        'ibkr',
        'ai',
        'release',
      ],
    ],
    'subject-case': [2, 'never', ['sentence-case', 'start-case', 'pascal-case', 'upper-case']],
    'subject-full-stop': [2, 'never', '.'],
    'header-max-length': [2, 'always', 100],
    'body-max-line-length': [2, 'always', 100],
  },
};
