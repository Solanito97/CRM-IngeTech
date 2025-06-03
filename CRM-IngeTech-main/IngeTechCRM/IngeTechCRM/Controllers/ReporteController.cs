using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using IngeTechCRM.Models;

namespace IngeTechCRM.Controllers
{
    [Authorize]
    public class ReporteController : Controller
    {
        private readonly IngeTechDbContext _context;

        public ReporteController(IngeTechDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public IActionResult InventarioPorAlmacen()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener resumen de inventario por almacén
            var inventarioPorAlmacen = _context.Almacenes
                .Include(a => a.Provincia)
                .Select(a => new
                {
                    Almacen = a,
                    TotalProductos = _context.Inventarios
                        .Count(i => i.ID_ALMACEN == a.ID_ALMACEN),
                    ValorTotal = _context.Inventarios
                        .Where(i => i.ID_ALMACEN == a.ID_ALMACEN)
                        .Join(_context.Productos,
                            i => i.ID_PRODUCTO,
                            p => p.ID_PRODUCTO,
                            (i, p) => (decimal?)(i.CANTIDAD * p.PRECIO)) // Cast to nullable decimal
                        .Sum() ?? 0M, // Sum nullable decimals and coalesce to 0M if null (empty sequence)
                    ProductosStockBajo = _context.Inventarios
                        .Count(i => i.ID_ALMACEN == a.ID_ALMACEN && i.CANTIDAD <= i.CANTIDAD_MINIMA)
                })
                .ToList();

            var reporte = inventarioPorAlmacen.Select(x => new InventarioAlmacenViewModel
            {
                ID_ALMACEN = x.Almacen.ID_ALMACEN,
                NOMBRE_ALMACEN = x.Almacen.NOMBRE,
                PROVINCIA = x.Almacen.Provincia.NOMBRE,
                TOTAL_PRODUCTOS = x.TotalProductos,
                VALOR_TOTAL = x.ValorTotal,
                PRODUCTOS_STOCK_BAJO = x.ProductosStockBajo
            }).ToList();

            return View(reporte);
        }

        public IActionResult DetalleInventarioPorAlmacen(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var almacen = _context.Almacenes
                .Include(a => a.Provincia)
                .FirstOrDefault(a => a.ID_ALMACEN == id);

            if (almacen == null)
            {
                return NotFound();
            }

            var inventarios = _context.Inventarios
                .Include(i => i.Producto)
                .ThenInclude(p => p.Categoria)
                .Include(i => i.Producto)
                .ThenInclude(p => p.Marca)
                .Where(i => i.ID_ALMACEN == id)
                .OrderBy(i => i.Producto.NOMBRE)
                .ToList();

            ViewBag.Almacen = almacen;
            ViewBag.ValorTotal = inventarios.Sum(i => i.CANTIDAD * i.Producto.PRECIO);

            return View(inventarios);
        }

        public IActionResult InventarioPorCategoria()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener resumen de inventario por categoría
            var inventarioPorCategoria = _context.Categorias
                .Select(c => new
                {
                    Categoria = c,
                    TotalProductos = _context.Productos
                        .Count(p => p.ID_CATEGORIA == c.ID_CATEGORIA),
                    UnidadesTotales = _context.Inventarios
                        .Join(_context.Productos.Where(p => p.ID_CATEGORIA == c.ID_CATEGORIA),
                            i => i.ID_PRODUCTO,
                            p => p.ID_PRODUCTO,
                            (i, p) => (int?)i.CANTIDAD) // Cast to nullable int
                        .Sum() ?? 0, // Sum nullable ints and coalesce to 0 if null
                    ValorTotal = _context.Inventarios
                        .Join(_context.Productos.Where(p => p.ID_CATEGORIA == c.ID_CATEGORIA),
                            i => i.ID_PRODUCTO,
                            p => p.ID_PRODUCTO,
                            (i, p) => (decimal?)(i.CANTIDAD * p.PRECIO)) // Cast to nullable decimal
                        .Sum() ?? 0M // Sum nullable decimals and coalesce to 0M if null
                })
                .ToList();

            var reporte = inventarioPorCategoria.Select(x => new InventarioCategoriaViewModel
            {
                ID_CATEGORIA = x.Categoria.ID_CATEGORIA,
                NOMBRE_CATEGORIA = x.Categoria.NOMBRE,
                TOTAL_PRODUCTOS = x.TotalProductos,
                UNIDADES_TOTALES = x.UnidadesTotales,
                VALOR_TOTAL = x.ValorTotal
            }).ToList();

            return View(reporte);
        }

