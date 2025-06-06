// wwwroot/js/usuario.js - Versión corregida para móviles

// Función para prevenir la entrada de números en tiempo real (solo letras y tildes)
function soloLetrasYTildes(event) {
    const char = String.fromCharCode(event.which);
    // Permite letras (a-z, A-Z), espacios, tildes y caracteres especiales del español
    const regex = /^[a-zA-ZÀ-ÿ\u00f1\u00d1\s]$/;

    if (!regex.test(char)) {
        event.preventDefault();
        return false;
    }
    return true;
}

// Función para validar el contenido completo del campo nombre
function validarNombreCompleto(input) {
    const valor = input.value;
    const regex = /^[a-zA-ZÀ-ÿ\u00f1\u00d1\s]*$/;
    const errorDiv = document.getElementById('error-nombre');

    if (!regex.test(valor)) {
        // Remover caracteres no válidos
        input.value = valor.replace(/[^a-zA-ZÀ-ÿ\u00f1\u00d1\s]/g, '');
        if (errorDiv) {
            errorDiv.classList.remove('hidden');
        }
        input.classList.add('border-red-500');
        input.classList.remove('border-gray-300');
    } else {
        if (errorDiv) {
            errorDiv.classList.add('hidden');
        }
        input.classList.remove('border-red-500');
        input.classList.add('border-gray-300');
    }
}

// Función para permitir solo números en keypress
function soloNumeros(event) {
    const char = String.fromCharCode(event.which);

    // Permite números (0-9) y teclas de control (backspace, delete, etc.)
    if (!/[0-9]/.test(char) && event.which !== 8 && event.which !== 0) {
        event.preventDefault();
        return false;
    }
    return true;
}

// Función para validar el contenido completo del campo teléfono
function validarTelefono(input) {
    const valor = input.value;
    const regex = /^[0-9]*$/;
    const errorDiv = document.getElementById('error-telefono');

    if (!regex.test(valor)) {
        // Remover caracteres no válidos (mantener solo números)
        input.value = valor.replace(/[^0-9]/g, '');
        if (errorDiv) {
            errorDiv.classList.remove('hidden');
        }
        input.classList.add('border-red-500');
        input.classList.remove('border-gray-300');
    } else {
        if (errorDiv) {
            errorDiv.classList.add('hidden');
        }
        input.classList.remove('border-red-500');
        input.classList.add('border-gray-300');
    }
}

// Función para validar contenido pegado en campo de teléfono
function validarPegado(event) {
    // Prevenir el pegado por defecto
    event.preventDefault();

    // Obtener el texto pegado
    const paste = (event.clipboardData || window.clipboardData).getData('text');

    // Filtrar solo números del texto pegado
    const soloNumeros = paste.replace(/[^0-9]/g, '');

    // Insertar solo los números en el campo
    const input = event.target;
    const start = input.selectionStart;
    const end = input.selectionEnd;
    const valorActual = input.value;

    input.value = valorActual.substring(0, start) + soloNumeros + valorActual.substring(end);

    // Validar el resultado
    validarTelefono(input);

    return false;
}

// ===== VALIDACIONES ADICIONALES PARA FORMULARIO DE PERFIL =====

// Función para validar campos vacíos
function validarCampoVacio(elementoId, errorId, mensajeError) {
    const elemento = document.getElementById(elementoId);
    const errorDiv = document.getElementById(errorId);

    if (!elemento) return true;

    const valor = elemento.value.trim();

    if (valor === '') {
        elemento.classList.add('border-red-500');
        elemento.classList.remove('border-gray-300');
        if (errorDiv) {
            if (mensajeError) errorDiv.textContent = mensajeError;
            errorDiv.classList.remove('hidden');
        }
        return false;
    } else {
        elemento.classList.remove('border-red-500');
        elemento.classList.add('border-gray-300');
        if (errorDiv) {
            errorDiv.classList.add('hidden');
        }
        return true;
    }
}

