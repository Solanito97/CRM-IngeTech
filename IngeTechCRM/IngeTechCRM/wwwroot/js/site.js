// Scripts personalizados para IngeTech CRM

// Funciones comunes para toda la aplicación
$(document).ready(function () {
    // Inicializar tooltips
    $('[data-toggle="tooltip"]').tooltip();

    // Inicializar popovers
    $('[data-toggle="popover"]').popover();

    // Auto-ocultar alertas después de 5 segundos
    setTimeout(function () {
        $('.alert-dismissible').fadeTo(500, 0).slideUp(500, function () {
            $(this).remove();
        });
    }, 5000);

    // Confirmación para acciones peligrosas
    $('.confirm-action').click(function (e) {
        var message = $(this).data('confirm') || '¿Está seguro de que desea realizar esta acción?';
        if (!confirm(message)) {
            e.preventDefault();
        }
    });

    // Inicializar datepickers
    if ($.fn.datepicker) {
        $('.datepicker').datepicker({
            format: 'dd/mm/yyyy',
            language: 'es',
            autoclose: true
        });
    }

    // Inicializar select2 si está disponible
    if ($.fn.select2) {
        $('.select2').select2({
            language: 'es',
            placeholder: 'Seleccione una opción',
            allowClear: true
        });
    }

    // Validación de formularios
    validateForms();

    // Inicializar máscaras de entrada si está disponible
    if ($.fn.inputmask) {
        $('.phone-mask').inputmask('(999) 9999-9999', { placeholder: '(___) ____-____' });
        $('.id-mask').inputmask('9-9999-9999', { placeholder: '_-____-____' });
    }

    // Asegurar que los dropdowns de Bootstrap funcionen correctamente
    $('.dropdown-toggle').dropdown();

    // Manejador alternativo para dropdowns
    $('.nav-item.dropdown').on('click', function (e) {
        var $dropdown = $(this);
        var $menu = $dropdown.find('.dropdown-menu');

        // Si el clic fue directamente en el enlace dropdown-toggle
        if ($(e.target).hasClass('dropdown-toggle') || $(e.target).parent().hasClass('dropdown-toggle')) {
            e.preventDefault();
            e.stopPropagation();

            // Cerrar otros dropdowns abiertos
            $('.dropdown-menu.show').not($menu).removeClass('show');
            $('.dropdown.show').not($dropdown).removeClass('show');

            // Alternar este dropdown
            $dropdown.toggleClass('show');
            $menu.toggleClass('show');
        }
    });

    // Cerrar dropdowns al hacer clic fuera
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.dropdown').length) {
            $('.dropdown-menu.show').removeClass('show');
            $('.dropdown.show').removeClass('show');
        }
    });
});

// Función para validar formularios
function validateForms() {
    // Validar que las contraseñas coincidan
    $('.validate-password').on('submit', function (e) {
        var password = $(this).find('.password').val();
        var confirmPassword = $(this).find('.confirm-password').val();

        if (password !== confirmPassword) {
            e.preventDefault();
            alert('Las contraseñas no coinciden.');
            return false;
        }

        return true;
    });

    // Validar campos numéricos
    $('.numeric-only').on('keypress', function (e) {
        return e.charCode >= 48 && e.charCode <= 57;
    });

    // Validar campos de email
    $('.email-validation').on('blur', function () {
        var email = $(this).val();
        var regex = /^([a-zA-Z0-9_\.\-\+])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$/;

        if (!regex.test(email)) {
            $(this).addClass('is-invalid');
        } else {
            $(this).removeClass('is-invalid').addClass('is-valid');
        }
    });

    // Validación de formulario antes de enviar
    $('form.validate-form').on('submit', function (e) {
        var isValid = true;

        // Validar campos requeridos
        $(this).find('[required]').each(function () {
            if ($(this).val() === '') {
                isValid = false;
                $(this).addClass('is-invalid');
            } else {
                $(this).removeClass('is-invalid');
            }
        });

        if (!isValid) {
            e.preventDefault();
            alert('Por favor complete todos los campos requeridos.');
            return false;
        }

        return true;
    });

    // Limpiar clases de validación al cambiar el valor
    $('[required]').on('change keyup', function () {
        if ($(this).val() !== '') {
            $(this).removeClass('is-invalid');
        }
    });
}

