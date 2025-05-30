using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IngeTechCRM.Models;
using Microsoft.AspNetCore.Authorization;

namespace IngeTechCRM.Controllers
{
    public class HomeController : Controller
    {
        private readonly IngeTechDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(IngeTechDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Verificar si hay un usuario en sesión
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");

            if (usuarioId.HasValue)
            {
                if (tipoUsuarioId == 1) // Si es Administrador
                {
                    return RedirectToAction("Dashboard");
                }
                else // Si es Cliente
                {
                    return RedirectToAction("Catalogo");
                }
            }

            // Mostrar información para la página principal
            var categoriasDestacadas = _context.Categorias.Take(5).ToList();
            var productosDestacados = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Imagenes)
                .Where(p => p.ACTIVO)
                .OrderByDescending(p => p.FECHA_CREACION)
                .Take(8)
                .ToList();

            ViewBag.CategoriasDestacadas = categoriasDestacadas;
            ViewBag.ProductosDestacados = productosDestacados;

            return View();
        }

        public IActionResult Dashboard()
        {
            try
            {
                // Verificar si el usuario es administrador
                var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
                if (tipoUsuarioId != 1)
                {
                    return RedirectToAction("Index");
                }

                // Obtener datos para el dashboard con manejo adecuado de errores
                ViewBag.TotalUsuarios = _context.Usuarios.Count();
                ViewBag.TotalProductos = _context.Productos.Count();
                ViewBag.TotalPedidos = _context.Pedidos.Count();

                // Alertas de inventario bajo
                var alertasInventario = _context.AlertasInventario
                    .Include(a => a.Inventario)
                    .Include(a => a.Inventario.Producto)
                    .Include(a => a.Inventario.Almacen)
                    .Where(a => !a.PROCESADA)
                    .OrderByDescending(a => a.FECHA_ALERTA)
                    .Take(10)
                    .ToList();

                // Últimos pedidos con manejo de NULL
                var ultimosPedidos = _context.Pedidos
                    .Include(p => p.Usuario)
                    .OrderByDescending(p => p.FECHA_PEDIDO)
                    .Take(5)
                    .Select(p => new {
                        ID_PEDIDO = p.ID_PEDIDO,
                        FECHA_PEDIDO = p.FECHA_PEDIDO,
                        ESTADO = p.ESTADO ?? "PENDIENTE",
                        TOTAL = p.TOTAL,
                        Usuario = p.Usuario
                    })
                    .ToList();

                // Productos más vendidos (solo en pedidos ENVIADOS o ENTREGADOS)
                List<object> productosMasVendidos = new List<object>();

                // Verificar si hay pedidos primero
                if (_context.DetallesPedido.Any())
                {
                    productosMasVendidos = _context.DetallesPedido
                        .Include(d => d.Pedido)
                        .Include(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                        .Where(d => d.Producto != null &&
                               d.Producto.Categoria != null &&
                               (d.Pedido.ESTADO == "ENVIADO" || d.Pedido.ESTADO == "ENTREGADO"))
                        .GroupBy(d => d.ID_PRODUCTO)
                        .Select(g => new {
                            Producto = g.First().Producto,
                            TotalVendido = g.Sum(d => d.CANTIDAD)
                        })
                        .OrderByDescending(x => x.TotalVendido)
                        .Take(5)
                        .ToList<object>();
                }

                // Calcular ventas mensuales (últimos 6 meses)
                var hoy = DateTime.Today;
                var fechaInicio = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-5); // Primer día de hace 5 meses

                var ventasMensuales = _context.Pedidos
                    .Where(p => p.FECHA_PEDIDO >= fechaInicio &&
                           (p.ESTADO == "ENVIADO" || p.ESTADO == "ENTREGADO"))
                    .GroupBy(p => new {
                        Año = p.FECHA_PEDIDO.Year,
                        Mes = p.FECHA_PEDIDO.Month
                    })
                    .Select(g => new {
                        Año = g.Key.Año,
                        Mes = g.Key.Mes,
                        Total = g.Sum(p => p.TOTAL)
                    })
                    .OrderBy(x => x.Año)
                    .ThenBy(x => x.Mes)
                    .ToList();

                // Generar datos para los últimos 6 meses
                var etiquetasMeses = new List<string>();
                var datosVentas = new List<decimal>();

                // Obtener los nombres de los meses en español
                var nombresMeses = new string[] {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

                // Generar lista para los últimos 6 meses
                for (int i = 5; i >= 0; i--)
                {
                    var fecha = hoy.AddMonths(-i);
                    var año = fecha.Year;
                    var mes = fecha.Month;

                    var venta = ventasMensuales.FirstOrDefault(v => v.Año == año && v.Mes == mes);
                    var total = venta != null ? venta.Total : 0;

                    etiquetasMeses.Add(nombresMeses[mes - 1]);
                    datosVentas.Add(total);
                }

                ViewBag.AlertasInventario = alertasInventario;
                ViewBag.UltimosPedidos = ultimosPedidos;
                ViewBag.ProductosMasVendidos = productosMasVendidos;
                ViewBag.EtiquetasMeses = etiquetasMeses;
                ViewBag.DatosVentas = datosVentas;

                return View();
            }
            catch (Exception ex)
            {
                // Loguear el error
                Console.WriteLine($"Error en Dashboard: {ex.Message}");

                // Mostrar mensaje de error al usuario
                TempData["Error"] = "Hubo un problema al cargar el dashboard.";

                // Inicializar ViewBags con valores vacíos/por defecto
                ViewBag.TotalUsuarios = 0;
                ViewBag.TotalProductos = 0;
                ViewBag.TotalPedidos = 0;
                ViewBag.AlertasInventario = new List<object>();
                ViewBag.UltimosPedidos = new List<object>();
                ViewBag.ProductosMasVendidos = new List<object>();
                ViewBag.EtiquetasMeses = new List<string>();
                ViewBag.DatosVentas = new List<decimal>();

                return View();
            }
        }

        public IActionResult Catalogo(int? categoriaId, int? marcaId, string buscar, int? provinciaId)
        {
            var productosQuery = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Imagenes)
                .Where(p => p.ACTIVO);

            // Filtrar por categoría si se especifica
            if (categoriaId.HasValue)
            {
                productosQuery = productosQuery.Where(p => p.ID_CATEGORIA == categoriaId.Value);
            }

            // Filtrar por marca si se especifica
            if (marcaId.HasValue)
            {
                productosQuery = productosQuery.Where(p => p.ID_MARCA == marcaId.Value);
            }

            // Filtrar por búsqueda si se especifica
            if (!string.IsNullOrEmpty(buscar))
            {
                productosQuery = productosQuery.Where(p =>
                    p.NOMBRE.Contains(buscar) ||
                    p.DESCRIPCION.Contains(buscar) ||
                    p.CODIGO.Contains(buscar));
            }

            // Obtener disponibilidad de productos en la provincia seleccionada
            var inventarioProductos = new Dictionary<int, int>();
            if (provinciaId.HasValue)
            {
                var almacenesEnProvincia = _context.Almacenes
                    .Where(a => a.ID_PROVINCIA == provinciaId.Value)
                    .Select(a => a.ID_ALMACEN)
                    .ToList();

                var inventariosEnProvincia = _context.Inventarios
                    .Where(i => almacenesEnProvincia.Contains(i.ID_ALMACEN))
                    .ToList();

                // Agrupar por producto y sumar cantidades
                foreach (var inv in inventariosEnProvincia)
                {
                    if (inventarioProductos.ContainsKey(inv.ID_PRODUCTO))
                    {
                        inventarioProductos[inv.ID_PRODUCTO] += inv.CANTIDAD;
                    }
                    else
                    {
                        inventarioProductos[inv.ID_PRODUCTO] = inv.CANTIDAD;
                    }
                }
            }
            else
            {
                // Si no se selecciona provincia, obtener inventario total
                var inventarios = _context.Inventarios.ToList();
                foreach (var inv in inventarios)
                {
                    if (inventarioProductos.ContainsKey(inv.ID_PRODUCTO))
                    {
                        inventarioProductos[inv.ID_PRODUCTO] += inv.CANTIDAD;
                    }
                    else
                    {
                        inventarioProductos[inv.ID_PRODUCTO] = inv.CANTIDAD;
                    }
                }
            }

            var productos = productosQuery.ToList();

            // Combinar productos con su inventario disponible
            var productosConInventario = productos.Select(p => new {
                Producto = p,
                CantidadDisponible = inventarioProductos.ContainsKey(p.ID_PRODUCTO) ? inventarioProductos[p.ID_PRODUCTO] : 0
            }).ToList();

            ViewBag.Categorias = _context.Categorias.ToList();
            ViewBag.Marcas = _context.Marcas.ToList();
            ViewBag.Provincias = _context.Provincias.ToList();
            ViewBag.CategoriaSeleccionada = categoriaId;
            ViewBag.MarcaSeleccionada = marcaId;
            ViewBag.BusquedaActual = buscar;
            ViewBag.ProvinciaSeleccionada = provinciaId;
            ViewBag.ProductosConInventario = productosConInventario;

            return View(productos);
        }

