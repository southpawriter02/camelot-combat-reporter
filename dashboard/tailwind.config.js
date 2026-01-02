/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Damage type colors
        damage: {
          crush: '#8b5cf6',
          slash: '#ef4444',
          thrust: '#f97316',
          heat: '#dc2626',
          cold: '#3b82f6',
          matter: '#84cc16',
          body: '#10b981',
          spirit: '#a855f7',
          energy: '#06b6d4',
        },
        // Marker category colors
        marker: {
          'damage-out': '#ef4444',
          'damage-in': '#f97316',
          'heal-out': '#22c55e',
          'heal-in': '#10b981',
          cc: '#a855f7',
          death: '#dc2626',
        },
        // Performance rating colors
        rating: {
          excellent: '#22c55e',
          good: '#84cc16',
          average: '#eab308',
          below: '#f97316',
          poor: '#ef4444',
        },
      },
    },
  },
  plugins: [],
};
