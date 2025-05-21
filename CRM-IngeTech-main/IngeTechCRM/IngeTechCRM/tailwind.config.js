module.exports = {
    content: [
        './Pages/**/*.cshtml',
        './Views/**/*.cshtml',
        './ wwwroot/**/ *.html',
        './wwwroot/js/**/*.js',
    ],
    theme: {
        extend: {
            colors: {
                customBlue: '#4F719A', // Nombre personalizado para el color
                'brand-blue': '#005da4',
                'gray-dark': '#545454',
                'gray-light': '#a6a6a6',
            },
            fontFamily: {
              sans: ['Rostis Sans Serif', 'ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', '"Noto Sans"', 'sans-serif', '"Apple Color Emoji"', '"Segoe UI Emoji"', '"Segoe UI Symbol"', '"Noto Color Emoji"'],
            },
        },
    },
    plugins: [],
};