// Función para validar longitud mínima
function validarLongitudMinima(elementoId, longitudMinima, mensajeError) {
    const elemento = document.getElementById(elementoId);
    if (!elemento) return true;

    const valor = elemento.value.trim();
    const errorDiv = elemento.parentElement.querySelector('.text-red-600');

    if (valor.length > 0 && valor.length < longitudMinima) {
        elemento.classList.add('border-red-500');
        elemento.classList.remove('border-gray-300');
        if (errorDiv && mensajeError) {
            errorDiv.textContent = mensajeError;
            errorDiv.classList.remove('hidden');
        }
        return false;
    }
    return true;
}

// Función para inicializar validaciones del formulario de perfil
function inicializarValidacionesPerfil() {
    // Validación de nombre de usuario
    const nombreUsuario = document.getElementById('NombreUsuario');
    if (nombreUsuario) {
        nombreUsuario.addEventListener('blur', function () {
            validarCampoVacio('NombreUsuario', 'error-nombreUsuario', 'El nombre de usuario es obligatorio');
            validarLongitudMinima('NombreUsuario', 3, 'El nombre de usuario debe tener al menos 3 caracteres');
        });
    }

    // Validación de provincia
    const provincia = document.getElementById('IdProvincia');
    if (provincia) {
        provincia.addEventListener('change', function () {
            validarCampoVacio('IdProvincia', 'error-provincia', 'Debe seleccionar una provincia');
        });
    }

    // Validación de dirección
    const direccion = document.getElementById('DireccionCompleta');
    if (direccion) {
        direccion.addEventListener('blur', function () {
            const valor = this.value.trim();
            const errorDiv = document.getElementById('error-direccion');

            if (valor === '') {
                this.classList.add('border-red-500');
                if (errorDiv) {
                    errorDiv.textContent = 'La dirección es obligatoria';
                    errorDiv.classList.remove('hidden');
                }
            } else if (valor.length < 10) {
                this.classList.add('border-red-500');
                if (errorDiv) {
                    errorDiv.textContent = 'La dirección debe tener al menos 10 caracteres';
                    errorDiv.classList.remove('hidden');
                }
            } else {
                this.classList.remove('border-red-500');
                if (errorDiv) {
                    errorDiv.classList.add('hidden');
                }
            }
        });
    }

    // Validación de confirmación de contraseña
    const confirmarContrasena = document.getElementById('confirmarContrasena');
    if (confirmarContrasena) {
        confirmarContrasena.addEventListener('keyup', function () {
            const contrasena = document.getElementById('Contrasena');
            const errorDiv = document.getElementById('error-confirmar-contrasena');

            if (!contrasena) return;

            if (contrasena.value === this.value) {
                this.classList.remove('border-red-500');
                this.classList.add('border-green-500');
                if (errorDiv) errorDiv.classList.add('hidden');
            } else {
                this.classList.remove('border-green-500');
                this.classList.add('border-red-500');
                if (errorDiv) errorDiv.classList.remove('hidden');
            }
        });
    }

    // Validación completa del formulario al enviar
    const formularioPerfil = document.getElementById('perfilForm');
    if (formularioPerfil) {
        formularioPerfil.addEventListener('submit', function (event) {
            if (!validarFormularioPerfil()) {
                event.preventDefault();
                return false;
            }
        });
    }
}

