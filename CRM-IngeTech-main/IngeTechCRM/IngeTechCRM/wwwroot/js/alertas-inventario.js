$(document).ready(function () {
    // Interceptar formularios de procesar alerta
    $('form[action*="ProcesarAlerta"]').on('submit', function (e) {
        e.preventDefault();

        const form = this;
        const fila = $(this).closest('tr');
        const producto = fila.find('td:nth-child(2) a').text().trim();
        const almacen = fila.find('td:nth-child(3)').text().trim();
        const estado = fila.find('td:nth-child(4) span').text().trim();
        const stock = fila.find('td:nth-child(5)').text().trim();
        const minimo = fila.find('td:nth-child(6)').text().trim();

        // Determinar color del badge según el estado
        const badgeClass = estado === 'Crítico' ? 'swal2-badge-danger' : 'swal2-badge-success';

        Swal.fire({
            title: '¿Procesar alerta?',
            html: `
                <div style="text-align: left;">
                    <div style="background-color: #f8f9fa; padding: 1rem; border-radius: 0.5rem; margin-bottom: 1rem;">
                        <p style="margin-bottom: 0.5rem;"><strong>Producto:</strong> ${producto}</p>
                        <p style="margin-bottom: 0.5rem;"><strong>Almacén:</strong> ${almacen}</p>
                        <p style="margin-bottom: 0.5rem;"><strong>Estado:</strong> <span class="${badgeClass}">${estado}</span></p>
                        <p style="margin-bottom: 0.5rem;"><strong>Stock actual:</strong> ${stock}</p>
                        <p style="margin-bottom: 0;"><strong>Stock mínimo:</strong> ${minimo}</p>
                    </div>
                    <div style="background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 0.75rem; border-radius: 0.375rem; color: #856404;">
                        <i class="fas fa-info-circle"></i>
                        <strong>Nota:</strong> Esta acción no modificará el stock del producto.
                    </div>
                </div>
            `,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#28a745',
            cancelButtonColor: '#6c757d',
            confirmButtonText: '<i class="fas fa-check"></i> Sí, procesar',
            cancelButtonText: '<i class="fas fa-times"></i> Cancelar',
            reverseButtons: true,
            customClass: {
                popup: 'swal2-popup-custom',
                htmlContainer: 'swal2-html-container-custom'
            },
            didOpen: () => {
                // Agregar estilos para los badges después de que el modal se abra
                const style = document.createElement('style');
                style.textContent = `
                    .swal2-badge-danger {
                        background-color: #dc3545 !important;
                        color: white !important;
                        padding: 0.25em 0.6em !important;
                        border-radius: 0.375rem !important;
                        font-size: 0.75em !important;
                    }
                    .swal2-badge-success {
                        background-color: #28a745 !important;
                        color: white !important;
                        padding: 0.25em 0.6em !important;
                        border-radius: 0.375rem !important;
                        font-size: 0.75em !important;
                    }
                `;
                document.head.appendChild(style);
            }
        }).then((result) => {
            if (result.isConfirmed) {
                // Mostrar loading
                Swal.fire({
                    title: 'Procesando alerta...',
                    text: 'Marcando como procesada',
                    icon: 'info',
                    allowOutsideClick: false,
                    showConfirmButton: false,
                    didOpen: () => {
                        Swal.showLoading();
                    }
                });

                // Enviar formulario después de 1 segundo
                setTimeout(() => {
                    form.submit();
                }, 1000);
            }
        });
    });
});