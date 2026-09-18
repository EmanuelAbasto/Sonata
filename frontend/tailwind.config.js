/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        base: {
          DEFAULT: '#0a0a0a',
          soft: '#141414',
        },
        surface: {
          DEFAULT: '#171717',
          hover: '#262626',   
          raised: '#333333',
        },
        border: {
          DEFAULT: '#2a2a2a',
          soft: '#171717',
        },
        ink: {
          DEFAULT: '#fafafa',
          muted: '#a3a3a3',
          faint: '#525252',   
        },
        accent: {
          DEFAULT: '#2563eb',
          hover: '#1d4ed8',
          soft: 'rgba(37, 99, 235, 0.15)',
        },
        good: {
          DEFAULT: '#10b981', 
          soft: 'rgba(16, 185, 129, 0.15)',
        },
        bad: {
          DEFAULT: '#ef4444', 
          soft: 'rgba(239, 68, 68, 0.15)',
        },
      },
      fontFamily: {
        display: ['"DM Sans"', 'sans-serif'],
        sans: ['"DM Sans"', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      keyframes: {
        'fade-in': {
          '0%': { opacity: 0, transform: 'translateY(4px)' },
          '100%': { opacity: 1, transform: 'translateY(0)' },
        },
        'pulse-ring': {
          '0%': { boxShadow: '0 0 0 0 rgba(37, 99, 235, 0.35)' },
          '100%': { boxShadow: '0 0 0 8px rgba(37, 99, 235, 0)' },
        },
      },
      animation: {
        'fade-in': 'fade-in 0.25s ease-out',
        'pulse-ring': 'pulse-ring 1.4s ease-out infinite',
      },
    },
  },
  plugins: [],
};