        public IActionResult DetalleProducto(int id)
        {
            var producto = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .Include(p => p.Imagenes)
                .FirstOrDefault(p => p.ID_PRODUCTO == id && p.ACTIVO);

            if (producto == null)
            {
                return NotFound();
            }

            // Obtener disponibilidad por provincia
            var disponibilidadPorProvincia = _context.Almacenes
                .Include(a => a.Provincia)
                .Join(_context.Inventarios.Where(i => i.ID_PRODUCTO == id),
                    almacen => almacen.ID_ALMACEN,
                    inventario => inventario.ID_ALMACEN,
                    (almacen, inventario) => new { almacen.Provincia, inventario.CANTIDAD })
                .GroupBy(x => new { x.Provincia.ID_PROVINCIA, x.Provincia.NOMBRE })
                .Select(g => new {
                    ProvinciaId = g.Key.ID_PROVINCIA,
                    ProvinciaNombre = g.Key.NOMBRE,
                    CantidadTotal = g.Sum(x => x.CANTIDAD)
                })
                .ToList();

            // Productos relacionados (misma categoría)
            var productosRelacionados = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Imagenes)
                .Where(p => p.ID_CATEGORIA == producto.ID_CATEGORIA && p.ID_PRODUCTO != producto.ID_PRODUCTO && p.ACTIVO)
                .Take(4)
                .ToList();

