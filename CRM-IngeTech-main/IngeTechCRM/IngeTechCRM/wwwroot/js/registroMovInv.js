// registroMovInv.js - Funcionalidad para registro de movimientos de inventario

$(document).ready(function () {
    // Funciones para mostrar/ocultar mensajes de validación
    window.showValidationAlert = function (message, type = 'error') {
        const alert = document.getElementById('validationAlert');
        const icon = document.getElementById('validationIcon');
        const messageEl = document.getElementById('validationMessage');

        // Configurar estilos según el tipo
        if (type === 'error') {
            alert.className = 'mb-4 rounded-lg border-l-4 border-red-400 bg-red-50 p-4 transition-all duration-300';
            icon.className = 'fas fa-exclamation-circle text-lg text-red-400';
            messageEl.className = 'text-sm font-medium text-red-800';
        } else if (type === 'warning') {
            alert.className = 'mb-4 rounded-lg border-l-4 border-yellow-400 bg-yellow-50 p-4 transition-all duration-300';
            icon.className = 'fas fa-exclamation-triangle text-lg text-yellow-400';
            messageEl.className = 'text-sm font-medium text-yellow-800';
        }

        messageEl.textContent = message;
        alert.classList.remove('hidden');

        // Scroll suave hacia el mensaje
        alert.scrollIntoView({ behavior: 'smooth', block: 'center' });
    };

    window.hideValidationAlert = function () {
        document.getElementById('validationAlert').classList.add('hidden');
    };

    // Función para mostrar modal (alternativa)
    window.showModal = function (title, message, type = 'error') {
        const modal = document.getElementById('confirmModal');
        if (!modal) return; // Si no existe el modal, salir

        const titleEl = document.getElementById('modalTitle');
        const messageEl = document.getElementById('modalMessage');
        const iconContainer = document.getElementById('modalIconContainer');
        const icon = document.getElementById('modalIcon');

        titleEl.textContent = title;
        messageEl.textContent = message;

        if (type === 'error') {
            iconContainer.className = 'mx-auto flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-red-100 sm:mx-0 sm:h-10 sm:w-10';
            icon.className = 'fas fa-exclamation-circle text-xl text-red-600';
        } else if (type === 'warning') {
            iconContainer.className = 'mx-auto flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-yellow-100 sm:mx-0 sm:h-10 sm:w-10';
            icon.className = 'fas fa-exclamation-triangle text-xl text-yellow-600';
        }

        modal.classList.remove('hidden');
    };

    window.closeModal = function () {
        const modal = document.getElementById('confirmModal');
        if (modal) {
            modal.classList.add('hidden');
        }
    };

    // **NUEVA FUNCIÓN**: Interceptar validación HTML5 para todos los campos
    function interceptarValidacionHTML5(elemento, mensajesPersonalizados) {
        if (!elemento) return;

        // Limpiar validaciones del navegador cuando el usuario interactúe
        elemento.addEventListener('input', function () {
            this.setCustomValidity('');
            hideValidationAlert();
        });

        elemento.addEventListener('change', function () {
            this.setCustomValidity('');
            hideValidationAlert();
        });

        // Interceptar TODOS los eventos de validación del navegador
        elemento.addEventListener('invalid', function (e) {
            e.preventDefault(); // Previene el mensaje del navegador
            e.stopPropagation(); // Detiene la propagación del evento

            // Determinar el mensaje personalizado
            let mensaje = 'Por favor complete este campo correctamente.';

            if (this.validity.valueMissing && mensajesPersonalizados.required) {
                mensaje = mensajesPersonalizados.required;
            } else if (this.validity.patternMismatch && mensajesPersonalizados.pattern) {
                mensaje = mensajesPersonalizados.pattern;
            } else if (this.validity.rangeUnderflow && mensajesPersonalizados.min) {
                mensaje = mensajesPersonalizados.min;
            } else if (this.validity.stepMismatch && mensajesPersonalizados.step) {
                mensaje = mensajesPersonalizados.step;
            }

            showValidationAlert(mensaje);
            this.focus();
            return false;
        });
    }

    // **APLICAR INTERCEPTACIÓN A TODOS LOS CAMPOS**

    // Campo Producto
    const productoSelect = document.getElementById('productoSelect');
    interceptarValidacionHTML5(productoSelect, {
        required: 'Por favor seleccione un producto.'
    });

    // Campo Almacén
    const almacenSelect = document.getElementById('almacenSelect');
    interceptarValidacionHTML5(almacenSelect, {
        required: 'Por favor seleccione un almacén.'
    });

    // Campo Tipo de Movimiento
    const tipoMovimientoSelect = document.getElementById('tipoMovimientoSelect');
    interceptarValidacionHTML5(tipoMovimientoSelect, {
        required: 'Por favor seleccione un tipo de movimiento.'
    });

    // Campo Cantidad (con lógica especial existente)
    const cantidadInput = document.querySelector('#cantidadInput');
    const cantidadHelp = document.querySelector('#cantidadHelp');

    if (cantidadInput) {
        // Validación para entrada de caracteres según tipo de movimiento
        cantidadInput.addEventListener('keypress', function (e) {
            const tipoMovimiento = $('#tipoMovimientoSelect').val();
            const isNumber = /[0-9]/.test(e.key);
            const isMinus = e.key === '-' && this.selectionStart === 0 && !this.value.includes('-');
            const isControlKey = ['Backspace', 'Delete', 'Tab', 'Enter', 'ArrowLeft', 'ArrowRight'].includes(e.key);

            // Solo permitir signo menos para ajustes
            const allowMinus = tipoMovimiento === 'AJUSTE' && isMinus;

            if (!isNumber && !allowMinus && !isControlKey) {
                e.preventDefault();
            }
        });

        cantidadInput.addEventListener('paste', function (e) {
            setTimeout(() => {
                const tipoMovimiento = $('#tipoMovimientoSelect').val();
                let valor = this.value.replace(/[^0-9-]/g, '');

                // Si no es ajuste, eliminar signos menos
                if (tipoMovimiento !== 'AJUSTE') {
                    valor = valor.replace(/-/g, '');
                } else {
                    // Para ajustes, asegurar que solo haya un signo menos al inicio
                    if (valor.indexOf('-') > 0) {
                        valor = valor.replace(/-/g, '');
                    }
                    if ((valor.match(/-/g) || []).length > 1) {
                        valor = '-' + valor.replace(/-/g, '');
                    }
                }

                this.value = valor;
            }, 0);
        });

        cantidadInput.addEventListener('blur', function () {
            if (this.value === '' || this.value === '-') {
                this.value = '1';
            }
        });

        // Aplicar interceptación con mensajes personalizados para cantidad
        interceptarValidacionHTML5(cantidadInput, {
            required: 'Por favor ingrese una cantidad.',
            min: function () {
                const tipo = $('#tipoMovimientoSelect').val();
                if (tipo === 'ENTRADA') {
                    return 'Para movimientos de entrada, la cantidad debe ser mayor a 0.';
                } else if (tipo === 'SALIDA') {
                    return 'Para movimientos de salida, la cantidad debe ser mayor a 0.';
                } else {
                    return 'La cantidad debe ser un número válido.';
                }
            },
            step: 'Por favor ingrese un número entero válido.'
        });

        // Interceptar validación específica con lógica dinámica
        cantidadInput.addEventListener('invalid', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const tipo = $('#tipoMovimientoSelect').val();

            if (this.validity.valueMissing) {
                showValidationAlert('Por favor ingrese una cantidad.');
            } else if (this.validity.rangeUnderflow) {
                if (tipo === 'ENTRADA') {
                    showValidationAlert('Para movimientos de entrada, la cantidad debe ser mayor a 0.');
                } else if (tipo === 'SALIDA') {
                    showValidationAlert('Para movimientos de salida, la cantidad debe ser mayor a 0.');
                } else {
                    showValidationAlert('La cantidad debe ser un número válido.');
                }
            } else if (this.validity.stepMismatch) {
                showValidationAlert('Por favor ingrese un número entero válido.');
            }

            this.focus();
            return false;
        });
    }

    // Manejar cambio de tipo de movimiento
    $('#tipoMovimientoSelect').change(function () {
        const tipo = $(this).val();
        const cantidadInputJQ = $('#cantidadInput');

        hideValidationAlert();

        // Ocultar todos los paneles de información
        $('#entradaInfo, #salidaInfo, #ajusteInfo').addClass('hidden');

        // Actualizar texto de ayuda y configurar input
        if (tipo === 'AJUSTE') {
            if (cantidadHelp) {
                cantidadHelp.textContent = 'Números enteros positivos o negativos (ej: -5 para reducir)';
            }
            $('#ajusteInfo').removeClass('hidden');

            // Configurar para ajustes (QUITAR todos los atributos de validación HTML5)
            cantidadInputJQ.removeAttr('min');
            cantidadInputJQ.removeAttr('max');
            cantidadInputJQ.attr('step', '1');
        } else {
            if (cantidadHelp) {
                cantidadHelp.textContent = 'Solo números enteros positivos';
            }

            // Configurar para entrada/salida (QUITAR min para evitar validación HTML5)
            cantidadInputJQ.removeAttr('min');
            cantidadInputJQ.attr('step', '1');

            if (tipo === 'ENTRADA') {
                $('#entradaInfo').removeClass('hidden');
            } else if (tipo === 'SALIDA') {
                $('#salidaInfo').removeClass('hidden');
            }
        }

        // Siempre limpiar validaciones del navegador al cambiar tipo
        if (cantidadInput) {
            cantidadInput.setCustomValidity('');
        }

        // Limpiar valores negativos si cambia de AJUSTE a otro tipo
        if (tipo !== 'AJUSTE' && cantidadInput && cantidadInput.value.includes('-')) {
            cantidadInput.value = Math.abs(parseInt(cantidadInput.value)) || 1;
        }
    });

    // Verificar stock al seleccionar producto y almacén
    function verificarStock() {
        const productoId = $('#productoSelect').val();
        const almacenId = $('#almacenSelect').val();

        if (productoId && almacenId) {
            // Construir URL dinámicamente (necesitarás pasarla desde la vista)
            const url = window.obtenerStockUrl || '/Inventario/ObtenerStockActual';

            $.ajax({
                url: url,
                type: 'GET',
                data: { productoId: productoId, almacenId: almacenId },
                success: function (response) {
                    $('#stockActual').text(response.success ? response.stock : '0');
                    $('#stockActualContainer').removeClass('hidden');
                },
                error: function () {
                    // Si falla el AJAX, ocultar el stock
                    $('#stockActualContainer').addClass('hidden');
                }
            });
        } else {
            $('#stockActualContainer').addClass('hidden');
        }
    }

    $('#productoSelect, #almacenSelect').change(verificarStock);

    // **INTERCEPTAR EL SUBMIT DEL FORMULARIO COMPLETAMENTE**
    $('form').on('submit', function (e) {
        // Siempre prevenir el submit por defecto
        e.preventDefault();

        // Limpiar todos los mensajes de validación HTML5
        const campos = ['productoSelect', 'almacenSelect', 'tipoMovimientoSelect', 'cantidadInput'];
        campos.forEach(id => {
            const elemento = document.getElementById(id);
            if (elemento) {
                elemento.setCustomValidity('');
            }
        });

        const tipo = $('#tipoMovimientoSelect').val();
        const cantidadValue = $('#cantidadInput').val().trim();
        const cantidad = parseInt(cantidadValue);
        const stockActual = parseInt($('#stockActual').text()) || 0;
        const productoId = $('#productoSelect').val();
        const almacenId = $('#almacenSelect').val();

        hideValidationAlert();

        // Validaciones personalizadas
        if (!productoId) {
            showValidationAlert('Por favor seleccione un producto.');
            $('#productoSelect').focus();
            return false;
        }

        if (!almacenId) {
            showValidationAlert('Por favor seleccione un almacén.');
            $('#almacenSelect').focus();
            return false;
        }

        if (!tipo) {
            showValidationAlert('Por favor seleccione un tipo de movimiento.');
            $('#tipoMovimientoSelect').focus();
            return false;
        }

        // Validar que sea un número entero válido
        if (cantidadValue === '' || isNaN(cantidad)) {
            showValidationAlert('Por favor ingrese una cantidad válida (solo números enteros).');
            $('#cantidadInput').focus();
            return false;
        }

        // Validaciones específicas por tipo de movimiento
        if (tipo === 'ENTRADA' && cantidad <= 0) {
            showValidationAlert('Para movimientos de entrada, la cantidad debe ser mayor a 0.');
            $('#cantidadInput').focus();
            return false;
        }

        if (tipo === 'SALIDA') {
            if (cantidad <= 0) {
                showValidationAlert('Para movimientos de salida, la cantidad debe ser mayor a 0.');
                $('#cantidadInput').focus();
                return false;
            }
            if (cantidad > stockActual) {
                showValidationAlert(`No hay suficiente stock disponible. Stock actual: ${stockActual} unidades, cantidad solicitada: ${cantidad} unidades.`, 'warning');
                $('#cantidadInput').focus();
                return false;
            }
        }

        if (tipo === 'AJUSTE') {
            if (cantidad === 0) {
                showValidationAlert('Para ajustes, la cantidad no puede ser 0.');
                $('#cantidadInput').focus();
                return false;
            }
            if (cantidad < 0 && Math.abs(cantidad) > stockActual) {
                showValidationAlert(`El ajuste negativo excede el stock disponible. Stock actual: ${stockActual} unidades, máximo ajuste negativo: -${stockActual}.`, 'warning');
                $('#cantidadInput').focus();
                return false;
            }
        }

        // Si llegamos aquí, todas las validaciones pasaron
        // Enviar el formulario manualmente
        this.submit();
        return true;
    });

    // **INTERCEPTAR VALIDACIÓN HTML5 AL NIVEL DEL FORMULARIO**
    $('form')[0].addEventListener('invalid', function (e) {
        e.preventDefault();
        e.stopPropagation();
        return false;
    }, true);

    // Inicialización: quitar atributos de validación HTML5 de todos los campos
    $(document).ready(function () {
        // Quitar required de todos los campos para manejar validación manualmente
        $('#productoSelect, #almacenSelect, #tipoMovimientoSelect').removeAttr('required');

        if (cantidadInput) {
            $(cantidadInput).removeAttr('min').removeAttr('required');
        }
    });
});