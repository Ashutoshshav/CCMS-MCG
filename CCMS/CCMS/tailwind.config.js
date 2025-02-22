/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './Views/**/*.cshtml', // Razor views
        './wwwroot/**/*.html', // Static HTML files
        './Pages/**/*.cshtml', // For Razor Pages
        './Scripts/**/*.js',  // Any JS files
    ],
  theme: {
    extend: {},
  },
  plugins: [],
}