            ViewBag.DisponibilidadPorProvincia = disponibilidadPorProvincia;
            ViewBag.ProductosRelacionados = productosRelacionados;

            return View(producto);
        }

        [Authorize]
        public IActionResult AgregarAlCarrito(int idProducto, int cantidad = 1)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Buscar el producto
            var producto = _context.Productos.FirstOrDefault(p => p.ID_PRODUCTO == idProducto && p.ACTIVO);
            if (producto == null)
            {
                return NotFound();
            }

            // Buscar o crear un carrito activo para el usuario
            var carrito = _context.Carritos
                .FirstOrDefault(c => c.ID_USUARIO == usuarioId && c.ACTIVO);

            if (carrito == null)
            {
                carrito = new Carrito
                {
                    ID_USUARIO = usuarioId.Value,
                    FECHA_CREACION = DateTime.Now,
                    ACTIVO = true
                };
                _context.Carritos.Add(carrito);
                _context.SaveChanges();
            }

            // Verificar si el producto ya está en el carrito
            var itemCarrito = _context.ItemsCarrito
                .FirstOrDefault(i => i.ID_CARRITO == carrito.ID_CARRITO && i.ID_PRODUCTO == idProducto);

            if (itemCarrito != null)
            {
                // Actualizar cantidad
                itemCarrito.CANTIDAD += cantidad;
                _context.Update(itemCarrito);
            }
            else
            {
                // Agregar nuevo ítem
                itemCarrito = new ItemCarrito
                {
                    ID_CARRITO = carrito.ID_CARRITO,
                    ID_PRODUCTO = idProducto,
                    CANTIDAD = cantidad,
                    PRECIO_UNITARIO = producto.PRECIO,
                    FECHA_AGREGADO = DateTime.Now
                };
                _context.ItemsCarrito.Add(itemCarrito);
            }

            _context.SaveChanges();

            TempData["Message"] = "Producto agregado al carrito exitosamente";
            return RedirectToAction("MiCarrito");
        }

        public IActionResult MiCarrito()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Buscar el carrito activo del usuario con todas las relaciones necesarias
            var carrito = _context.Carritos
                .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
                .ThenInclude(p => p.Imagenes) 
                .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
                .ThenInclude(p => p.Categoria)
                .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
                .ThenInclude(p => p.Marca)
                .FirstOrDefault(c => c.ID_USUARIO == usuarioId && c.ACTIVO);

            if (carrito == null || carrito.Items.Count == 0)
            {
                ViewBag.CarritoVacio = true;
                return View(new Carrito { Items = new List<ItemCarrito>() });
            }

            // Calcular total
            var total = carrito.Items.Sum(i => i.CANTIDAD * i.PRECIO_UNITARIO);
            ViewBag.Total = total;

            return View(carrito);
        }

        [Authorize]
        [HttpPost]
        public IActionResult ActualizarCarrito(int[] idItem, int[] cantidad)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (idItem.Length != cantidad.Length)
            {
                TempData["Error"] = "Datos inválidos";
                return RedirectToAction("MiCarrito");
            }

            for (int i = 0; i < idItem.Length; i++)
            {
                var item = _context.ItemsCarrito.FirstOrDefault(it => it.ID_ITEM == idItem[i]);
                if (item != null)
                {
                    if (cantidad[i] <= 0)
                    {
                        _context.ItemsCarrito.Remove(item);
                    }
                    else
                    {
                        item.CANTIDAD = cantidad[i];
                        _context.Update(item);
                    }
                }
            }

            _context.SaveChanges();
            TempData["Message"] = "Carrito actualizado exitosamente";
            return RedirectToAction("MiCarrito");
        }

        [Authorize]
        public IActionResult EliminarDelCarrito(int idItem)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var item = _context.ItemsCarrito.FirstOrDefault(it => it.ID_ITEM == idItem);
            if (item != null)
            {
                _context.ItemsCarrito.Remove(item);
                _context.SaveChanges();
                TempData["Message"] = "Producto eliminado del carrito";
            }

            return RedirectToAction("MiCarrito");
        }

        [Authorize]
        public IActionResult Checkout()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Obtener el carrito activo
            var carrito = _context.Carritos
        .Include(c => c.Items)
        .ThenInclude(i => i.Producto)
        .ThenInclude(p => p.Imagenes)  // Crucial para resolver el error
        .Include(c => c.Items)
        .ThenInclude(i => i.Producto)
        .ThenInclude(p => p.Categoria)
        .FirstOrDefault(c => c.ID_USUARIO == usuarioId && c.ACTIVO);

            if (carrito == null || carrito.Items.Count == 0)
            {
                TempData["Error"] = "Su carrito está vacío";
                return RedirectToAction("MiCarrito");
            }

            // Obtener el usuario y sus datos
            var usuario = _context.Usuarios
                .Include(u => u.Provincia)
                .FirstOrDefault(u => u.IDENTIFICACION == usuarioId);

            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Usuario = usuario;
            ViewBag.Provincias = _context.Provincias.ToList();
            ViewBag.Total = carrito.Items.Sum(i => i.CANTIDAD * i.PRECIO_UNITARIO);

            return View(carrito);
        }

        [Authorize]
        [HttpPost]
        public IActionResult RealizarPedido(string direccionEnvio, int idProvincia, string notas)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Obtener el carrito activo
            var carrito = _context.Carritos
                .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
                .FirstOrDefault(c => c.ID_USUARIO == usuarioId && c.ACTIVO);

            if (carrito == null || carrito.Items.Count == 0)
            {
                TempData["Error"] = "Su carrito está vacío";
                return RedirectToAction("MiCarrito");
            }

            // Calcular el total
            decimal total = carrito.Items.Sum(i => i.CANTIDAD * i.PRECIO_UNITARIO);

            // Crear el pedido
            var pedido = new Pedido
            {
                ID_USUARIO = usuarioId.Value,
                FECHA_PEDIDO = DateTime.Now,
                ESTADO = "PENDIENTE",
                TOTAL = total,
                DIRECCION_ENVIO = direccionEnvio,
                ID_PROVINCIA = idProvincia,
                NOTAS = notas
            };

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            // Agregar los detalles del pedido
            foreach (var item in carrito.Items)
            {
                // Buscar el almacén más cercano con stock disponible
                var almacenConStock = _context.Inventarios
                    .Include(i => i.Almacen)
                    .Where(i => i.ID_PRODUCTO == item.ID_PRODUCTO && i.CANTIDAD >= item.CANTIDAD)
                    .OrderBy(i => i.Almacen.ID_PROVINCIA == idProvincia ? 0 : 1) // Priorizar almacenes en la misma provincia
                    .ThenByDescending(i => i.CANTIDAD) // Luego por mayor disponibilidad
                    .FirstOrDefault();

                if (almacenConStock == null)
                {
                    // Si no hay stock suficiente, cancelar el pedido
                    _context.Pedidos.Remove(pedido);
                    _context.SaveChanges();
                    TempData["Error"] = $"No hay suficiente stock para el producto {item.Producto.NOMBRE}";
                    return RedirectToAction("MiCarrito");
                }

                // Agregar detalle del pedido
                var detalle = new DetallePedido
                {
                    ID_PEDIDO = pedido.ID_PEDIDO,
                    ID_PRODUCTO = item.ID_PRODUCTO,
                    CANTIDAD = item.CANTIDAD,
                    PRECIO_UNITARIO = item.PRECIO_UNITARIO,
                    ID_ALMACEN_ORIGEN = almacenConStock.ID_ALMACEN
                };

                _context.DetallesPedido.Add(detalle);

                // Reducir el inventario
                almacenConStock.CANTIDAD -= item.CANTIDAD;
                almacenConStock.FECHA_ACTUALIZACION = DateTime.Now;
                _context.Update(almacenConStock);

                // Registrar el movimiento de inventario
                var movimiento = new MovimientoInventario
                {
                    ID_PRODUCTO = item.ID_PRODUCTO,
                    ID_ALMACEN = almacenConStock.ID_ALMACEN,
                    TIPO_MOVIMIENTO = "SALIDA",
                    CANTIDAD = item.CANTIDAD,
                    FECHA_MOVIMIENTO = DateTime.Now,
                    ID_USUARIO = usuarioId.Value,
                    OBSERVACION = $"Pedido #{pedido.ID_PEDIDO}"
                };

                _context.MovimientosInventario.Add(movimiento);
            }

            // Desactivar el carrito actual y crear uno nuevo
            carrito.ACTIVO = false;
            _context.Update(carrito);

            var nuevoCarrito = new Carrito
            {
                ID_USUARIO = usuarioId.Value,
                FECHA_CREACION = DateTime.Now,
                ACTIVO = true
            };
            _context.Carritos.Add(nuevoCarrito);

            _context.SaveChanges();

            TempData["Message"] = $"Su pedido #{pedido.ID_PEDIDO} ha sido creado exitosamente";
            return RedirectToAction("MisPedidos","Pedido");
        }

        [Authorize]
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

        [Authorize]
        public IActionResult DetallePedido(int id)
        {
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

        public IActionResult Contacto()
        {
            return View();
        }

        private async Task EnviarPorCorreo(string email, string titulo, string mensaje)
        {
            try
            {
                using (var client = new System.Net.Mail.SmtpClient(_configuration["Email:SmtpServer"]))
                {
                    client.Port = int.Parse(_configuration["Email:Port"]);
                    client.Credentials = new System.Net.NetworkCredential(
                        _configuration["Email:Username"],
                        _configuration["Email:Password"]);
                    client.EnableSsl = true;

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_configuration["Email:FromAddress"]),
                        Subject = titulo,
                        Body = mensaje,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Puedes registrar el error en algún log o simplemente imprimirlo en la consola
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ProcesarContacto()
        {
            try
            {
                // Obtener datos del formulario directamente
                string nombre = Request.Form["nombre"];
                string telefono = Request.Form["telefono"];
                string email = Request.Form["email"];
                string asunto = Request.Form["asunto"];
                string mensaje = Request.Form["mensaje"];
                bool aceptoTerminos = Request.Form["acepto-terminos"] == "on";

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(mensaje))
                {
                    return BadRequest("Nombre, email y mensaje son obligatorios");
                }

                if (!aceptoTerminos)
                {
                    return BadRequest("Debe aceptar los términos y condiciones");
                }

                // Crear el mensaje para el correo
                string tituloCorreo = $"Contacto web: {asunto}";
                string mensajeCorreo = $@"
            <h3>Nuevo mensaje desde el formulario de contacto</h3>
            <p><strong>Nombre:</strong> {nombre}</p>
            <p><strong>Email:</strong> {email}</p>
            <p><strong>Teléfono:</strong> {(!string.IsNullOrWhiteSpace(telefono) ? telefono : "No proporcionado")}</p>
            <p><strong>Asunto:</strong> {asunto}</p>
            <p><strong>Mensaje:</strong></p>
            <div style='background: #f5f5f5; padding: 15px; border-left: 4px solid #005da4;'>
                {mensaje.Replace("\n", "<br>")}
            </div>
            <p><strong>Fecha:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
        ";

                // Enviar correo (usando tu método existente)
                await EnviarPorCorreo("jabs9606@gmail.com", tituloCorreo, mensajeCorreo);

                // Opcional: Enviar confirmación al cliente
                string confirmacion = $@"
            <h3>¡Gracias por contactarnos, {nombre}!</h3>
            <p>Hemos recibido tu mensaje y nos pondremos en contacto contigo pronto.</p>
            <p>Tu consulta: <em>{asunto}</em></p>
        ";
                await EnviarPorCorreo(email, "Gracias por contactarnos - IngeTech", confirmacion);

                return Ok(new { success = true, message = "Mensaje enviado correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar contacto: {ex.Message}");
                return StatusCode(500, "Error al enviar el mensaje");
            }
        }

        public IActionResult SobreNosotros()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}