/**
 * Archivo: movimientos.js
 * Descripción: Funcionalidades para la página de movimientos de inventario
 */

$(document).ready(function () {
    // Funcionalidad de filtrado automático
    $('#productoId, #almacenId, #tipoMovimiento, #fechaInicio, #fechaFin').change(function () {
        // Enviar el formulario automáticamente
        $('#filtroForm').submit();
    });

    // Limpiar filtros
    $('#limpiarFiltros').click(function () {
        $('#productoId').val('');
        $('#almacenId').val('');
        $('#tipoMovimiento').val('');
        $('#fechaInicio').val('');
        $('#fechaFin').val('');
        // Enviar el formulario
        $('#filtroForm').submit();
    });

    // Exportar a Excel (implementación del lado del cliente)
    $('#exportarExcel').click(function () {
        exportarExcel();
    });

    // Exportar a PDF (implementación del lado del cliente)
    $('#exportarPdf').click(function () {
        exportarPDF();
    });

    // Validación de fechas
    $('#filtroForm').submit(function (e) {
        var fechaInicio = $('#fechaInicio').val();
        var fechaFin = $('#fechaFin').val();

        if (fechaInicio && fechaFin) {
            var inicio = new Date(fechaInicio);
            var fin = new Date(fechaFin);

            if (inicio > fin) {
                e.preventDefault();
                alert('La fecha de inicio no puede ser posterior a la fecha de fin.');
                return false;
            }
        }

        return true;
    });

    // Función para exportar a Excel
    function exportarExcel() {
        // Crear un nuevo libro de trabajo
        var wb = XLSX.utils.book_new();

        // Datos para la tabla
        var datos = [];

        // Encabezados de la tabla
        var encabezados = [
            "Fecha y Hora", "Producto", "Código", "Almacén", "Tipo",
            "Cantidad", "Usuario", "Observación"
        ];

        datos.push(encabezados);

        // Obtener filas de la tabla
        $('table tbody tr').each(function () {
            var fila = $(this);
            var celdas = fila.find('td');

            // Obtener valores de las celdas
            var fechaHora = $(celdas[0]).text().trim();
            var producto = $(celdas[1]).text().trim().split('\n')[0].trim();
            var codigo = $(celdas[1]).find('span.text-xs').text().trim();
            var almacen = $(celdas[2]).text().trim();
            var tipo = $(celdas[3]).text().trim();
            var cantidad = $(celdas[4]).text().trim();
            var usuario = $(celdas[5]).text().trim();
            var observacion = $(celdas[6]).text().trim();

            datos.push([
                fechaHora, producto, codigo, almacen, tipo,
                cantidad, usuario, observacion
            ]);
        });

        // Crear hoja de trabajo
        var ws = XLSX.utils.aoa_to_sheet(datos);

        // Agregar hoja al libro
        XLSX.utils.book_append_sheet(wb, ws, "Movimientos");

        // Obtener nombre de archivo basado en filtros
        var nombreArchivo = obtenerNombreArchivo("Excel");

        // Guardar archivo
        XLSX.writeFile(wb, nombreArchivo);

        // Mostrar notificación
        mostrarNotificacion('Archivo Excel generado exitosamente', 'success');
    }

    // Función para exportar a PDF
    function exportarPDF() {
        // Importar jsPDF
        const { jsPDF } = window.jspdf;

        // Crear nuevo documento PDF
        var doc = new jsPDF('l', 'mm', 'a4'); // Landscape para más espacio

        // Datos para la tabla
        var datos = [];

        // Obtener filas de la tabla
        $('table tbody tr').each(function () {
            var fila = $(this);
            var celdas = fila.find('td');

            // Obtener valores de las celdas
            var fechaHora = $(celdas[0]).text().trim();
            var producto = $(celdas[1]).text().trim().split('\n')[0].trim();
            var codigo = $(celdas[1]).find('span.text-xs').text().trim();
            var almacen = $(celdas[2]).text().trim();
            var tipo = $(celdas[3]).text().trim();
            var cantidad = $(celdas[4]).text().trim();
            var usuario = $(celdas[5]).text().trim();
            var observacion = $(celdas[6]).text().trim();

            datos.push([
                fechaHora, producto, almacen, tipo,
                cantidad, usuario, observacion
            ]);
        });

        // Título del documento
        doc.setFontSize(18);
        doc.text('Historial de Movimientos de Inventario', 14, 22);

        // Información de filtros
        doc.setFontSize(11);
        var fecha = new Date().toLocaleDateString();
        doc.text('Generado el: ' + fecha, 14, 30);

        var filtros = obtenerTextoFiltros();
        var linea = 30;

        filtros.forEach(function (filtro) {
            linea += 6;
            doc.text(filtro, 14, linea);
        });

        linea += 10;

        // Generar tabla
        doc.autoTable({
            startY: linea,
            head: [['Fecha y Hora', 'Producto', 'Almacén', 'Tipo', 'Cantidad', 'Usuario', 'Observación']],
            body: datos,
            theme: 'grid',
            headStyles: {
                fillColor: [63, 81, 181],
                textColor: 255
            },
            styles: {
                fontSize: 9,
                cellPadding: 3,
            },
            columnStyles: {
                0: { cellWidth: 25 }, // Fecha
                1: { cellWidth: 40 }, // Producto
                2: { cellWidth: 25 }, // Almacén
                3: { cellWidth: 20, halign: 'center' }, // Tipo
                4: { cellWidth: 15, halign: 'center' }, // Cantidad
                5: { cellWidth: 25 }, // Usuario
                6: { cellWidth: 40 }, // Observación
            }
        });

        // Obtener nombre de archivo basado en filtros
        var nombreArchivo = obtenerNombreArchivo("PDF");

        // Guardar archivo
        doc.save(nombreArchivo);

        // Mostrar notificación
        mostrarNotificacion('Archivo PDF generado exitosamente', 'success');
    }

    // Función para obtener texto de filtros
    function obtenerTextoFiltros() {
        var filtros = [];

        var producto = $('#productoId option:selected').text();
        if (producto && producto !== 'Todos los productos') {
            filtros.push('Producto: ' + producto);
        }

        var almacen = $('#almacenId option:selected').text();
        if (almacen && almacen !== 'Todos los almacenes') {
            filtros.push('Almacén: ' + almacen);
        }

        var tipoMov = $('#tipoMovimiento option:selected').text();
        if (tipoMov && tipoMov !== '') {
            filtros.push('Tipo de Movimiento: ' + tipoMov);
        }

        var fechaInicio = $('#fechaInicio').val();
        if (fechaInicio) {
            filtros.push('Desde: ' + formatearFecha(fechaInicio));
        }

        var fechaFin = $('#fechaFin').val();
        if (fechaFin) {
            filtros.push('Hasta: ' + formatearFecha(fechaFin));
        }

        // Si no hay filtros, agregar "Sin filtros"
        if (filtros.length === 0) {
            filtros.push('Sin filtros aplicados');
        }

        return filtros;
    }

    // Función para formatear fecha
    function formatearFecha(fechaStr) {
        var fecha = new Date(fechaStr);
        return fecha.toLocaleDateString();
    }

    // Función para obtener nombre de archivo basado en filtros
    function obtenerNombreArchivo(tipo) {
        var nombreBase = "Movimientos_Inventario";
        var fecha = new Date().toISOString().slice(0, 10);

        var productoId = $('#productoId').val();
        var almacenId = $('#almacenId').val();
        var tipoMovimiento = $('#tipoMovimiento').val();

        var nombre = nombreBase + "_" + fecha;

        if (productoId) {
            var productoTexto = $('#productoId option:selected').text().replace(/\s+/g, '_');
            nombre += "_" + productoTexto;
        }

        if (almacenId) {
            var almacenTexto = $('#almacenId option:selected').text().replace(/\s+/g, '_');
            nombre += "_" + almacenTexto;
        }

        if (tipoMovimiento) {
            nombre += "_" + tipoMovimiento;
        }

        // Limitar longitud del nombre
        if (nombre.length > 100) {
            nombre = nombre.substring(0, 100);
        }

        // Extensión según tipo
        var extension = tipo === "Excel" ? ".xlsx" : ".pdf";

        return nombre + extension;
    }

    // Función para mostrar notificaciones
    function mostrarNotificacion(mensaje, tipo) {
        // Eliminar notificaciones existentes
        $('.notificacion').remove();

        // Crear elemento de notificación
        var claseColor = tipo === 'success'
            ? 'bg-green-100 text-green-800 border-green-200'
            : 'bg-red-100 text-red-800 border-red-200';

        var icono = tipo === 'success'
            ? '<i class="fas fa-check-circle mr-2 text-green-500"></i>'
            : '<i class="fas fa-exclamation-circle mr-2 text-red-500"></i>';

        var html = `
        <div class="notificacion fixed top-4 right-4 z-50 max-w-md rounded-lg border ${claseColor} p-4 shadow-lg">
            <div class="flex items-center">
                ${icono}
                <span>${mensaje}</span>
            </div>
        </div>
        `;

        // Agregar a DOM
        $('body').append(html);

        // Auto eliminar después de un tiempo
        setTimeout(function () {
            $('.notificacion').fadeOut('slow', function () {
                $(this).remove();
            });
        }, 3000);
    }
});