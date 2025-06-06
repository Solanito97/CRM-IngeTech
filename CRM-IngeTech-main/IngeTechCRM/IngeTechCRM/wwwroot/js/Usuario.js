// wwwroot/js/usuario.js - Versión híbrida optimizada para móviles y desktop

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

// ===== DETECCIÓN DE DISPOSITIVO MEJORADA =====

// Variable global para almacenar el estado del dispositivo
let estadoDispositivo = null;

// Función para detectar tipo de dispositivo de manera más precisa
function detectarTipoDispositivo() {
    if (estadoDispositivo !== null) return estadoDispositivo;

    const userAgent = navigator.userAgent.toLowerCase();
    const ancho = window.innerWidth;
    const alto = window.innerHeight;

    // Detección más específica
    const esMobileUA = /android|webos|iphone|ipod|blackberry|iemobile|opera mini/i.test(userAgent);
    const esTabletUA = /ipad|android.*tablet|kindle|silk/i.test(userAgent);
    const esPantallaMovil = ancho <= 768;
    const esPantallaTablet = ancho > 768 && ancho <= 1024;
    const tieneTouch = 'ontouchstart' in window;

    // Lógica de decisión
    if (esMobileUA || (esPantallaMovil && tieneTouch)) {
        estadoDispositivo = 'mobile';
    } else if (esTabletUA || (esPantallaTablet && tieneTouch)) {
        estadoDispositivo = 'tablet';
    } else {
        estadoDispositivo = 'desktop';
    }

    console.log(`Dispositivo detectado: ${estadoDispositivo} (${ancho}x${alto})`);
    return estadoDispositivo;
}

// ===== FUNCIONALIDAD MOSTRAR/OCULTAR CONTRASEÑA OPTIMIZADA =====

// Función para agregar estilos CSS optimizados
function agregarEstilosPersonalizados() {
    if (document.getElementById('custom-password-styles')) return;

    const tipoDispositivo = detectarTipoDispositivo();

    const style = document.createElement('style');
    style.id = 'custom-password-styles';

    let estilosCSS = `
        /* Estilos base para todos los dispositivos */
        .password-field-container {
            position: relative;
            display: flex;
            align-items: center;
        }
        
        .toggle-password {
            position: absolute;
            right: 8px;
            background: transparent;
            border: none;
            cursor: pointer;
            z-index: 10;
            color: #6b7280;
            transition: all 0.2s ease-in-out;
            border-radius: 4px;
            display: flex;
            align-items: center;
            justify-content: center;
            user-select: none;
            -webkit-user-select: none;
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
            pointer-events: none;
            font-size: 14px;
        }
    `;

    // Estilos específicos según el dispositivo
    if (tipoDispositivo === 'mobile') {
        estilosCSS += `
            /* Estilos optimizados para móviles */
            .toggle-password {
                padding: 14px;
                min-width: 48px;
                min-height: 48px;
                right: 4px;
                top: 50%;
                transform: translateY(-50%);
            }
            
            .toggle-password i {
                font-size: 16px;
            }
            
            .password-field-container input[type="password"],
            .password-field-container input[type="text"] {
                padding-right: 56px !important;
                font-size: 16px !important; /* Evita zoom en iOS */
            }
            
            /* Mejorar interacción táctil */
            .toggle-password:active {
                background-color: rgba(79, 70, 229, 0.2);
                transform: translateY(-50%) scale(0.95);
            }
        `;
    } else if (tipoDispositivo === 'tablet') {
        estilosCSS += `
            /* Estilos para tablets */
            .toggle-password {
                padding: 12px;
                min-width: 44px;
                min-height: 44px;
                right: 6px;
                top: 50%;
                transform: translateY(-50%);
            }
            
            .toggle-password i {
                font-size: 15px;
            }
            
            .password-field-container input[type="password"],
            .password-field-container input[type="text"] {
                padding-right: 52px !important;
            }
        `;
    } else {
        estilosCSS += `
            /* Estilos para desktop */
            .toggle-password {
                padding: 8px;
                width: 32px;
                height: 32px;
                right: 8px;
                top: 50%;
                transform: translateY(-50%);
            }
            
            .password-field-container input[type="password"],
            .password-field-container input[type="text"] {
                padding-right: 48px !important;
            }
            
            /* Ocultar botones nativos solo en desktop */
            input[type="password"]::-ms-reveal {
                display: none !important;
            }
            
            input[type="password"]::-webkit-credentials-auto-fill-button {
                display: none !important;
                visibility: hidden !important;
            }
        `;
    }

    style.textContent = estilosCSS;
    document.head.appendChild(style);
}

