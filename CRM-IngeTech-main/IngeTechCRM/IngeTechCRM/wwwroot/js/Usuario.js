// wwwroot/js/usuario.js

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
        errorDiv.classList.remove('hidden');
        input.classList.add('border-red-500');
        input.classList.remove('border-gray-300');
    } else {
        errorDiv.classList.add('hidden');
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
        errorDiv.classList.remove('hidden');
        input.classList.add('border-red-500');
        input.classList.remove('border-gray-300');
    } else {
        errorDiv.classList.add('hidden');
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

// ===== FUNCIONALIDAD MOSTRAR/OCULTAR CONTRASEÑA =====

// Función para agregar estilos CSS automáticamente
function agregarEstilosPersonalizados() {
    // Verificar si ya se agregaron los estilos
    if (document.getElementById('custom-password-styles')) return;

    const style = document.createElement('style');
    style.id = 'custom-password-styles';
    style.textContent = `
        /* Ocultar botones nativos del navegador */
        input[type="password"]::-ms-reveal,
        input[type="password"]::-webkit-credentials-auto-fill-button {
            display: none !important;
            visibility: hidden !important;
            pointer-events: none !important;
        }
        
        input[type="password"]::-moz-reveal {
            display: none !important;
        }
        
        input[type="password"]::-webkit-textfield-decoration-container {
            visibility: hidden !important;
            display: none !important;
        }
        
        input[type="password"] {
            -webkit-text-security: disc;
        }
        
        /* Estilos para botón personalizado */
        .toggle-password {
            position: absolute;
            right: 0;
            top: 50%;
            transform: translateY(-50%);
            padding: 0.5rem;
            cursor: pointer;
            z-index: 10;
            background: transparent;
            border: none;
            color: #6b7280;
            transition: color 0.2s ease-in-out;
            border-radius: 0.25rem;
        }
        
        .toggle-password:hover {
            color: #4f46e5;
        }
        
        .toggle-password:focus {
            outline: none;
            color: #4f46e5;
            box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.2);
        }
        
        .toggle-password i {
            font-size: 1rem;
            pointer-events: none;
        }
        
        .password-field-container input[type="password"],
        .password-field-container input[type="text"] {
            padding-right: 2.5rem !important;
        }
    `;

    document.head.appendChild(style);
}

// Función para crear el botón de mostrar/ocultar contraseña
function crearBotonMostrarContrasena(inputId) {
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

    // Asegurar que el input tenga el padding correcto para nuestro botón
    if (!input.style.paddingRight) {
        input.style.paddingRight = '2.5rem';
    }

    // Prevenir que aparezcan botones nativos del navegador
    input.setAttribute('autocomplete', 'new-password');

    // Crear el botón de mostrar/ocultar
    const toggleButton = document.createElement('button');
    toggleButton.type = 'button';
    toggleButton.className = 'toggle-password';
    toggleButton.innerHTML = '<i class="fas fa-eye"></i>';
    toggleButton.setAttribute('title', 'Mostrar contraseña');
    toggleButton.setAttribute('aria-label', 'Mostrar contraseña');

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

    // Insertar el botón en el contenedor
    container.appendChild(toggleButton);
}

// Función para alternar la visibilidad de la contraseña
function togglePasswordVisibility(inputId) {
    const input = document.getElementById(inputId);
    const button = input.parentElement.querySelector('.toggle-password');
    const icon = button.querySelector('i');

    if (input.type === 'password') {
        // Mostrar contraseña
        input.type = 'text';
        icon.className = 'fas fa-eye-slash';
        button.setAttribute('title', 'Ocultar contraseña');
        button.setAttribute('aria-label', 'Ocultar contraseña');

        // Asegurar que no aparezcan botones nativos cuando cambiamos a text
        input.setAttribute('autocomplete', 'off');
    } else {
        // Ocultar contraseña
        input.type = 'password';
        icon.className = 'fas fa-eye';
        button.setAttribute('title', 'Mostrar contraseña');
        button.setAttribute('aria-label', 'Mostrar contraseña');

        // Restaurar autocomplete para contraseñas
        input.setAttribute('autocomplete', 'new-password');
    }
}

// Función para inicializar todos los campos de contraseña
function inicializarCamposContrasena() {
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

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    agregarEstilosPersonalizados(); // Agregar estilos CSS automáticamente
    inicializarCamposContrasena();
});

// También inicializar cuando jQuery esté listo (para compatibilidad)
$(document).ready(function () {
    agregarEstilosPersonalizados(); // Agregar estilos CSS automáticamente
    inicializarCamposContrasena();

    // Reinicializar si se agregan campos dinámicamente
    setTimeout(function () {
        inicializarCamposContrasena();
    }, 100);
});

// ===== FUNCIÓN ADICIONAL PARA AGREGAR MANUALMENTE =====

// Función que se puede llamar manualmente para agregar el botón a un campo específico
function agregarMostrarContrasena(inputId) {
    crearBotonMostrarContrasena(inputId);
}

// Función para agregar a múltiples campos
function agregarMostrarContrasenaMultiple(arrayIds) {
    arrayIds.forEach(function (id) {
        crearBotonMostrarContrasena(id);
    });
}