// Función para actualizar cantidad en carrito
function updateCartItemQuantity(itemId, newQuantity) {
    if (newQuantity <= 0) {
        if (confirm('¿Está seguro de que desea eliminar este producto del carrito?')) {
            window.location.href = '/Home/EliminarDelCarrito?idItem=' + itemId;
        }
        return;
    }

    // Actualizar visualmente
    $('#subtotal-' + itemId).text((parseFloat($('#precio-' + itemId).data('precio')) * newQuantity).toFixed(2));

    // Calcular nuevo total
    let total = 0;
    $('.cart-item-subtotal').each(function () {
        total += parseFloat($(this).text());
    });
    $('#cart-total').text(total.toFixed(2));

    // Actualizar en base de datos mediante AJAX
    $.ajax({
        url: '/Home/ActualizarCantidadCarrito',
        type: 'POST',
        data: {
            idItem: itemId,
            cantidad: newQuantity
        },
        success: function (response) {
            if (response.success) {
                // Actualización exitosa
                updateCartCounter();
            } else {
                alert('Error al actualizar la cantidad: ' + response.message);
            }
        },
        error: function () {
            alert('Error al actualizar la cantidad');
        }
    });
}

// Función para actualizar contador de elementos en carrito
function updateCartCounter() {
    $.ajax({
        url: '/Home/ObtenerContadorCarrito',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                $('#cart-counter').text(response.counter);
                if (response.counter > 0) {
                    $('#cart-counter').show();
                } else {
                    $('#cart-counter').hide();
                }
            }
        }
    });
}

// Funciones para gestión de inventario
function registerInventoryMovement(productId, warehouseId, movementType, quantity) {
    if (!quantity || quantity <= 0) {
        alert('La cantidad debe ser mayor que cero');
        return false;
    }

    let observation = prompt('Ingrese una observación para este movimiento (opcional)');

    $.ajax({
        url: '/Inventario/RegistrarMovimientoAjax',
        type: 'POST',
        data: {
            idProducto: productId,
            idAlmacen: warehouseId,
            tipoMovimiento: movementType,
            cantidad: quantity,
            observacion: observation
        },
        success: function (response) {
            if (response.success) {
                alert('Movimiento registrado correctamente');
                location.reload();
            } else {
                alert('Error al registrar el movimiento: ' + response.message);
            }
        },
        error: function () {
            alert('Error al registrar el movimiento');
        }
    });
}

// Funciones para gráficos y reportes
function initializeCharts() {
    // Si se está utilizando Chart.js
    if (typeof Chart !== 'undefined') {
        // Ejemplo de gráfico de ventas por día
        var salesChartCtx = document.getElementById('salesChart');
        if (salesChartCtx) {
            var salesData = JSON.parse(salesChartCtx.getAttribute('data-sales'));
            var salesChart = new Chart(salesChartCtx, {
                type: 'line',
                data: {
                    labels: salesData.map(function (d) { return d.date; }),
                    datasets: [{
                        label: 'Ventas',
                        data: salesData.map(function (d) { return d.amount; }),
                        backgroundColor: 'rgba(52, 152, 219, 0.2)',
                        borderColor: 'rgba(52, 152, 219, 1)',
                        borderWidth: 2,
                        tension: 0.4
                    }]
                },
                options: {
                    responsive: true,
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                callback: function (value) {
                                    return '₡' + value.toLocaleString();
                                }
                            }
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    return 'Ventas: ₡' + context.parsed.y.toLocaleString();
                                }
                            }
                        }
                    }
                }
            });
        }

        // Ejemplo de gráfico de distribución de inventario por categoría
        var inventoryChartCtx = document.getElementById('inventoryChart');
        if (inventoryChartCtx) {
            var inventoryData = JSON.parse(inventoryChartCtx.getAttribute('data-inventory'));
            var inventoryChart = new Chart(inventoryChartCtx, {
                type: 'pie',
                data: {
                    labels: inventoryData.map(function (d) { return d.category; }),
                    datasets: [{
                        data: inventoryData.map(function (d) { return d.quantity; }),
                        backgroundColor: [
                            'rgba(52, 152, 219, 0.7)',
                            'rgba(46, 204, 113, 0.7)',
                            'rgba(155, 89, 182, 0.7)',
                            'rgba(241, 196, 15, 0.7)',
                            'rgba(231, 76, 60, 0.7)',
                            'rgba(52, 73, 94, 0.7)'
                        ],
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    }
                }
            });
        }
    }
}

