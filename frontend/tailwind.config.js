/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      borderRadius: {
        card: '0.75rem',
        control: '0.5rem',
      },
      boxShadow: {
        card: '0 8px 24px rgb(15 23 42 / 0.08)',
        subtle: '0 1px 2px rgb(15 23 42 / 0.06)',
      },
      colors: {
        border: '#e2e8f0',
        'border-soft': '#f1f5f9',
        brand: '#6366f1',
        'brand-accent': '#a855f7',
        'brand-soft': '#eef2ff',
        'brand-strong': '#4f46e5',
        canvas: '#f7f8f5',
        ink: '#172b35',
        muted: '#64748b',
        'on-brand': '#ffffff',
        surface: '#ffffff',
      },
      fontFamily: {
        sans: ['Be Vietnam Pro', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};
