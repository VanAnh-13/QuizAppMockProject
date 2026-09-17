/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      borderRadius: {
        card: 'var(--glass-radius)',
        control: '0.5rem',
      },
      boxShadow: {
        card: 'var(--glass-shadow)',
        subtle: '0 1px 2px rgb(15 23 42 / 0.06)',
      },
      colors: {
        border: 'rgba(100, 116, 139, 0.2)',
        'border-soft': 'rgba(100, 116, 139, 0.12)',
        brand: '#2563eb',
        'brand-accent': '#38bdf8',
        'brand-soft': '#eff6ff',
        'brand-strong': '#1d4ed8',
        canvas: '#f4f7fc',
        ink: '#172033',
        muted: '#536078',
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