// Función para la búsqueda en tiempo real
function initializeSearch() {
    var searchTimer;

    $('#searchInput').on('keyup', function () {
        clearTimeout(searchTimer);
        var query = $(this).val();

        if (query.length < 3) {
            $('#searchResults').empty().hide();
            return;
        }

        searchTimer = setTimeout(function () {
            $.ajax({
                url: '/Home/BuscarAjax',
                type: 'GET',
                data: { q: query },
                success: function (results) {
                    var resultsContainer = $('#searchResults');
                    resultsContainer.empty();

                    if (results.length === 0) {
                        resultsContainer.append('<p class="text-muted p-3">No se encontraron resultados</p>');
                    } else {
                        var list = $('<div class="list-group"></div>');
                        results.forEach(function (result) {
                            list.append(`
                                <a href="${result.url}" class="list-group-item list-group-item-action">
                                    <div class="d-flex w-100 justify-content-between">
                                        <h5 class="mb-1">${result.name}</h5>
                                        <small>${result.type}</small>
                                    </div>
                                    <p class="mb-1">${result.description}</p>
                                    <small>${result.price ? '₡' + result.price.toFixed(2) : ''}</small>
                                </a>
                            `);
                        });
                        resultsContainer.append(list);
                    }

                    resultsContainer.show();
                }
            });
        }, 500);
    });

    // Ocultar resultados al hacer clic fuera del campo de búsqueda
    $(document).on('click', function (e) {
        if (!$(e.target).closest('#searchContainer').length) {
            $('#searchResults').hide();
        }
    });
}

// Función para integración con WhatsApp
function initializeWhatsAppIntegration() {
    $('#sendWhatsAppBtn').on('click', function () {
        var phone = $('#whatsappPhone').val();
        var message = $('#whatsappMessage').val();

        if (!phone || !message) {
            alert('Por favor ingrese un número de teléfono y un mensaje');
            return;
        }

        $('#whatsappForm').submit();
    });

    // Plantillas predefinidas
    $('.template-select').on('change', function () {
        var template = $(this).val();
        if (template) {
            $('#whatsappMessage').val(template);
        }
    });
}

// Función para la visualización de pedidos
function initializeOrderVisualization() {
    $('.order-status-select').on('change', function () {
        var orderId = $(this).data('order-id');
        var newStatus = $(this).val();

        if (confirm('¿Está seguro de cambiar el estado del pedido a ' + newStatus + '?')) {
            $.ajax({
                url: '/Pedido/ActualizarEstadoAjax',
                type: 'POST',
                data: {
                    id: orderId,
                    nuevoEstado: newStatus
                },
                success: function (response) {
                    if (response.success) {
                        // Actualizar visualmente
                        $('#order-status-badge-' + orderId)
                            .removeClass('estado-pendiente estado-procesando estado-enviado estado-entregado estado-cancelado')
                            .addClass('estado-' + newStatus.toLowerCase())
                            .text(newStatus);

                        alert('Estado actualizado correctamente');
                    } else {
                        alert('Error al actualizar el estado: ' + response.message);
                    }
                },
                error: function () {
                    alert('Error al actualizar el estado');
                }
            });
        } else {
            // Restaurar valor original
            $(this).val($(this).data('original-status'));
        }
    });
}

// Inicializar funciones específicas según la página
$(document).ready(function () {
    // Inicializar gráficos si estamos en la página de reportes
    if ($('.report-chart').length) {
        initializeCharts();
    }

    // Inicializar búsqueda si existe el campo
    if ($('#searchInput').length) {
        initializeSearch();
    }

    // Inicializar integración con WhatsApp si estamos en esa página
    if ($('#whatsappForm').length) {
        initializeWhatsAppIntegration();
    }

    // Inicializar visualización de pedidos
    if ($('.order-status-select').length) {
        initializeOrderVisualization();
    }

    // Inicializar contador de carrito
    updateCartCounter();
});