// Función para validar todo el formulario de perfil
function validarFormularioPerfil() {
    let esValido = true;
    let primerCampoInvalido = null;

    // Definir campos obligatorios con sus validaciones
    const camposObligatorios = [
        { id: 'NombreUsuario', min: 3, nombre: 'Nombre de Usuario' },
        { id: 'NombreCompleto', min: 2, nombre: 'Nombre Completo' },
        { id: 'Telefono', exact: 8, nombre: 'Teléfono' },
        { id: 'IdProvincia', select: true, nombre: 'Provincia' },
        { id: 'DireccionCompleta', min: 10, nombre: 'Dirección' }
    ];

    // Validar cada campo
    camposObligatorios.forEach(function (campo) {
        const elemento = document.getElementById(campo.id);
        if (!elemento) return;

        const valor = elemento.value.trim();
        let valido = true;

        if (valor === '') {
            valido = false;
        } else if (campo.exact && valor.length !== campo.exact) {
            valido = false;
        } else if (campo.min && valor.length < campo.min) {
            valido = false;
        }

        if (!valido) {
            elemento.classList.add('border-red-500');
            esValido = false;
            if (!primerCampoInvalido) primerCampoInvalido = elemento;
        }
    });

    // Validar contraseñas si se proporcionan
    const contrasena = document.getElementById('Contrasena');
    const confirmarContrasena = document.getElementById('confirmarContrasena');

    if (contrasena && confirmarContrasena) {
        const valorContrasena = contrasena.value;
        const valorConfirmar = confirmarContrasena.value;

        if (valorContrasena !== '' || valorConfirmar !== '') {
            if (valorContrasena.length < 6) {
                contrasena.classList.add('border-red-500');
                const errorContrasena = document.getElementById('error-contrasena');
                if (errorContrasena) errorContrasena.classList.remove('hidden');
                esValido = false;
                if (!primerCampoInvalido) primerCampoInvalido = contrasena;
            }

            if (valorContrasena !== valorConfirmar) {
                confirmarContrasena.classList.add('border-red-500');
                const errorConfirmar = document.getElementById('error-confirmar-contrasena');
                if (errorConfirmar) errorConfirmar.classList.remove('hidden');
                esValido = false;
                if (!primerCampoInvalido) primerCampoInvalido = confirmarContrasena;
            }
        }
    }

    if (!esValido) {
        if (primerCampoInvalido) {
            primerCampoInvalido.focus();
        }
        alert('Por favor complete todos los campos obligatorios correctamente.');
    }

    return esValido;
}

// ===== FUNCIONALIDAD MOSTRAR/OCULTAR CONTRASEÑA - SOLO DESKTOP =====

// Función para detectar si es un dispositivo móvil o tablet
function esMobile() {
    const userAgent = navigator.userAgent.toLowerCase();
    const isMobileDevice = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini|mobile|tablet/i.test(userAgent);
    const isSmallScreen = window.innerWidth <= 768;
    const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

    return isMobileDevice || (isSmallScreen && isTouchDevice);
}

// Función para agregar estilos CSS solo para desktop
function agregarEstilosPersonalizados() {
    // Verificar si ya se agregaron los estilos
    if (document.getElementById('custom-password-styles')) return;

    // NO agregar estilos en móviles para evitar interferencias
    if (esMobile()) {
        console.log('Dispositivo móvil detectado - funcionalidad de mostrar/ocultar contraseña deshabilitada');
        return;
    }

    const style = document.createElement('style');
    style.id = 'custom-password-styles';
    style.textContent = `
        /* Estilos SOLO para desktop */
        @media (min-width: 769px) and (hover: hover) and (pointer: fine) {
            /* Ocultar botones nativos del navegador en desktop */
            input[type="password"]::-ms-reveal {
                display: none !important;
            }
            
            input[type="password"]::-webkit-credentials-auto-fill-button {
                display: none !important;
                visibility: hidden !important;
            }
            
            /* Estilos para botón personalizado */
            .toggle-password {
                position: absolute;
                right: 8px;
                top: 50%;
                transform: translateY(-50%);
                padding: 6px;
                cursor: pointer;
                z-index: 10;
                background: transparent;
                border: none;
                color: #6b7280;
                transition: color 0.2s ease-in-out;
                border-radius: 4px;
                width: 32px;
                height: 32px;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            
            .toggle-password:hover {
                color: #4f46e5;
                background-color: rgba(79, 70, 229, 0.1);
            }
            
            .toggle-password:focus {
                outline: 2px solid #4f46e5;
                outline-offset: 1px;
                color: #4f46e5;
            }
            
            .toggle-password i {
                font-size: 14px;
                pointer-events: none;
            }
            
            .password-field-container {
                position: relative;
            }
            
            .password-field-container input[type="password"],
            .password-field-container input[type="text"] {
                padding-right: 40px !important;
            }
        }
    `;

    document.head.appendChild(style);
}

