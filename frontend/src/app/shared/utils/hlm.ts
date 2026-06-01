import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Conditionally join class names (via clsx) and de-duplicate conflicting
 * Tailwind utilities (via tailwind-merge). This is the helper every spartan-ng
 * `helm` component uses to compose its host classes.
 *
 * @param classes class-name fragments — strings, arrays, or condition maps
 * @returns the merged, conflict-free class string
 */
export function hlm(...classes: ClassValue[]): string {
  return twMerge(clsx(classes));
}
