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
            },
        },
    },
    plugins: [],
};