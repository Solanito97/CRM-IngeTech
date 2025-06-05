$(document).ready(function () {
    // Crear modal de error si no existe
    if ($('#errorModal').length === 0) {
        $('body').append(`
            <div id="errorModal" class="hidden fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
                <div class="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                    <div class="mt-3 text-center">
                        <div class="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100">
                            <svg class="h-6 w-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                        </div>
                        <h3 class="text-lg leading-6 font-medium text-gray-900 mt-3">Error de validación</h3>
                        <div class="mt-2 px-7 py-3">
                            <p id="errorMessage" class="text-sm text-gray-500"></p>
                        </div>
                        <div class="items-center px-4 py-3">
                            <button id="closeErrorModal" class="px-4 py-2 bg-red-500 text-white text-base font-medium rounded-md w-full shadow-sm hover:bg-red-600 focus:outline-none focus:ring-2 focus:ring-red-300">
                                Cerrar
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `);

        // Evento para cerrar el modal
        $('#closeErrorModal').on('click', function () {
            $('#errorModal').addClass('hidden');
        });
    }

    // Función para mostrar errores en modal
    function showError(message) {
        $('#errorMessage').text(message);
        $('#errorModal').removeClass('hidden');
    }

    // Función para validar fortaleza de contraseña
    function validarFortalezaContrasena(contrasena) {
        // Mínimo 6 caracteres
        if (contrasena.length < 6) {
            return { valido: false, mensaje: "La contraseña debe tener al menos 6 caracteres" };
        }

        // Debe contener al menos una letra
        if (!/[a-zA-Z]/.test(contrasena)) {
            return { valido: false, mensaje: "La contraseña debe incluir al menos una letra" };
        }

        // Debe contener al menos un número
        if (!/\d/.test(contrasena)) {
            return { valido: false, mensaje: "La contraseña debe incluir al menos un número" };
        }

        return { valido: true, mensaje: "" };
    }

    // Validación de contraseñas en tiempo real
    $('#Contrasena').on('keyup', function () {
        var contrasena = $(this).val();
        var resultado = validarFortalezaContrasena(contrasena);

        // Obtener el contenedor del mensaje (lo agregamos en el HTML ahora)
        var $feedbackContainer = $(this).closest('div.relative').next('.password-feedback');

        if (contrasena.length > 0) {
            if (resultado.valido) {
                $(this).removeClass('border-red-500').addClass('border-green-500');
                $feedbackContainer.find('#password-strength').text('').removeClass('text-red-500 text-green-500');
            } else {
                $(this).removeClass('border-green-500').addClass('border-red-500');
                $feedbackContainer.find('#password-strength').text(resultado.mensaje)
                    .removeClass('text-green-500')
                    .addClass('text-red-500');
            }
        } else {
            $(this).removeClass('border-red-500 border-green-500');
            $feedbackContainer.find('#password-strength').text('');
        }
    });

    // Validación de coincidencia de contraseñas - VERSIÓN CORREGIDA
    $('#confirmarContrasena').on('keyup', function () {
        var contrasena = $('#Contrasena').val();
        var confirmarContrasena = $(this).val();
        var $feedbackContainer = $(this).closest('div.relative').next('.password-feedback');

        if (confirmarContrasena.length > 0) {
            if (contrasena === confirmarContrasena) {
                $(this).removeClass('border-red-500').addClass('border-green-500');
                $feedbackContainer.find('#password-match').text('Las contraseñas coinciden')
                    .removeClass('text-red-500')
                    .addClass('text-green-500');
            } else {
                $(this).removeClass('border-green-500').addClass('border-red-500');
                $feedbackContainer.find('#password-match').text('Las contraseñas no coinciden')
                    .removeClass('text-green-500')
                    .addClass('text-red-500');
            }
        } else {
            $(this).removeClass('border-red-500 border-green-500');
            $feedbackContainer.find('#password-match').text('');
        }
    });

    // Agregar elementos HTML para mostrar mensajes
    if ($('#password-feedback').length === 0) {
        $('#Contrasena').after('<div id="password-feedback" class="mt-1 text-sm"><div id="password-strength"></div><div id="password-match" class="mt-1"></div></div>');
    }

    // Validación del campo teléfono - solo números
    $('#Telefono').on('input', function () {
        var telefono = $(this).val();
        // Remover cualquier carácter que no sea número
        var telefonoLimpio = telefono.replace(/[^\d]/g, '');
        $(this).val(telefonoLimpio);

        // Limitar a 8 dígitos (ajusta según tu país)
        if (telefonoLimpio.length > 8) {
            $(this).val(telefonoLimpio.substring(0, 8));
        }
    });

    // Validación del campo nombre completo - solo letras, tildes y espacios
    $('#NombreCompleto').on('input', function () {
        var nombre = $(this).val();
        // Permitir solo letras (incluye tildes), espacios y algunos caracteres especiales del español
        var nombreLimpio = nombre.replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]/g, '');
        $(this).val(nombreLimpio);

        // Evitar espacios múltiples
        var nombreFormateado = nombreLimpio.replace(/\s+/g, ' ');
        if (nombreFormateado !== nombreLimpio) {
            $(this).val(nombreFormateado);
        }
    });

    // Validación adicional para nombre de usuario - sin espacios
    $('#NombreUsuario').on('input', function () {
        var usuario = $(this).val();
        // Remover espacios del nombre de usuario
        var usuarioLimpio = usuario.replace(/\s/g, '');
        $(this).val(usuarioLimpio);
    });

    // Validación del campo identificación - solo números
    $('#Identificacion').on('input', function () {
        var identificacion = $(this).val();
        // Remover cualquier carácter que no sea número
        var identificacionLimpia = identificacion.replace(/[^\d]/g, '');
        $(this).val(identificacionLimpia);

        // Limitar a 9 dígitos para cédula costarricense (ajusta según tu país)
        if (identificacionLimpia.length > 9) {
            $(this).val(identificacionLimpia.substring(0, 9));
        }
    });

    // Prevenir pegado de contenido no válido en teléfono
    $('#Telefono').on('paste', function (e) {
        setTimeout(() => {
            var telefono = $(this).val();
            var telefonoLimpio = telefono.replace(/[^\d]/g, '');
            $(this).val(telefonoLimpio.substring(0, 8));
        }, 10);
    });

    // Prevenir pegado de contenido no válido en nombre completo
    $('#NombreCompleto').on('paste', function (e) {
        setTimeout(() => {
            var nombre = $(this).val();
            var nombreLimpio = nombre.replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]/g, '');
            var nombreFormateado = nombreLimpio.replace(/\s+/g, ' ');
            $(this).val(nombreFormateado);
        }, 10);
    });

    // Prevenir pegado de contenido no válido en identificación
    $('#Identificacion').on('paste', function (e) {
        setTimeout(() => {
            var identificacion = $(this).val();
            var identificacionLimpia = identificacion.replace(/[^\d]/g, '');
            $(this).val(identificacionLimpia.substring(0, 9));
        }, 10);
    });

    // Formatear nombre completo para capitalizar primera letra de cada palabra
    $('#NombreCompleto').on('blur', function () {
        var nombre = $(this).val().trim();
        if (nombre) {
            var nombreFormateado = nombre.toLowerCase().replace(/\b\w/g, function (letter) {
                return letter.toUpperCase();
            });
            $(this).val(nombreFormateado);
        }
    });

    // Validación del formulario al enviar
    $('form').submit(function (event) {
        var contrasena = $('#Contrasena').val();
        var confirmarContrasena = $('#confirmarContrasena').val();
        var nombreCompleto = $('#NombreCompleto').val().trim();
        var telefono = $('#Telefono').val();
        var identificacion = $('#Identificacion').val();

        // Validar fortaleza de contraseña
        var validacionContrasena = validarFortalezaContrasena(contrasena);
        if (!validacionContrasena.valido) {
            event.preventDefault();
            showError(validacionContrasena.mensaje);
            $('#Contrasena').focus();
            return false;
        }

        // Validar coincidencia de contraseñas
        if (contrasena !== confirmarContrasena) {
            event.preventDefault();
            showError('Las contraseñas no coinciden');
            return false;
        }

        // Validar nombre completo
        if (nombreCompleto.length < 2) {
            event.preventDefault();
            showError('El nombre completo debe tener al menos 2 caracteres');
            $('#NombreCompleto').focus();
            return false;
        }

        // Validar que el nombre no contenga solo espacios
        if (!/[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ]/.test(nombreCompleto)) {
            event.preventDefault();
            showError('El nombre completo debe contener al menos una letra válida');
            $('#NombreCompleto').focus();
            return false;
        }

        // Validar teléfono (opcional, si se llena)
        if (telefono && telefono.length < 8) {
            event.preventDefault();
            showError('El teléfono debe tener al menos 8 dígitos');
            $('#Telefono').focus();
            return false;
        }

        // Validar identificación
        if (identificacion.length < 9) {
            event.preventDefault();
            showError('La identificación debe tener 9 dígitos');
            $('#Identificacion').focus();
            return false;
        }
    });
});