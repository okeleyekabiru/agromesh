/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        forest:  '#0D2B1F',
        canopy:  '#1A4A32',
        leaf:    '#2D7A4F',
        palm:    '#3DAA6A',
        harvest: '#E8A838',
        mist:    '#F0F4F1',
        fog:     '#E2EBE5',
        stone:   '#6B7C72',
        ink:     '#0A1A10',
        data:    '#00C896',
        alert:   '#DC3C3C',
      },
      fontFamily: {
        display: ['Syne', 'sans-serif'],
        body:    ['Inter', 'sans-serif'],
        mono:    ['JetBrains Mono', 'monospace'],
      },
      fontSize: {
        '2xs': '9px',
        'xs':  '10px',
        'sm':  '12px',
        'base':'13px',
        'md':  '15px',
      },
      borderRadius: {
        'sm': '6px',
        'md': '8px',
        'lg': '12px',
        'xl': '16px',
      }
    }
  },
  plugins: []
}