// Función para crear el botón de mostrar/ocultar contraseña SOLO EN DESKTOP
function crearBotonMostrarContrasena(inputId) {
    // NO crear botón en móviles
    if (esMobile()) {
        return;
    }

    const input = document.getElementById(inputId);
    if (!input) return;

    // Verificar si ya tiene el botón
    const container = input.parentElement;
    if (container.querySelector('.toggle-password')) return;

    // Asegurarse de que el contenedor tenga posición relativa
    if (!container.classList.contains('relative')) {
        container.classList.add('relative');
    }

    // Agregar clase para identificar contenedores de contraseña
    container.classList.add('password-field-container');

    // Configurar atributos del input
    input.setAttribute('autocomplete', 'new-password');

    // Crear el botón de mostrar/ocultar
    const toggleButton = document.createElement('button');
    toggleButton.type = 'button';
    toggleButton.className = 'toggle-password';
    toggleButton.innerHTML = '<i class="fas fa-eye" aria-hidden="true"></i>';
    toggleButton.setAttribute('title', 'Mostrar contraseña');
    toggleButton.setAttribute('aria-label', 'Mostrar contraseña');
    toggleButton.setAttribute('tabindex', '0');

    // Agregar el evento click
    toggleButton.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        togglePasswordVisibility(inputId);
    });

    // Prevenir que el botón interfiera con el foco del input
    toggleButton.addEventListener('mousedown', function (e) {
        e.preventDefault();
    });

    // Soporte para navegación por teclado
    toggleButton.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            togglePasswordVisibility(inputId);
        }
    });

    // Insertar el botón en el contenedor
    container.appendChild(toggleButton);
}

// Función para alternar la visibilidad de la contraseña SOLO EN DESKTOP
function togglePasswordVisibility(inputId) {
    // NO funcionar en móviles
    if (esMobile()) {
        return;
    }

    const input = document.getElementById(inputId);
    if (!input) return;

    const button = input.parentElement.querySelector('.toggle-password');
    if (!button) return;

    const icon = button.querySelector('i');
    const cursorPosition = input.selectionStart;

    if (input.type === 'password') {
        // Mostrar contraseña
        input.type = 'text';
        icon.className = 'fas fa-eye-slash';
        button.setAttribute('title', 'Ocultar contraseña');
        button.setAttribute('aria-label', 'Ocultar contraseña');
        input.setAttribute('autocomplete', 'off');
    } else {
        // Ocultar contraseña
        input.type = 'password';
        icon.className = 'fas fa-eye';
        button.setAttribute('title', 'Mostrar contraseña');
        button.setAttribute('aria-label', 'Mostrar contraseña');
        input.setAttribute('autocomplete', 'new-password');
    }

    // Restaurar la posición del cursor
    setTimeout(() => {
        input.focus();
        input.setSelectionRange(cursorPosition, cursorPosition);
    }, 0);
}

// Función para inicializar todos los campos de contraseña SOLO EN DESKTOP
function inicializarCamposContrasena() {
    // NO inicializar en móviles
    if (esMobile()) {
        console.log('Funcionalidad de mostrar/ocultar contraseña deshabilitada en móviles');
        return;
    }

    // Lista de IDs de campos de contraseña que pueden existir en las diferentes vistas
    const camposContrasena = [
        'Contrasena',           // Campo principal de contraseña
        'contrasena',
        'CONTRASENA',           // Variante en mayúsculas
        'confirmarContrasena',  // Campo de confirmación
        'nuevaContrasena',      // Campo de nueva contraseña (edición)
        'NuevaContrasena',      // Variante para reset password
        'ConfirmarContrasena'   // Variante para reset password
    ];

    // Agregar el botón a cada campo que exista
    camposContrasena.forEach(function (campoId) {
        crearBotonMostrarContrasena(campoId);
    });
}

