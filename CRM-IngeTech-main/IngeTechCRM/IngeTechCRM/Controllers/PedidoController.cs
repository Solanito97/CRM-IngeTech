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
    public class PedidoController : Controller
    {
        private readonly IngeTechDbContext _context;

        public PedidoController(IngeTechDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string buscar, string estado, DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var pedidosQuery = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Provincia)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(buscar))
            {
                int pedidoId;
                if (int.TryParse(buscar, out pedidoId))
                {
                    pedidosQuery = pedidosQuery.Where(p => p.ID_PEDIDO == pedidoId);
                }
                else
                {
                    pedidosQuery = pedidosQuery.Where(p =>
                        p.Usuario.NOMBRE_COMPLETO.Contains(buscar) ||
                        p.DIRECCION_ENVIO.Contains(buscar) ||
                        p.NOTAS.Contains(buscar));
                }
            }

            if (!string.IsNullOrEmpty(estado))
            {
                pedidosQuery = pedidosQuery.Where(p => p.ESTADO == estado);
            }

            if (fechaInicio.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.FECHA_PEDIDO >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                // Ajustar fecha fin para incluir todo el día
                var fechaFinAjustada = fechaFin.Value.Date.AddDays(1).AddTicks(-1);
                pedidosQuery = pedidosQuery.Where(p => p.FECHA_PEDIDO <= fechaFinAjustada);
            }

            var pedidos = pedidosQuery
                .OrderByDescending(p => p.FECHA_PEDIDO)
                .ToList();

            ViewBag.Estados = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos" },
                new SelectListItem { Value = "PENDIENTE", Text = "Pendiente", Selected = estado == "PENDIENTE" },
                new SelectListItem { Value = "PROCESANDO", Text = "Procesando", Selected = estado == "PROCESANDO" },
                new SelectListItem { Value = "ENVIADO", Text = "Enviado", Selected = estado == "ENVIADO" },
                new SelectListItem { Value = "ENTREGADO", Text = "Entregado", Selected = estado == "ENTREGADO" },
                new SelectListItem { Value = "CANCELADO", Text = "Cancelado", Selected = estado == "CANCELADO" }
            };

            ViewBag.Buscar = buscar;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            return View(pedidos);
        }

        public IActionResult Detalles(int id)
        {
            // Verificar si el usuario está autenticado
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var pedido = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Provincia)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .ThenInclude(p => p.Categoria)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .ThenInclude(p => p.Marca)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.AlmacenOrigen)
                .FirstOrDefault(p => p.ID_PEDIDO == id);

            if (pedido == null)
            {
                return NotFound();
            }

            // Verificar que el pedido pertenezca al usuario actual o sea un administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (pedido.ID_USUARIO != usuarioId && tipoUsuarioId != 1)
            {
                return Forbid();
            }

            return View(pedido);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ActualizarEstado(int id, string nuevoEstado)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }

            // Validar el nuevo estado
            var estadosValidos = new[] { "PENDIENTE", "PROCESANDO", "ENVIADO", "ENTREGADO", "CANCELADO" };
            if (!estadosValidos.Contains(nuevoEstado))
            {
                TempData["Error"] = "Estado no válido";
                return RedirectToAction("Detalles", new { id });
            }

            // Si se está cancelando un pedido, devolver los productos al inventario
            if (nuevoEstado == "CANCELADO" && pedido.ESTADO != "CANCELADO")
            {
                await DevolverProductosAlInventario(id);
            }

            pedido.ESTADO = nuevoEstado;
            _context.Update(pedido);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Estado del pedido actualizado exitosamente";
            return RedirectToAction("Detalles", new { id });
        }

        private async Task DevolverProductosAlInventario(int idPedido)
        {
            var detalles = await _context.DetallesPedido
                .Where(d => d.ID_PEDIDO == idPedido)
                .ToListAsync();

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId").Value;

            foreach (var detalle in detalles)
            {
                // Buscar inventario correspondiente
                var inventario = await _context.Inventarios
                    .FirstOrDefaultAsync(i => i.ID_PRODUCTO == detalle.ID_PRODUCTO && i.ID_ALMACEN == detalle.ID_ALMACEN_ORIGEN);

                if (inventario != null)
                {
                    // Actualizar inventario
                    inventario.CANTIDAD += detalle.CANTIDAD;
                    inventario.FECHA_ACTUALIZACION = DateTime.Now;
                    _context.Update(inventario);

                    // Registrar movimiento de inventario
                    var movimiento = new MovimientoInventario
                    {
                        ID_PRODUCTO = detalle.ID_PRODUCTO,
                        ID_ALMACEN = detalle.ID_ALMACEN_ORIGEN,
                        TIPO_MOVIMIENTO = "ENTRADA",
                        CANTIDAD = detalle.CANTIDAD,
                        FECHA_MOVIMIENTO = DateTime.Now,
                        ID_USUARIO = usuarioId,
                        OBSERVACION = $"Devolución por cancelación de pedido #{idPedido}"
                    };

                    _context.MovimientosInventario.Add(movimiento);
                }
            }

            await _context.SaveChangesAsync();
        }

        public IActionResult MisPedidos()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var pedidos = _context.Pedidos
                .Include(p => p.Provincia)
                .Where(p => p.ID_USUARIO == usuarioId)
                .OrderByDescending(p => p.FECHA_PEDIDO)
                .ToList();

            return View(pedidos);
        }

        [HttpPost]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }

            // Verificar que el pedido pertenezca al usuario actual
            if (pedido.ID_USUARIO != usuarioId)
            {
                return Forbid();
            }

            // Solo se pueden cancelar pedidos pendientes o en procesamiento
            if (pedido.ESTADO != "PENDIENTE" && pedido.ESTADO != "PROCESANDO")
            {
                TempData["Error"] = "No se puede cancelar un pedido que ya ha sido enviado o entregado";
                return RedirectToAction("MisPedidos");
            }

            // Cambiar estado a cancelado
            pedido.ESTADO = "CANCELADO";
            _context.Update(pedido);

            // Devolver productos al inventario
            await DevolverProductosAlInventario(id);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Pedido cancelado exitosamente";
            return RedirectToAction("MisPedidos");
        }

        public IActionResult Crear()
        {
            // Esta funcionalidad se implementa en el HomeController con MiCarrito y Checkout
            // Los pedidos se crean cuando el cliente finaliza su compra
            return RedirectToAction("Index", "Home");
        }
    }
}