        public IActionResult DetalleInventarioPorCategoria(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var categoria = _context.Categorias
                .FirstOrDefault(c => c.ID_CATEGORIA == id);

            if (categoria == null)
            {
                return NotFound();
            }

            var productos = _context.Productos
                .Include(p => p.Marca)
                .Where(p => p.ID_CATEGORIA == id && p.ACTIVO)
                .ToList();

            // Obtener inventario total por producto
            var inventarioPorProducto = productos.Select(p => new ProductoInventarioViewModel
            {
                ID_PRODUCTO = p.ID_PRODUCTO,
                CODIGO = p.CODIGO,
                NOMBRE = p.NOMBRE,
                MARCA = p.Marca.NOMBRE,
                PRECIO = p.PRECIO,
                CANTIDAD_TOTAL = _context.Inventarios
                    .Where(i => i.ID_PRODUCTO == p.ID_PRODUCTO)
                    .Sum(i => i.CANTIDAD),
                VALOR_TOTAL = _context.Inventarios
                    .Where(i => i.ID_PRODUCTO == p.ID_PRODUCTO)
                    .Sum(i => i.CANTIDAD) * p.PRECIO
            }).ToList();

            ViewBag.Categoria = categoria;
            ViewBag.ValorTotal = inventarioPorProducto.Sum(p => p.VALOR_TOTAL);

            return View(inventarioPorProducto);
        }

        public IActionResult VentasPorPeriodo()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Fechas por defecto: último mes
            var fechaFin = DateTime.Now.Date;
            var fechaInicio = fechaFin.AddMonths(-1);

