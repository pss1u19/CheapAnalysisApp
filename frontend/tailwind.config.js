/**
 * Tailwind CSS configuration.
 *
 * The spartan-ng brain ships an official Tailwind v3 preset that maps every
 * design token (`--background`, `--primary`, `--radius`, ...) to Tailwind
 * colour/utility scales and wires in the `tailwindcss-animate` plugin. The CSS
 * variables those tokens read from are defined in `src/styles.scss`.
 *
 * @type {import('tailwindcss').Config}
 */
module.exports = {
  presets: [require('@spartan-ng/brain/hlm-tailwind-preset')],
  darkMode: 'class',
  content: ['./src/**/*.{html,ts}'],
};
