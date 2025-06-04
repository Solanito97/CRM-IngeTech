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
using iTextSharp.text.pdf;
using iTextSharp.text;
using OfficeOpenXml;

namespace IngeTechCRM.Controllers
{
    public class InventarioController : Controller
    {
        private readonly IngeTechDbContext _context;

        public InventarioController(IngeTechDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? almacenId, int? categoriaId, string buscar)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var inventariosQuery = _context.Inventarios
                .Include(i => i.Producto)
                .ThenInclude(p => p.Categoria)
                .Include(i => i.Producto)
                .ThenInclude(p => p.Marca)
                .Include(i => i.Almacen)
                .ThenInclude(a => a.Provincia)
                .AsQueryable();

            // Filtrar por almacén si se especifica y es mayor que 0
            if (almacenId.HasValue && almacenId.Value > 0)
            {
                inventariosQuery = inventariosQuery.Where(i => i.ID_ALMACEN == almacenId.Value);
            }

            // Filtrar por categoría si se especifica y es mayor que 0
            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                inventariosQuery = inventariosQuery.Where(i => i.Producto.ID_CATEGORIA == categoriaId.Value);
            }

            // Filtrar por texto de búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                buscar = buscar.ToLower();
                inventariosQuery = inventariosQuery.Where(i =>
                    i.Producto.NOMBRE.ToLower().Contains(buscar) ||
                    i.Producto.CODIGO.ToLower().Contains(buscar) ||
                    i.Producto.DESCRIPCION.ToLower().Contains(buscar));
            }

            var inventarios = inventariosQuery
                .OrderBy(i => i.Almacen.NOMBRE)
                .ThenBy(i => i.Producto.NOMBRE)
                .ToList();

            // Preparar listas para filtros usando List<SelectListItem>
            var almacenes = _context.Almacenes.OrderBy(a => a.NOMBRE).ToList();
            var categorias = _context.Categorias.OrderBy(c => c.NOMBRE).ToList();

            var almacenesItems = new List<SelectListItem>();
            almacenesItems.Add(new SelectListItem
            {
                Value = "0",
                Text = "Todos los almacenes",
                Selected = !almacenId.HasValue || almacenId.Value == 0
            });

            foreach (var almacen in almacenes)
            {
                almacenesItems.Add(new SelectListItem
                {
                    Value = almacen.ID_ALMACEN.ToString(),
                    Text = almacen.NOMBRE,
                    Selected = almacenId.HasValue && almacen.ID_ALMACEN == almacenId.Value
                });
            }

            var categoriasItems = new List<SelectListItem>();
            categoriasItems.Add(new SelectListItem
            {
                Value = "0",
                Text = "Todas las categorías",
                Selected = !categoriaId.HasValue || categoriaId.Value == 0
            });

            foreach (var categoria in categorias)
            {
                categoriasItems.Add(new SelectListItem
                {
                    Value = categoria.ID_CATEGORIA.ToString(),
                    Text = categoria.NOMBRE,
                    Selected = categoriaId.HasValue && categoria.ID_CATEGORIA == categoriaId.Value
                });
            }

            // Contar alertas (si tienes esa funcionalidad)
            var totalAlertas = _context.AlertasInventario.Count(a => !a.PROCESADA);

            // Configurar ViewBag
            ViewBag.Almacenes = almacenesItems;
            ViewBag.Categorias = categoriasItems;
            ViewBag.AlmacenId = almacenId ?? 0;
            ViewBag.CategoriaId = categoriaId ?? 0;
            ViewBag.Buscar = buscar;
            ViewBag.TotalAlertas = totalAlertas;

            // Identificar productos con stock bajo
            foreach (var inventario in inventarios)
            {
                inventario.Producto.ACTIVO = inventario.CANTIDAD <= inventario.CANTIDAD_MINIMA;
            }

            return View(inventarios);
        }

        [Authorize]
        public IActionResult Detalles(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var inventario = _context.Inventarios
                .Include(i => i.Producto)
                .ThenInclude(p => p.Categoria)
                .Include(i => i.Producto)
                .ThenInclude(p => p.Marca)
                .Include(i => i.Almacen)
                .ThenInclude(a => a.Provincia)
                .FirstOrDefault(i => i.ID_INVENTARIO == id);

            if (inventario == null)
            {
                return NotFound();
            }

            // Obtener historial de movimientos
            var movimientos = _context.MovimientosInventario
                .Include(m => m.Usuario)
                .Where(m => m.ID_PRODUCTO == inventario.ID_PRODUCTO && m.ID_ALMACEN == inventario.ID_ALMACEN)
                .OrderByDescending(m => m.FECHA_MOVIMIENTO)
                .Take(20)
                .ToList();

            ViewBag.Movimientos = movimientos;

            return View(inventario);
        }

        [Authorize]
        public IActionResult Crear()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // En lugar de usar SelectList, usamos listas simples
            ViewBag.ProductosList = _context.Productos.Where(p => p.ACTIVO).ToList();
            ViewBag.AlmacenesList = _context.Almacenes.ToList();

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Inventario inventario)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.Remove("Producto");
            ModelState.Remove("Almacen");
            ModelState.Remove("Alertas");

            if (ModelState.IsValid)
            {
                // Verificar si ya existe un registro para este producto y almacén
                var inventarioExistente = await _context.Inventarios
                    .FirstOrDefaultAsync(i => i.ID_PRODUCTO == inventario.ID_PRODUCTO && i.ID_ALMACEN == inventario.ID_ALMACEN);

                if (inventarioExistente != null)
                {
                    ModelState.AddModelError("", "Ya existe un registro de inventario para este producto en este almacén");
                    ViewBag.ProductosList = _context.Productos.Where(p => p.ACTIVO).ToList();
                    ViewBag.AlmacenesList = _context.Almacenes.ToList();
                    return View(inventario);
                }

                inventario.FECHA_ACTUALIZACION = DateTime.Now;
                _context.Add(inventario);
                await _context.SaveChangesAsync();

                // Registrar movimiento de inventario
                var usuarioId = HttpContext.Session.GetInt32("UsuarioId").Value;
                var movimiento = new MovimientoInventario
                {
                    ID_PRODUCTO = inventario.ID_PRODUCTO,
                    ID_ALMACEN = inventario.ID_ALMACEN,
                    TIPO_MOVIMIENTO = "ENTRADA",
                    CANTIDAD = inventario.CANTIDAD,
                    FECHA_MOVIMIENTO = DateTime.Now,
                    ID_USUARIO = usuarioId,
                    OBSERVACION = "Entrada al inventario"
                };

                _context.MovimientosInventario.Add(movimiento);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Inventario creado exitosamente";
                return RedirectToAction(nameof(Index));
            }

            // Si el modelo no es válido, volvemos a cargar las listas
            ViewBag.ProductosList = _context.Productos.Where(p => p.ACTIVO).ToList();
            ViewBag.AlmacenesList = _context.Almacenes.ToList();
            return View(inventario);
        }

        [Authorize]
        public IActionResult Editar(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var inventario = _context.Inventarios
                .Include(i => i.Producto)
                .Include(i => i.Almacen)
                .FirstOrDefault(i => i.ID_INVENTARIO == id);

            if (inventario == null)
            {
                return NotFound();
            }

            // En lugar de SelectList, usamos listas simples
            ViewBag.ProductosList = _context.Productos.Where(p => p.ACTIVO).ToList();
            ViewBag.AlmacenesList = _context.Almacenes.ToList();

            return View(inventario);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Inventario inventario)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != inventario.ID_INVENTARIO)
            {
                return NotFound();
            }

            // Limpiar errores de ModelState relacionados con propiedades de navegación
            ModelState.Remove("Producto");
            ModelState.Remove("Almacen");
            ModelState.Remove("Alertas");

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener inventario original para calcular diferencia
                    var inventarioOriginal = await _context.Inventarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.ID_INVENTARIO == id);

                    if (inventarioOriginal == null)
                    {
                        return NotFound();
                    }

                    // Calcular diferencia en cantidad
                    int diferencia = inventario.CANTIDAD - inventarioOriginal.CANTIDAD;

                    // Iniciar transacción explícita
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // 1. Actualizar el inventario usando SQL directo para evitar problemas con el trigger
                        var fechaActualizacion = DateTime.Now;

                        await _context.Database.ExecuteSqlRawAsync(
                            "UPDATE CRM_INVENTARIO SET CANTIDAD = {0}, CANTIDAD_MINIMA = {1}, FECHA_ACTUALIZACION = {2} WHERE ID_INVENTARIO = {3}",
                            inventario.CANTIDAD,
                            inventario.CANTIDAD_MINIMA,
                            fechaActualizacion,
                            id);

                        // 2. Registrar movimiento de inventario si hay diferencia
                        if (diferencia != 0)
                        {
                            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
                            if (usuarioId == null)
                            {
                                await transaction.RollbackAsync();
                                TempData["Error"] = "No se encontró información del usuario en la sesión";
                                return RedirectToAction("Index", "Home");
                            }

                            // Crear el movimiento en memoria
                            var movimiento = new MovimientoInventario
                            {
                                ID_PRODUCTO = inventarioOriginal.ID_PRODUCTO,
                                ID_ALMACEN = inventarioOriginal.ID_ALMACEN,
                                TIPO_MOVIMIENTO = diferencia > 0 ? "ENTRADA" : "SALIDA",
                                CANTIDAD = Math.Abs(diferencia),
                                FECHA_MOVIMIENTO = DateTime.Now,
                                ID_USUARIO = usuarioId.Value,
                                OBSERVACION = "Ajuste manual de inventario"
                            };

                            // Agregar movimiento usando EF Core (esto debería ser seguro)
                            _context.MovimientosInventario.Add(movimiento);
                            await _context.SaveChangesAsync();
                        }

                        // Confirmar la transacción
                        await transaction.CommitAsync();

                        TempData["Message"] = "Inventario actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // Si hay algún error, hacer rollback
                        await transaction.RollbackAsync();
                        throw; // Relanzar la excepción para manejarla en el catch exterior
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!InventarioExists(inventario.ID_INVENTARIO))
                    {
                        return NotFound();
                    }
                    else
                    {
                        // Registrar el error para depuración
                        Console.WriteLine($"Error de concurrencia: {ex.Message}");
                        ModelState.AddModelError("", "Error de concurrencia al actualizar el inventario. Otro usuario puede haber modificado el registro.");
                    }
                }
                catch (Exception ex)
                {
                    // Capturar cualquier otra excepción
                    ModelState.AddModelError("", $"Error al actualizar el inventario: {ex.Message}");
                }
            }
            else
            {
                // Si hay errores de validación, los agregamos al ModelState
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Error de validación: {error.ErrorMessage}");
                }
            }

            // Si llegamos aquí, algo falló. Recargamos el inventario completo con sus relaciones
            var inventarioCompleto = await _context.Inventarios
                .Include(i => i.Producto)
                .Include(i => i.Almacen)
                .FirstOrDefaultAsync(i => i.ID_INVENTARIO == id);

            if (inventarioCompleto == null)
            {
                return NotFound();
            }

            // Actualizamos manualmente los valores editados para mantenerlos en el formulario
            inventarioCompleto.CANTIDAD = inventario.CANTIDAD;
            inventarioCompleto.CANTIDAD_MINIMA = inventario.CANTIDAD_MINIMA;

            // Recargamos las listas para el formulario
            ViewBag.ProductosList = await _context.Productos.Where(p => p.ACTIVO).ToListAsync();
            ViewBag.AlmacenesList = await _context.Almacenes.ToListAsync();

            return View(inventarioCompleto);
        }



        [Authorize]
        public IActionResult RegistrarMovimiento()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Imprimir información de depuración - opcional, solo para verificar
            var productos = _context.Productos.Where(p => p.ACTIVO).ToList();
            var almacenes = _context.Almacenes.ToList();

            // Verificar los nombres exactos de las propiedades del modelo
            if (productos.Any())
            {
                var primerProducto = productos.First();
                // Si deseas puedes agregar logging aquí para ver las propiedades
            }

            // Crear las listas de SelectListItem asegurándonos de usar el ID correcto
            List<SelectListItem> productoItems = new List<SelectListItem>();
            foreach (var producto in productos)
            {
                productoItems.Add(new SelectListItem
                {
                    Value = producto.ID_PRODUCTO.ToString(),  // Asegúrate que sea ID_PRODUCTO y no IdProducto
                    Text = producto.NOMBRE  // Asegúrate que sea Nombre y no nombre 
                });
            }

            List<SelectListItem> almacenItems = new List<SelectListItem>();
            foreach (var almacen in almacenes)
            {
                almacenItems.Add(new SelectListItem
                {
                    Value = almacen.ID_ALMACEN.ToString(),  // Asegúrate que sea ID_ALMACEN y no IdAlmacen
                    Text = almacen.NOMBRE  // Asegúrate que sea Nombre y no nombre
                });
            }

            ViewBag.Productos = productoItems;
            ViewBag.Almacenes = almacenItems;
            ViewBag.TiposMovimiento = new List<SelectListItem>
            {
                new SelectListItem { Value = "ENTRADA", Text = "Entrada" },
                new SelectListItem { Value = "SALIDA", Text = "Salida" },
                new SelectListItem { Value = "AJUSTE", Text = "Ajuste" }
            };

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarMovimiento(MovimientoInventario movimiento)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener ID del usuario en sesión
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                // Verificar si es una solicitud AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest("No se pudo identificar el usuario en sesión");
                }

                TempData["Error"] = "No se encontró información del usuario en la sesión";
                return RedirectToAction("Index", "Home");
            }

            // Completar datos del movimiento
            movimiento.ID_USUARIO = usuarioId.Value;
            movimiento.FECHA_MOVIMIENTO = DateTime.Now;

            // Eliminar errores de propiedades de navegación si existen
            ModelState.Remove("Producto");
            ModelState.Remove("Almacen");
            ModelState.Remove("Usuario");

            if (ModelState.IsValid)
            {
                try
                {
                    // Iniciar transacción explícita
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Buscar inventario existente
                        var inventario = await _context.Inventarios
                            .FirstOrDefaultAsync(i => i.ID_PRODUCTO == movimiento.ID_PRODUCTO && i.ID_ALMACEN == movimiento.ID_ALMACEN);

                        bool crearInventario = false;

                        if (inventario == null)
                        {
                            if (movimiento.TIPO_MOVIMIENTO == "ENTRADA")
                            {
                                // Crear nuevo registro de inventario
                                inventario = new Inventario
                                {
                                    ID_PRODUCTO = movimiento.ID_PRODUCTO,
                                    ID_ALMACEN = movimiento.ID_ALMACEN,
                                    CANTIDAD = 0, // Se actualizará después
                                    CANTIDAD_MINIMA = 5, // Valor por defecto
                                    FECHA_ACTUALIZACION = DateTime.Now
                                };
                                _context.Inventarios.Add(inventario);
                                await _context.SaveChangesAsync();
                                crearInventario = true;
                            }
                            else
                            {
                                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                                {
                                    return BadRequest("No existe inventario para este producto en este almacén");
                                }

                                ModelState.AddModelError("", "No existe inventario para este producto en este almacén");
                                PrepararListasParaVista(movimiento);
                                return View(movimiento);
                            }
                        }

                        // Calcular nueva cantidad según tipo de movimiento
                        int nuevaCantidad = inventario.CANTIDAD;

                        switch (movimiento.TIPO_MOVIMIENTO)
                        {
                            case "ENTRADA":
                                nuevaCantidad += movimiento.CANTIDAD;
                                break;
                            case "SALIDA":
                                if (inventario.CANTIDAD < movimiento.CANTIDAD)
                                {
                                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                                    {
                                        return BadRequest("No hay suficiente stock disponible");
                                    }

                                    ModelState.AddModelError("", "No hay suficiente stock disponible");
                                    PrepararListasParaVista(movimiento);
                                    return View(movimiento);
                                }
                                nuevaCantidad -= movimiento.CANTIDAD;
                                break;
                            case "AJUSTE":
                                // Para ajustes, la cantidad puede ser positiva (entrada) o negativa (salida)
                                if (movimiento.CANTIDAD < 0 && inventario.CANTIDAD < Math.Abs(movimiento.CANTIDAD))
                                {
                                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                                    {
                                        return BadRequest("No hay suficiente stock para el ajuste negativo");
                                    }

                                    ModelState.AddModelError("", "No hay suficiente stock disponible para el ajuste negativo");
                                    PrepararListasParaVista(movimiento);
                                    return View(movimiento);
                                }
                                nuevaCantidad += movimiento.CANTIDAD;
                                break;
                        }

                        // Actualizar inventario usando SQL directo para evitar problemas con el trigger
                        await _context.Database.ExecuteSqlRawAsync(
                            "UPDATE CRM_INVENTARIO SET CANTIDAD = {0}, FECHA_ACTUALIZACION = {1} WHERE ID_INVENTARIO = {2}",
                            nuevaCantidad,
                            DateTime.Now,
                            inventario.ID_INVENTARIO);

                        // Registrar el movimiento
                        _context.MovimientosInventario.Add(movimiento);
                        await _context.SaveChangesAsync();

                        // *** AUTO-PROCESAR ALERTAS PENDIENTES ***
                        if (movimiento.TIPO_MOVIMIENTO == "ENTRADA" && nuevaCantidad > inventario.CANTIDAD_MINIMA)
                        {
                            // Auto-procesar alertas pendientes para este inventario
                            var alertasPendientes = await _context.AlertasInventario
                                .Where(a => a.ID_INVENTARIO == inventario.ID_INVENTARIO && !a.PROCESADA)
                                .ToListAsync();

                            if (alertasPendientes.Any())
                            {
                                foreach (var alertaPendiente in alertasPendientes)
                                {
                                    alertaPendiente.PROCESADA = true;
                                }
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Verificar si se debe crear una alerta de stock bajo
                        if (nuevaCantidad <= inventario.CANTIDAD_MINIMA)
                        {
                            // Verificar si ya existe una alerta no procesada
                            var alertaExistente = await _context.AlertasInventario
                                .AnyAsync(a => a.ID_INVENTARIO == inventario.ID_INVENTARIO && !a.PROCESADA);

                            if (!alertaExistente)
                            {
                                var alerta = new AlertaInventario
                                {
                                    ID_INVENTARIO = inventario.ID_INVENTARIO,
                                    FECHA_ALERTA = DateTime.Now,
                                    PROCESADA = false
                                };

                                _context.AlertasInventario.Add(alerta);
                                await _context.SaveChangesAsync();
                            }
                        }

                        await transaction.CommitAsync();

                        // Verificar si es una solicitud AJAX
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Ok("Movimiento registrado exitosamente");
                        }

                        TempData["Message"] = "Movimiento registrado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw; // Re-lanzar la excepción para manejarla en el bloque catch exterior
                    }
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return BadRequest("Error al registrar el movimiento: " + ex.Message);
                    }

                    ModelState.AddModelError("", "Error al registrar el movimiento: " + ex.Message);
                }
            }
            else if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Si es AJAX y hay errores de validación, retornar BadRequest
                var errores = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest("Datos inválidos: " + errores);
            }

            // Si llegamos aquí es porque hubo un error y no es una petición AJAX
            PrepararListasParaVista(movimiento);
            return View(movimiento);
        }

        // Método auxiliar para preparar las listas para la vista
        private void PrepararListasParaVista(MovimientoInventario movimiento)
        {
            List<SelectListItem> productoItems = _context.Productos
                .Where(p => p.ACTIVO)
                .Select(p => new SelectListItem
                {
                    Value = p.ID_PRODUCTO.ToString(),
                    Text = p.NOMBRE,
                    Selected = p.ID_PRODUCTO == movimiento.ID_PRODUCTO
                })
                .ToList();

            List<SelectListItem> almacenItems = _context.Almacenes
                .Select(a => new SelectListItem
                {
                    Value = a.ID_ALMACEN.ToString(),
                    Text = a.NOMBRE,
                    Selected = a.ID_ALMACEN == movimiento.ID_ALMACEN
                })
                .ToList();

            ViewBag.Productos = productoItems;
            ViewBag.Almacenes = almacenItems;
            ViewBag.TiposMovimiento = new List<SelectListItem>
            {
                new SelectListItem { Value = "ENTRADA", Text = "Entrada", Selected = movimiento.TIPO_MOVIMIENTO == "ENTRADA" },
                new SelectListItem { Value = "SALIDA", Text = "Salida", Selected = movimiento.TIPO_MOVIMIENTO == "SALIDA" },
                new SelectListItem { Value = "AJUSTE", Text = "Ajuste", Selected = movimiento.TIPO_MOVIMIENTO == "AJUSTE" }
             };
        }

        [Authorize]
        public IActionResult Movimientos(int? productoId, int? almacenId, string tipoMovimiento, DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var movimientosQuery = _context.MovimientosInventario
                .Include(m => m.Producto)
                .Include(m => m.Almacen)
                .Include(m => m.Usuario)
                .AsQueryable();

            // Aplicar filtros
            if (productoId.HasValue)
            {
                movimientosQuery = movimientosQuery.Where(m => m.ID_PRODUCTO == productoId.Value);
            }

            if (almacenId.HasValue)
            {
                movimientosQuery = movimientosQuery.Where(m => m.ID_ALMACEN == almacenId.Value);
            }

            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                movimientosQuery = movimientosQuery.Where(m => m.TIPO_MOVIMIENTO == tipoMovimiento);
            }

            if (fechaInicio.HasValue)
            {
                movimientosQuery = movimientosQuery.Where(m => m.FECHA_MOVIMIENTO >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                // Ajustar fecha fin para incluir todo el día
                var fechaFinAjustada = fechaFin.Value.Date.AddDays(1).AddTicks(-1);
                movimientosQuery = movimientosQuery.Where(m => m.FECHA_MOVIMIENTO <= fechaFinAjustada);
            }

            var movimientos = movimientosQuery
                .OrderByDescending(m => m.FECHA_MOVIMIENTO)
                .ToList();

            // Crear listas de SelectListItem directamente
            var productos = _context.Productos.ToList();
            var productoItems = new List<SelectListItem>();
            foreach (var producto in productos)
            {
                productoItems.Add(new SelectListItem
                {
                    Value = producto.ID_PRODUCTO.ToString(),
                    Text = producto.NOMBRE,
                    Selected = productoId.HasValue && producto.ID_PRODUCTO == productoId.Value
                });
            }

            var almacenes = _context.Almacenes.ToList();
            var almacenItems = new List<SelectListItem>();
            foreach (var almacen in almacenes)
            {
                almacenItems.Add(new SelectListItem
                {
                    Value = almacen.ID_ALMACEN.ToString(),
                    Text = almacen.NOMBRE,
                    Selected = almacenId.HasValue && almacen.ID_ALMACEN == almacenId.Value
                });
            }

            ViewBag.Productos = productoItems;
            ViewBag.Almacenes = almacenItems;
            ViewBag.TiposMovimiento = new List<SelectListItem>
            {
                 new SelectListItem { Value = "", Text = "Todos" },
                 new SelectListItem { Value = "ENTRADA", Text = "Entrada", Selected = tipoMovimiento == "ENTRADA" },
                 new SelectListItem { Value = "SALIDA", Text = "Salida", Selected = tipoMovimiento == "SALIDA" },
                 new SelectListItem { Value = "AJUSTE", Text = "Ajuste", Selected = tipoMovimiento == "AJUSTE" },
                 new SelectListItem { Value = "TRANSFERENCIA", Text = "Transferencia", Selected = tipoMovimiento == "TRANSFERENCIA" }
            };
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;

            return View(movimientos);
        }

        [Authorize]
        public async Task<IActionResult> Alertas(bool? procesadas)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Verificar y procesar alertas automáticamente antes de cargar la vista
            await VerificarYProcesarAlertasAutomaticamente();

            // Forzar recarga del contexto para obtener datos actualizados
            _context.ChangeTracker.Clear();

            var mostrarProcesadas = procesadas ?? false;

            var alertasQuery = _context.AlertasInventario
                .Include(a => a.Inventario)
                    .ThenInclude(i => i.Producto)
                        .ThenInclude(p => p.Categoria)
                .Include(a => a.Inventario)
                    .ThenInclude(i => i.Producto)
                        .ThenInclude(p => p.Marca)
                .Include(a => a.Inventario)
                    .ThenInclude(i => i.Almacen)
                .Where(a => a.PROCESADA == mostrarProcesadas)
                .AsQueryable();

            var alertas = await alertasQuery
                .OrderByDescending(a => a.FECHA_ALERTA)
                .ToListAsync();

            ViewBag.MostrarProcesadas = mostrarProcesadas;

            return View(alertas);
        }


        private async Task VerificarYProcesarAlertasAutomaticamente()
        {
            try
            {
                // Obtener todas las alertas pendientes con sus relaciones
                var alertasPendientes = await _context.AlertasInventario
                    .Include(a => a.Inventario)
                    .Where(a => !a.PROCESADA)
                    .ToListAsync();

                if (!alertasPendientes.Any())
                    return;

                var alertasParaProcesar = new List<AlertaInventario>();

                foreach (var alerta in alertasPendientes)
                {
                    // Verificar si el stock actual ya está por encima del mínimo
                    if (alerta.Inventario.CANTIDAD > alerta.Inventario.CANTIDAD_MINIMA)
                    {
                        alertasParaProcesar.Add(alerta);
                    }
                }

                if (alertasParaProcesar.Any())
                {
                    // Marcar alertas como procesadas usando SQL directo para asegurar persistencia
                    var idsAlertas = alertasParaProcesar.Select(a => a.ID_ALERTA).ToList();
                    var idsString = string.Join(",", idsAlertas);

                    await _context.Database.ExecuteSqlRawAsync(
                        $"UPDATE CRM_ALERTA_INVENTARIO SET PROCESADA = 1 WHERE ID_ALERTA IN ({idsString})"
                    );

                    // También actualizar en el contexto actual para consistencia
                    foreach (var alerta in alertasParaProcesar)
                    {
                        alerta.PROCESADA = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log del error si tienes sistema de logging
                // Por ahora, continuamos sin romper el flujo
                Console.WriteLine($"Error al verificar alertas: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProcesarAlerta(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var alerta = await _context.AlertasInventario.FindAsync(id);
            if (alerta == null)
            {
                return NotFound();
            }

            alerta.PROCESADA = true;
            _context.Update(alerta);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Alertas));
        }

        [Authorize]
        public IActionResult AdministrarAlmacenes()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var almacenes = _context.Almacenes
                .Include(a => a.Provincia)
                .ToList();

            return View(almacenes);
        }

        [Authorize]
        public IActionResult CrearAlmacen()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Provincias = new SelectList(_context.Provincias, "IdProvincia", "Nombre");
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearAlmacen(Almacen almacen)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                _context.Add(almacen);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Almacén creado exitosamente";
                return RedirectToAction(nameof(AdministrarAlmacenes));
            }

            ViewBag.Provincias = new SelectList(_context.Provincias, "IdProvincia", "Nombre", almacen.ID_PROVINCIA);
            return View(almacen);
        }

        [Authorize]
        public async Task<IActionResult> EditarAlmacen(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var almacen = await _context.Almacenes.FindAsync(id);
            if (almacen == null)
            {
                return NotFound();
            }

            ViewBag.Provincias = new SelectList(_context.Provincias, "IdProvincia", "Nombre", almacen.ID_PROVINCIA);
            return View(almacen);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarAlmacen(int id, Almacen almacen)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != almacen.ID_ALMACEN)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(almacen);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AlmacenExists(almacen.ID_ALMACEN))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["Message"] = "Almacén actualizado exitosamente";
                return RedirectToAction(nameof(AdministrarAlmacenes));
            }

            ViewBag.Provincias = new SelectList(_context.Provincias, "IdProvincia", "Nombre", almacen.ID_PROVINCIA);
            return View(almacen);
        }

        private bool InventarioExists(int id)
        {
            return _context.Inventarios.Any(e => e.ID_INVENTARIO == id);
        }

        private bool AlmacenExists(int id)
        {
            return _context.Almacenes.Any(e => e.ID_ALMACEN == id);
        }


        [HttpGet]
    public async Task<IActionResult> ExportarExcel(int? almacenId, int? categoriaId, string buscar)
    {
        // Obtener los mismos datos que se muestran en la vista
        var inventario = await ObtenerInventarioFiltrado(almacenId, categoriaId, buscar);
        
        // Configurar licencia EPPlus (requerido desde EPPlus 5.0)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Inventario");
            
            // Estilo de encabezados
            using (var range = worksheet.Cells["A1:J1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(63, 81, 181));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }
            
            // Encabezados
            worksheet.Cells[1, 1].Value = "Producto";
            worksheet.Cells[1, 2].Value = "Código";
            worksheet.Cells[1, 3].Value = "Categoría";
            worksheet.Cells[1, 4].Value = "Marca";
            worksheet.Cells[1, 5].Value = "Almacén";
            worksheet.Cells[1, 6].Value = "Provincia";
            worksheet.Cells[1, 7].Value = "Cantidad";
            worksheet.Cells[1, 8].Value = "Mínimo";
            worksheet.Cells[1, 9].Value = "Estado";
            worksheet.Cells[1, 10].Value = "Última Actualización";
            
            // Datos
            int row = 2;
            foreach (var item in inventario)
            {
                worksheet.Cells[row, 1].Value = item.Producto.NOMBRE;
                worksheet.Cells[row, 2].Value = item.Producto.CODIGO;
                worksheet.Cells[row, 3].Value = item.Producto.Categoria.NOMBRE;
                worksheet.Cells[row, 4].Value = item.Producto.Marca.NOMBRE;
                worksheet.Cells[row, 5].Value = item.Almacen.NOMBRE;
                worksheet.Cells[row, 6].Value = item.Almacen.Provincia.NOMBRE;
                worksheet.Cells[row, 7].Value = item.CANTIDAD;
                worksheet.Cells[row, 8].Value = item.CANTIDAD_MINIMA;
                
                // Determinar estado del stock
                string estadoStock = item.CANTIDAD <= item.CANTIDAD_MINIMA 
                    ? "Stock Bajo" 
                    : (item.CANTIDAD <= item.CANTIDAD_MINIMA * 1.5 ? "Stock Medio" : "Stock Óptimo");
                
                worksheet.Cells[row, 9].Value = estadoStock;
                worksheet.Cells[row, 10].Value = item.FECHA_ACTUALIZACION.ToString("dd/MM/yyyy HH:mm");
                
                // Colorear celdas de estado según el valor
                if (estadoStock == "Stock Bajo")
                {
                    worksheet.Cells[row, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
                }
                else if (estadoStock == "Stock Medio")
                {
                    worksheet.Cells[row, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                }
                else
                {
                    worksheet.Cells[row, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }
                
                row++;
            }
            
            // Autoajustar columnas
            worksheet.Cells.AutoFitColumns();
            
            // Crear nombre del archivo
            string fecha = DateTime.Now.ToString("yyyyMMdd");
            string fileName = $"Inventario_{fecha}.xlsx";
            
            // Devolver el archivo
            var content = package.GetAsByteArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportarPDF(int? almacenId, int? categoriaId, string buscar)
    {
        // Obtener los mismos datos que se muestran en la vista
        var inventario = await ObtenerInventarioFiltrado(almacenId, categoriaId, buscar);
        
        // Crear documento PDF
        using (MemoryStream ms = new MemoryStream())
        {
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, ms);
            
            document.Open();
            
            // Fuentes
            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.BLACK);
            Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.DARK_GRAY);
            Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
            Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
            
            // Título
            Paragraph title = new Paragraph("Reporte de Inventario", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 15f;
            document.Add(title);
            
            // Información de filtros
            Paragraph infoFiltros = new Paragraph();
            infoFiltros.Add(new Chunk($"Fecha: {DateTime.Now.ToString("dd/MM/yyyy")}", subtitleFont));
            
            if (almacenId.HasValue)
            {
                var almacen = await _context.Almacenes.FindAsync(almacenId.Value);
                infoFiltros.Add(new Chunk($"\nAlmacén: {almacen?.NOMBRE ?? "Todos"}", subtitleFont));
            }
            
            if (categoriaId.HasValue)
            {
                var categoria = await _context.Categorias.FindAsync(categoriaId.Value);
                infoFiltros.Add(new Chunk($"\nCategoría: {categoria?.NOMBRE ?? "Todas"}", subtitleFont));
            }
            
            if (!string.IsNullOrEmpty(buscar))
            {
                infoFiltros.Add(new Chunk($"\nBúsqueda: {buscar}", subtitleFont));
            }
            
            infoFiltros.Alignment = Element.ALIGN_LEFT;
            infoFiltros.SpacingAfter = 20f;
            document.Add(infoFiltros);
            
            // Tabla
            PdfPTable table = new PdfPTable(8);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 4, 2, 2.5f, 2.5f, 3, 1.5f, 1.5f, 2.5f });
            
            // Encabezados
            BaseColor headerColor = new BaseColor(63, 81, 181);
            
            PdfPCell cell1 = new PdfPCell(new Phrase("Producto", headerFont));
            cell1.BackgroundColor = headerColor;
            cell1.Padding = 5;
            table.AddCell(cell1);
            
            PdfPCell cell2 = new PdfPCell(new Phrase("Código", headerFont));
            cell2.BackgroundColor = headerColor;
            cell2.Padding = 5;
            table.AddCell(cell2);
            
            PdfPCell cell3 = new PdfPCell(new Phrase("Categoría", headerFont));
            cell3.BackgroundColor = headerColor;
            cell3.Padding = 5;
            table.AddCell(cell3);
            
            PdfPCell cell4 = new PdfPCell(new Phrase("Marca", headerFont));
            cell4.BackgroundColor = headerColor;
            cell4.Padding = 5;
            table.AddCell(cell4);
            
            PdfPCell cell5 = new PdfPCell(new Phrase("Almacén", headerFont));
            cell5.BackgroundColor = headerColor;
            cell5.Padding = 5;
            table.AddCell(cell5);
            
            PdfPCell cell6 = new PdfPCell(new Phrase("Cantidad", headerFont));
            cell6.BackgroundColor = headerColor;
            cell6.Padding = 5;
            cell6.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell6);
            
            PdfPCell cell7 = new PdfPCell(new Phrase("Mínimo", headerFont));
            cell7.BackgroundColor = headerColor;
            cell7.Padding = 5;
            cell7.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell7);
            
            PdfPCell cell8 = new PdfPCell(new Phrase("Estado", headerFont));
            cell8.BackgroundColor = headerColor;
            cell8.Padding = 5;
            cell8.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell8);
            
            // Datos
            foreach (var item in inventario)
            {
                table.AddCell(new PdfPCell(new Phrase(item.Producto.NOMBRE, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Producto.CODIGO, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Producto.Categoria.NOMBRE, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Producto.Marca.NOMBRE, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Almacen.NOMBRE, cellFont)));
                
                PdfPCell cantidadCell = new PdfPCell(new Phrase(item.CANTIDAD.ToString(), cellFont));
                cantidadCell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(cantidadCell);
                
                PdfPCell minimoCell = new PdfPCell(new Phrase(item.CANTIDAD_MINIMA.ToString(), cellFont));
                minimoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(minimoCell);
                
                // Determinar estado del stock
                string estadoStock = item.CANTIDAD <= item.CANTIDAD_MINIMA 
                    ? "Stock Bajo" 
                    : (item.CANTIDAD <= item.CANTIDAD_MINIMA * 1.5 ? "Stock Medio" : "Stock Óptimo");
                
                PdfPCell estadoCell = new PdfPCell(new Phrase(estadoStock, cellFont));
                estadoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                
                // Colorear celdas de estado según el valor
                if (estadoStock == "Stock Bajo")
                {
                    estadoCell.BackgroundColor = new BaseColor(255, 200, 200);
                }
                else if (estadoStock == "Stock Medio")
                {
                    estadoCell.BackgroundColor = new BaseColor(255, 255, 200);
                }
                else
                {
                    estadoCell.BackgroundColor = new BaseColor(200, 255, 200);
                }
                
                table.AddCell(estadoCell);
            }
            
            document.Add(table);
            document.Close();
            
            // Crear nombre del archivo
            string fecha = DateTime.Now.ToString("yyyyMMdd");
            string fileName = $"Inventario_{fecha}.pdf";
            
            // Devolver el archivo
            return File(ms.ToArray(), "application/pdf", fileName);
        }
    }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RegistrarMovimientoRapido(int idProducto, int idAlmacen, string tipoMovimiento, int cantidad, string observacion = "")
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return Json(new { success = false, message = "No tiene permisos para realizar esta acción" });
            }

            // Obtener ID del usuario en sesión
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return Json(new { success = false, message = "No se encontró información del usuario en la sesión" });
            }

            try
            {
                // Validaciones básicas
                if (cantidad <= 0)
                {
                    return Json(new { success = false, message = "La cantidad debe ser mayor a 0" });
                }

                if (string.IsNullOrEmpty(tipoMovimiento) || (tipoMovimiento != "ENTRADA" && tipoMovimiento != "SALIDA"))
                {
                    return Json(new { success = false, message = "Tipo de movimiento inválido" });
                }

                // Crear el objeto MovimientoInventario
                var movimiento = new MovimientoInventario
                {
                    ID_PRODUCTO = idProducto,
                    ID_ALMACEN = idAlmacen,
                    TIPO_MOVIMIENTO = tipoMovimiento,
                    CANTIDAD = cantidad,
                    FECHA_MOVIMIENTO = DateTime.Now,
                    ID_USUARIO = usuarioId.Value,
                    OBSERVACION = string.IsNullOrEmpty(observacion) ? $"Movimiento rápido desde detalles - {tipoMovimiento.ToLower()}" : observacion
                };

                // Iniciar transacción explícita
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Buscar inventario existente
                    var inventario = await _context.Inventarios
                        .FirstOrDefaultAsync(i => i.ID_PRODUCTO == idProducto && i.ID_ALMACEN == idAlmacen);

                    if (inventario == null)
                    {
                        if (tipoMovimiento == "ENTRADA")
                        {
                            // Crear nuevo registro de inventario
                            inventario = new Inventario
                            {
                                ID_PRODUCTO = idProducto,
                                ID_ALMACEN = idAlmacen,
                                CANTIDAD = 0, // Se actualizará después
                                CANTIDAD_MINIMA = 5, // Valor por defecto
                                FECHA_ACTUALIZACION = DateTime.Now
                            };
                            _context.Inventarios.Add(inventario);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            return Json(new { success = false, message = "No existe inventario para este producto en este almacén" });
                        }
                    }

                    // Calcular nueva cantidad según tipo de movimiento
                    int nuevaCantidad = inventario.CANTIDAD;

                    if (tipoMovimiento == "ENTRADA")
                    {
                        nuevaCantidad += cantidad;
                    }
                    else if (tipoMovimiento == "SALIDA")
                    {
                        if (inventario.CANTIDAD < cantidad)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"No hay suficiente stock disponible. Stock actual: {inventario.CANTIDAD} unidades, cantidad solicitada: {cantidad} unidades."
                            });
                        }
                        nuevaCantidad -= cantidad;
                    }

                    // Actualizar inventario usando SQL directo para evitar problemas con el trigger
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE CRM_INVENTARIO SET CANTIDAD = {0}, FECHA_ACTUALIZACION = {1} WHERE ID_INVENTARIO = {2}",
                        nuevaCantidad,
                        DateTime.Now,
                        inventario.ID_INVENTARIO);

                    // Registrar el movimiento
                    _context.MovimientosInventario.Add(movimiento);
                    await _context.SaveChangesAsync();

                    // *** AUTO-PROCESAR ALERTAS PENDIENTES ***
                    if (tipoMovimiento == "ENTRADA" && nuevaCantidad > inventario.CANTIDAD_MINIMA)
                    {
                        // Auto-procesar alertas pendientes para este inventario
                        var alertasPendientes = await _context.AlertasInventario
                            .Where(a => a.ID_INVENTARIO == inventario.ID_INVENTARIO && !a.PROCESADA)
                            .ToListAsync();

                        if (alertasPendientes.Any())
                        {
                            foreach (var alertaPendiente in alertasPendientes)
                            {
                                alertaPendiente.PROCESADA = true;
                            }
                            await _context.SaveChangesAsync();
                        }
                    }

                    // Verificar si se debe crear una alerta de stock bajo
                    if (nuevaCantidad <= inventario.CANTIDAD_MINIMA)
                    {
                        // Verificar si ya existe una alerta no procesada
                        var alertaExistente = await _context.AlertasInventario
                            .AnyAsync(a => a.ID_INVENTARIO == inventario.ID_INVENTARIO && !a.PROCESADA);

                        if (!alertaExistente)
                        {
                            var alerta = new AlertaInventario
                            {
                                ID_INVENTARIO = inventario.ID_INVENTARIO,
                                FECHA_ALERTA = DateTime.Now,
                                PROCESADA = false
                            };

                            _context.AlertasInventario.Add(alerta);
                            await _context.SaveChangesAsync();
                        }
                    }

                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Movimiento registrado exitosamente",
                        nuevoStock = nuevaCantidad
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error al registrar el movimiento: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al procesar la solicitud: " + ex.Message });
            }
        }

        // Método para obtener inventario filtrado (reutiliza lógica del Index)
        private async Task<List<Inventario>> ObtenerInventarioFiltrado(int? almacenId, int? categoriaId, string buscar)
        {
        // Consulta base
        var query = _context.Inventarios
            .Include(i => i.Producto)
                .ThenInclude(p => p.Categoria)
            .Include(i => i.Producto)
                .ThenInclude(p => p.Marca)
            .Include(i => i.Almacen)
                .ThenInclude(a => a.Provincia)
            .AsQueryable();
        
        // Aplicar filtros
        if (almacenId.HasValue)
        {
            query = query.Where(i => i.ID_ALMACEN == almacenId.Value);
        }
        
        if (categoriaId.HasValue)
        {
            query = query.Where(i => i.Producto.ID_CATEGORIA == categoriaId.Value);
        }
        
        if (!string.IsNullOrEmpty(buscar))
        {
            buscar = buscar.ToLower();
            query = query.Where(i => 
                i.Producto.NOMBRE.ToLower().Contains(buscar) ||
                i.Producto.CODIGO.ToLower().Contains(buscar) ||
                i.Producto.Categoria.NOMBRE.ToLower().Contains(buscar) ||
                i.Producto.Marca.NOMBRE.ToLower().Contains(buscar) ||
                i.Almacen.NOMBRE.ToLower().Contains(buscar)
            );
        }
        
        // Ordenar resultados
        query = query.OrderBy(i => i.Producto.NOMBRE);
        
        return await query.ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerStockActual(int productoId, int almacenId)
        {
            try
            {
                // Buscar el inventario para el producto y almacén específicos
                var inventario = await _context.Inventarios
                    .Where(i => i.ID_PRODUCTO == productoId && i.ID_ALMACEN == almacenId)
                    .FirstOrDefaultAsync();

                if (inventario != null)
                {
                    return Json(new
                    {
                        success = true,
                        stock = inventario.CANTIDAD,
                        stockMinimo = inventario.CANTIDAD_MINIMA
                    });
                }
                else
                {
                    // No existe inventario para este producto en este almacén
                    return Json(new
                    {
                        success = false,
                        stock = 0,
                        message = "No existe inventario para este producto en este almacén"
                    });
                }
            }
            catch (Exception ex)
            {
                // Log del error si tienes sistema de logging
                // _logger.LogError(ex, "Error al obtener stock actual");

                return Json(new
                {
                    success = false,
                    stock = 0,
                    error = "Error al consultar el stock: " + ex.Message
                });
            }
        }
    }
}