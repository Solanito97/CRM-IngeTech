// Función para inicializar validación en la vista de Editar
function initializeInventarioEditValidation() {
    $(document).ready(function () {
        // Obtener datos desde la vista
        var cantidadOriginal = window.inventarioData ? window.inventarioData.cantidadOriginal : 0;
        var formSubmissionAllowed = false;

        // Validación adicional con JavaScript para prevenir decimales
        const cantidadInput = document.querySelector('input[name="CANTIDAD"]');
        const cantidadMinimaInput = document.querySelector('input[name="CANTIDAD_MINIMA"]');

        // Solo ejecutar si los elementos existen
        if (!cantidadInput || !cantidadMinimaInput) {
            return;
        }

        // Prevenir entrada de decimales
        [cantidadInput, cantidadMinimaInput].forEach(input => {
            input.addEventListener('keypress', function (e) {
                // Permitir solo números y teclas de control
                if (!/[0-9]/.test(e.key) && !['Backspace', 'Delete', 'Tab', 'Enter', 'ArrowLeft', 'ArrowRight'].includes(e.key)) {
                    e.preventDefault();
                }
            });

            input.addEventListener('paste', function (e) {
                // Validar contenido pegado
                setTimeout(() => {
                    if (!/^[0-9]+$/.test(this.value)) {
                        this.value = this.value.replace(/[^0-9]/g, '');
                    }
                }, 0);
            });

            // Prevenir rueda del mouse para cambiar valores accidentalmente
            input.addEventListener('wheel', function (e) {
                e.preventDefault();
            });
        });

        // Función para mostrar el modal
        function showStockReductionModal(currentStock, newStock) {
            const difference = currentStock - newStock;

            $('#currentStock').text(currentStock + ' unidades');
            $('#newStock').text(newStock + ' unidades');
            $('#stockDifference').text('-' + difference + ' unidades');

            $('#stockReductionModal').removeClass('hidden').addClass('flex');

            // Animación de entrada
            setTimeout(() => {
                $('#stockReductionModal > div').addClass('scale-100').removeClass('scale-95');
            }, 10);
        }

        // Función para ocultar el modal
        function hideStockReductionModal() {
            $('#stockReductionModal > div').addClass('scale-95').removeClass('scale-100');
            setTimeout(() => {
                $('#stockReductionModal').addClass('hidden').removeClass('flex');
            }, 150);
        }

        // Evento para cancelar en el modal
        $('#cancelReduction').click(function () {
            hideStockReductionModal();
            formSubmissionAllowed = false;
        });

        // Evento para confirmar en el modal
        $('#confirmReduction').click(function () {
            hideStockReductionModal();
            formSubmissionAllowed = true;
            $('form').submit();
        });

        // Cerrar modal al hacer clic en el fondo
        $('#stockReductionModal').click(function (e) {
            if (e.target === this) {
                hideStockReductionModal();
                formSubmissionAllowed = false;
            }
        });

        // Cerrar modal con tecla Escape
        $(document).keydown(function (e) {
            if (e.key === 'Escape' && !$('#stockReductionModal').hasClass('hidden')) {
                hideStockReductionModal();
                formSubmissionAllowed = false;
            }
        });

        // Al enviar el formulario
        $('form').submit(function (e) {
            // Si ya se confirmó la reducción, permitir envío
            if (formSubmissionAllowed) {
                return true;
            }

            var cantidadNueva = parseInt($('#CANTIDAD').val());

            // Validar que los valores sean números enteros válidos
            if (isNaN(cantidadNueva) || cantidadNueva < 0) {
                e.preventDefault();
                alert('Por favor ingrese una cantidad válida (número entero mayor o igual a 0).');
                return false;
            }

            var cantidadMinimaNueva = parseInt($('#CANTIDAD_MINIMA').val());
            if (isNaN(cantidadMinimaNueva) || cantidadMinimaNueva < 1) {
                e.preventDefault();
                alert('Por favor ingrese una cantidad mínima válida (número entero mayor a 0).');
                return false;
            }

            // Si la cantidad disminuye, mostrar modal de confirmación
            if (cantidadNueva < cantidadOriginal) {
                e.preventDefault();
                showStockReductionModal(cantidadOriginal, cantidadNueva);
                return false;
            }

            return true;
        });
    });
}

// Función para validación solo de números enteros (para usar en Crear)
function initializeInventarioCreateValidation() {
    $(document).ready(function () {
        const cantidadInput = document.querySelector('input[name="CANTIDAD"]');
        const cantidadMinimaInput = document.querySelector('input[name="CANTIDAD_MINIMA"]');

        // Solo ejecutar si los elementos existen
        if (!cantidadInput || !cantidadMinimaInput) {
            return;
        }

        [cantidadInput, cantidadMinimaInput].forEach(input => {
            input.addEventListener('keypress', function (e) {
                // Permitir solo números y teclas de control
                if (!/[0-9]/.test(e.key) && !['Backspace', 'Delete', 'Tab', 'Enter', 'ArrowLeft', 'ArrowRight'].includes(e.key)) {
                    e.preventDefault();
                }
            });

            input.addEventListener('paste', function (e) {
                // Validar contenido pegado
                setTimeout(() => {
                    if (!/^[0-9]+$/.test(this.value)) {
                        this.value = this.value.replace(/[^0-9]/g, '');
                    }
                }, 0);
            });

            // Prevenir rueda del mouse para cambiar valores accidentalmente
            input.addEventListener('wheel', function (e) {
                e.preventDefault();
            });
        });
    });
}