// Función para mejorar la experiencia en móviles sin interferir con la funcionalidad nativa
function mejorarExperienciaMovil() {
    if (!esMobile()) return;

    // Agregar un pequeño delay al hacer scroll cuando se enfoca un campo de contraseña
    document.addEventListener('focusin', function (e) {
        if (e.target.type === 'password') {
            setTimeout(() => {
                // Solo hacer scroll si es necesario
                const rect = e.target.getBoundingClientRect();
                const isVisible = (
                    rect.top >= 0 &&
                    rect.left >= 0 &&
                    rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
                    rect.right <= (window.innerWidth || document.documentElement.clientWidth)
                );

                if (!isVisible) {
                    e.target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'center',
                        inline: 'nearest'
                    });
                }
            }, 100);
        }
    });

    // Evitar zoom en iOS cuando se enfoca un input
    const inputs = document.querySelectorAll('input[type="password"]');
    inputs.forEach(input => {
        if (input.style.fontSize === '' || parseFloat(input.style.fontSize) < 16) {
            input.style.fontSize = '16px';
        }
    });
}

// ===== INICIALIZACIÓN =====

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    // Solo agregar estilos y funcionalidad en desktop
    if (!esMobile()) {
        agregarEstilosPersonalizados();
        inicializarCamposContrasena();
    } else {
        // En móviles, solo mejorar la experiencia sin interferir
        mejorarExperienciaMovil();
    }

    // Las validaciones siempre se inicializan
    inicializarValidacionesPerfil();
});

// También inicializar cuando jQuery esté listo (para compatibilidad)
if (typeof $ !== 'undefined') {
    $(document).ready(function () {
        if (!esMobile()) {
            agregarEstilosPersonalizados();
            inicializarCamposContrasena();

            // Reinicializar si se agregan campos dinámicamente
            setTimeout(function () {
                inicializarCamposContrasena();
            }, 100);
        } else {
            mejorarExperienciaMovil();
        }

        inicializarValidacionesPerfil();
    });
}

// Manejar cambios de tamaño de ventana
window.addEventListener('resize', function () {
    // Si cambia de móvil a desktop o viceversa, reinicializar
    setTimeout(() => {
        const esMovilAhora = esMobile();
        const tieneEstilos = document.getElementById('custom-password-styles');
        const tieneBotones = document.querySelector('.toggle-password');

        if (!esMovilAhora && !tieneEstilos) {
            // Cambió a desktop y no tiene estilos
            agregarEstilosPersonalizados();
            inicializarCamposContrasena();
        } else if (esMovilAhora && tieneBotones) {
            // Cambió a móvil y tiene botones, removerlos
            document.querySelectorAll('.toggle-password').forEach(btn => btn.remove());
            document.querySelectorAll('.password-field-container').forEach(container => {
                container.classList.remove('password-field-container');
                const input = container.querySelector('input[type="password"], input[type="text"]');
                if (input && input.style.paddingRight) {
                    input.style.paddingRight = '';
                }
            });
        }
    }, 100);
});

// ===== FUNCIONES PÚBLICAS PARA USO MANUAL =====

// Función que se puede llamar manualmente para agregar el botón a un campo específico
function agregarMostrarContrasena(inputId) {
    if (!esMobile()) {
        crearBotonMostrarContrasena(inputId);
    }
}

// Función para agregar a múltiples campos
function agregarMostrarContrasenaMultiple(arrayIds) {
    if (!esMobile()) {
        arrayIds.forEach(function (id) {
            crearBotonMostrarContrasena(id);
        });
    }
}