// Función para crear el botón de mostrar/ocultar contraseña
function crearBotonMostrarContrasena(inputId) {
    const input = document.getElementById(inputId);
    if (!input) return;

    const container = input.parentElement;
    if (container.querySelector('.toggle-password')) return;

    // Configurar el contenedor
    if (!container.classList.contains('relative')) {
        container.classList.add('relative');
    }
    container.classList.add('password-field-container');

    // Configurar el input
    const tipoDispositivo = detectarTipoDispositivo();
    input.setAttribute('autocomplete', 'new-password');
    input.setAttribute('spellcheck', 'false');

    if (tipoDispositivo === 'mobile') {
        input.setAttribute('autocorrect', 'off');
        input.setAttribute('autocapitalize', 'none');
        // Asegurar tamaño de fuente para evitar zoom en iOS
        if (!input.style.fontSize || parseFloat(input.style.fontSize) < 16) {
            input.style.fontSize = '16px';
        }
    }

    // Crear el botón
    const toggleButton = document.createElement('button');
    toggleButton.type = 'button';
    toggleButton.className = 'toggle-password';
    toggleButton.innerHTML = '<i class="fas fa-eye" aria-hidden="true"></i>';
    toggleButton.setAttribute('title', 'Mostrar contraseña');
    toggleButton.setAttribute('aria-label', 'Mostrar contraseña');
    toggleButton.setAttribute('tabindex', '0');

    // Eventos del botón
    const manejarToggle = function (e) {
        e.preventDefault();
        e.stopPropagation();
        togglePasswordVisibility(inputId);
    };

    // Agregar eventos según el tipo de dispositivo
    if (tipoDispositivo === 'mobile') {
        // En móviles, usar touchend para mejor respuesta
        toggleButton.addEventListener('touchend', manejarToggle);
        toggleButton.addEventListener('click', manejarToggle);
    } else {
        // En desktop, usar click normal
        toggleButton.addEventListener('click', manejarToggle);
    }

    // Prevenir interferencia con el foco
    toggleButton.addEventListener('mousedown', function (e) {
        e.preventDefault();
    });

    toggleButton.addEventListener('touchstart', function (e) {
        e.preventDefault();
    });

    // Soporte para navegación por teclado
    toggleButton.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            manejarToggle(e);
        }
    });

    container.appendChild(toggleButton);
}

// Función para alternar la visibilidad de la contraseña
function togglePasswordVisibility(inputId) {
    const input = document.getElementById(inputId);
    if (!input) return;

    const button = input.parentElement.querySelector('.toggle-password');
    if (!button) return;

    const icon = button.querySelector('i');
    const tipoDispositivo = detectarTipoDispositivo();

    // Guardar posición del cursor
    const cursorPosition = input.selectionStart || 0;
    const valorActual = input.value;

    if (input.type === 'password') {
        // Mostrar contraseña
        input.type = 'text';
        icon.className = 'fas fa-eye-slash';
        button.setAttribute('title', 'Ocultar contraseña');
        button.setAttribute('aria-label', 'Ocultar contraseña');

        // En móviles, configuraciones especiales
        if (tipoDispositivo === 'mobile') {
            input.setAttribute('autocomplete', 'off');
            input.setAttribute('readonly', 'true');
            // Quitar readonly después de un pequeño delay
            setTimeout(() => {
                input.removeAttribute('readonly');
                input.focus();
                input.setSelectionRange(cursorPosition, cursorPosition);
            }, 50);
        }
    } else {
        // Ocultar contraseña
        input.type = 'password';
        icon.className = 'fas fa-eye';
        button.setAttribute('title', 'Mostrar contraseña');
        button.setAttribute('aria-label', 'Mostrar contraseña');

        input.setAttribute('autocomplete', 'new-password');
    }

    // Restaurar valor y posición del cursor
    input.value = valorActual;

    setTimeout(() => {
        input.focus();
        input.setSelectionRange(cursorPosition, cursorPosition);
    }, 10);
}

// Función para inicializar todos los campos de contraseña
function inicializarCamposContrasena() {
    const camposContrasena = [
        'Contrasena',
        'contrasena',
        'CONTRASENA',
        'confirmarContrasena',
        'nuevaContrasena',
        'NuevaContrasena',
        'ConfirmarContrasena'
    ];

    camposContrasena.forEach(function (campoId) {
        crearBotonMostrarContrasena(campoId);
    });
}

// Función para mejorar la experiencia en móviles
function mejorarExperienciaMovil() {
    const tipoDispositivo = detectarTipoDispositivo();
    if (tipoDispositivo !== 'mobile') return;

    // Mejorar scroll cuando se enfoca un campo
    document.addEventListener('focusin', function (e) {
        if (e.target.type === 'password' || e.target.type === 'email' || e.target.type === 'text') {
            setTimeout(() => {
                const rect = e.target.getBoundingClientRect();
                const margenSuperior = 80;
                const margenInferior = 150;

                if (rect.top < margenSuperior || rect.bottom > (window.innerHeight - margenInferior)) {
                    e.target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'center',
                        inline: 'nearest'
                    });
                }
            }, 300);
        }
    });
}

// ===== INICIALIZACIÓN =====

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    // Resetear detección de dispositivo
    estadoDispositivo = null;

    // Inicializar en orden
    agregarEstilosPersonalizados();
    inicializarCamposContrasena();
    inicializarValidacionesPerfil();
    mejorarExperienciaMovil();

    console.log('Usuario.js inicializado correctamente');
});

// Compatibilidad con jQuery
if (typeof $ !== 'undefined') {
    $(document).ready(function () {
        estadoDispositivo = null;
        agregarEstilosPersonalizados();
        inicializarCamposContrasena();
        inicializarValidacionesPerfil();
        mejorarExperienciaMovil();

        // Reinicializar campos dinámicos
        setTimeout(function () {
            inicializarCamposContrasena();
        }, 100);
    });
}

// Manejar cambios de orientación/tamaño
window.addEventListener('resize', function () {
    // Reinicializar después de cambio de orientación
    setTimeout(() => {
        estadoDispositivo = null; // Forzar nueva detección
        const nuevoTipo = detectarTipoDispositivo();
        console.log(`Cambio de orientación detectado: ${nuevoTipo}`);

        // Remover estilos anteriores y reinicializar
        const estilosAnteriores = document.getElementById('custom-password-styles');
        if (estilosAnteriores) {
            estilosAnteriores.remove();
        }

        agregarEstilosPersonalizados();
    }, 100);
});

// ===== FUNCIONES PÚBLICAS =====

function agregarMostrarContrasena(inputId) {
    crearBotonMostrarContrasena(inputId);
}

function agregarMostrarContrasenaMultiple(arrayIds) {
    arrayIds.forEach(function (id) {
        crearBotonMostrarContrasena(id);
    });
}