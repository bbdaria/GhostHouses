const STORAGE_KEY = 'gh-theme';

const fallbackTheme = 'dark';

export function getStoredTheme() {
  if (typeof window === 'undefined') {
    return fallbackTheme;
  }
  try {
    return localStorage.getItem(STORAGE_KEY) || fallbackTheme;
  } catch {
    return fallbackTheme;
  }
}

export function applyTheme(theme) {
  const next = theme === 'light' ? 'light' : 'dark';
  const root = document.documentElement;
  root.classList.remove('theme-light', 'theme-dark');
  root.classList.add(`theme-${next}`);
  try {
    localStorage.setItem(STORAGE_KEY, next);
  } catch {
    // Ignore storage errors (e.g., private mode)
  }
  return next;
}

export function initTheme() {
  const theme = getStoredTheme();
  applyTheme(theme);
  return theme;
}
