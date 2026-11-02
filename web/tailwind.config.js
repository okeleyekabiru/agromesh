/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        forest:  '#0D2B1F',
        canopy:  '#1A4A32',
        leaf:    '#0F7A3A',
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
        display: ['Plus Jakarta Sans Variable', 'system-ui', 'sans-serif'],
        body:    ['Plus Jakarta Sans Variable', 'system-ui', 'sans-serif'],
        mono:    ['ui-monospace', 'SFMono-Regular', 'Menlo', 'Consolas', 'monospace'],
      },
      fontSize: {
        '2xs': '11px',
        'xs':  '11px',
        'sm':  '12.5px',
        'base':'14px',
        'md':  '16px',
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