            ViewBag.FechaInicio = fechaInicio.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpPost]
        public IActionResult VentasPorPeriodo(DateTime fechaInicio, DateTime fechaFin)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Ajustar fecha fin para incluir todo el día
            var fechaFinAjustada = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Obtener ventas por día en el período
            var ventasPorDia = _context.Pedidos
                .Where(p => p.FECHA_PEDIDO >= fechaInicio && p.FECHA_PEDIDO <= fechaFinAjustada && p.ESTADO != "CANCELADO")
                .GroupBy(p => p.FECHA_PEDIDO.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    TotalPedidos = g.Count(),
                    MontoTotal = g.Sum(p => p.TOTAL)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            // Obtener ventas por provincia
            var ventasPorProvincia = _context.Pedidos
                .Include(p => p.Provincia)
                .Where(p => p.FECHA_PEDIDO >= fechaInicio && p.FECHA_PEDIDO <= fechaFinAjustada && p.ESTADO != "CANCELADO")
                .GroupBy(p => new { p.ID_PROVINCIA, p.Provincia.NOMBRE })
                .Select(g => new
                {
                    IdProvincia = g.Key.ID_PROVINCIA,
                    Provincia = g.Key.NOMBRE,
                    TotalPedidos = g.Count(),
                    MontoTotal = g.Sum(p => p.TOTAL)
                })
                .OrderByDescending(x => x.MontoTotal)
                .ToList();

            // Obtener productos más vendidos
            var productosMasVendidos = _context.DetallesPedido
                .Include(d => d.Pedido)
                .Include(d => d.Producto)
                .ThenInclude(p => p.Categoria)
                .Where(d => d.Pedido.FECHA_PEDIDO >= fechaInicio && d.Pedido.FECHA_PEDIDO <= fechaFinAjustada && d.Pedido.ESTADO != "CANCELADO")
                .GroupBy(d => new { d.ID_PRODUCTO, d.Producto.CODIGO, d.Producto.NOMBRE })
                .Select(g => new
                {
                    IdProducto = g.Key.ID_PRODUCTO,
                    Codigo = g.Key.CODIGO,
                    Producto = g.Key.NOMBRE,
                    Categoria = g.Key.NOMBRE,
                    UnidadesVendidas = g.Sum(d => d.CANTIDAD),
                    MontoTotal = g.Sum(d => d.CANTIDAD * d.PRECIO_UNITARIO)
                })
                .OrderByDescending(x => x.UnidadesVendidas)
                .Take(10)
                .ToList();

            // Crear objetos para la vista
            var reporteVentasDiarias = ventasPorDia.Select(x => new VentasDiariasViewModel
            {
                FECHA = x.Fecha,
                TOTAL_PEDIDOS = x.TotalPedidos,
                MONTO_TOTAL = x.MontoTotal
            }).ToList();

            var reporteVentasProvincia = ventasPorProvincia.Select(x => new VentasProvinciaViewModel
            {
                ID_PROVINCIA = x.IdProvincia,
                PROVINCIA = x.Provincia,
                TOTAL_PEDIDOS = x.TotalPedidos,
                MONTO_TOTAL = x.MontoTotal
            }).ToList();

            var reporteProductosMasVendidos = productosMasVendidos.Select(x => new ProductosMasVendidosViewModel
            {
                ID_PRODCUTO = x.IdProducto,
                CODIGO = x.Codigo,
                PRODUCTO = x.Producto,
                CATEGORIA = x.Categoria,
                UNIDADES_VENDIDAS = x.UnidadesVendidas,
                MONTO_TOTAL = x.MontoTotal
            }).ToList();

            // Totales
            ViewBag.TotalPedidos = ventasPorDia.Sum(x => x.TotalPedidos);
            ViewBag.MontoTotalVentas = ventasPorDia.Sum(x => x.MontoTotal);
            ViewBag.PromedioVentasDiarias = ventasPorDia.Count > 0 ? ventasPorDia.Average(x => x.MontoTotal) : 0;

            ViewBag.FechaInicio = fechaInicio.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin.ToString("yyyy-MM-dd");
            ViewBag.VentasDiarias = reporteVentasDiarias;
            ViewBag.VentasProvincia = reporteVentasProvincia;
            ViewBag.ProductosMasVendidos = reporteProductosMasVendidos;

            return View();
        }

        public IActionResult ClientesPorProvincia()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Buscar el tipo de usuario 'Cliente' (normalmente tiene ID=2)
            var tipoClienteId = _context.TiposUsuario
                .Where(t => t.DESCRIPCION == "Cliente")
                .Select(t => t.ID_TIPO_USUARIO)
                .FirstOrDefault();

            // Obtener distribución de clientes por provincia
            var clientesPorProvincia = _context.Provincias
                .Select(p => new
                {
                    Provincia = p,
                    TotalClientes = _context.Usuarios
                        .Count(u => u.ID_PROVINCIA == p.ID_PROVINCIA && u.ID_TIPO_USUARIO == tipoClienteId),
                    ClientesActivos = _context.Usuarios
                        .Count(u => u.ID_PROVINCIA == p.ID_PROVINCIA && u.ID_TIPO_USUARIO == tipoClienteId &&
                                u.ULTIMO_ACCESO >= DateTime.Now.AddMonths(-1))
                })
                .ToList();

            var reporte = clientesPorProvincia.Select(x => new ClientesProvinciaViewModel
            {
                ID_PROVINCIA = x.Provincia.ID_PROVINCIA,
                PROVINCIA = x.Provincia.NOMBRE,
                TOTAL_CLIENTES = x.TotalClientes,
                CLIENTES_ACTIVOS = x.ClientesActivos,
                PORCENTAJE_ACTIVOS = x.TotalClientes > 0 ? (double)x.ClientesActivos / x.TotalClientes * 100 : 0
            }).ToList();

            return View(reporte);
        }

        public IActionResult DetalleClientesPorProvincia(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var provincia = _context.Provincias
                .FirstOrDefault(p => p.ID_PROVINCIA == id);

            if (provincia == null)
            {
                return NotFound();
            }

            // Buscar el tipo de usuario 'Cliente' (normalmente tiene ID=2)
            var tipoClienteId = _context.TiposUsuario
                .Where(t => t.DESCRIPCION == "Cliente")
                .Select(t => t.ID_TIPO_USUARIO)
                .FirstOrDefault();

            var clientes = _context.Usuarios
                .Where(u => u.ID_PROVINCIA == id && u.ID_TIPO_USUARIO == tipoClienteId)
                .OrderBy(u => u.NOMBRE_COMPLETO)
                .ToList();

            ViewBag.Provincia = provincia;
            ViewBag.TotalClientes = clientes.Count;
            ViewBag.ClientesActivos = clientes.Count(c => c.ULTIMO_ACCESO >= DateTime.Now.AddMonths(-1));

            return View(clientes);
        }

        public IActionResult EfectividadComunicados()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener comunicados con estadísticas de envío
            var comunicados = _context.Comunicados
                .Where(c => c.FECHA_ENVIO_REAL.HasValue)
                .Select(c => new
                {
                    Comunicado = c,
                    TotalEnvios = _context.EnviosComunicado.Count(e => e.ID_COMUNICADO == c.ID_COMUNICADO)
                })
                .ToList();

            var reporte = comunicados.Select(x => new ComunicadoEfectividadViewModel
            {
                ID_COMUNICADO = x.Comunicado.ID_COMUNICADO,
                TITULO = x.Comunicado.TITULO,
                FECHA_CREACION = x.Comunicado.FECHA_CREACION,
                FECHA_ENVIO = x.Comunicado.FECHA_ENVIO_REAL.Value,
                TOTAL_ENVIOS = x.TotalEnvios
            }).ToList();

            return View(reporte);
        }

        public IActionResult DetalleEfectividadComunicado(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var comunicado = _context.Comunicados
                .Include(c => c.UsuarioCreador)
                .Include(c => c.Segmentos)
                .ThenInclude(s => s.Provincia)
                .Include(c => c.Segmentos)
                .ThenInclude(s => s.TipoUsuario)
                .FirstOrDefault(c => c.ID_COMUNICADO == id);

            if (comunicado == null || !comunicado.FECHA_ENVIO_REAL.HasValue)
            {
                return NotFound();
            }

            // Obtener información de envíos
            var envios = _context.EnviosComunicado
                .Include(e => e.UsuarioDestinatario)
                .Where(e => e.ID_COMUNICADO == id)
                .ToList();

            ViewBag.TotalEnvios = envios.Count;
            ViewBag.Envios = envios;

            // Analizar por provincia
            var enviosPorProvincia = envios
                .GroupBy(e => e.UsuarioDestinatario.Provincia.NOMBRE)
                .Select(g => new
                {
                    Provincia = g.Key,
                    TotalEnvios = g.Count()
                })
                .ToList();

            ViewBag.EnviosPorProvincia = enviosPorProvincia;

            return View(comunicado);
        }
    }


}