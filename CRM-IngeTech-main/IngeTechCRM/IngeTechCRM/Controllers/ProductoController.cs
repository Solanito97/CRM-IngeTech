using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using IngeTechCRM.Models;

namespace IngeTechCRM.Controllers
{
    public class ProductoController : Controller
    {
        private readonly IngeTechDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductoController(IngeTechDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        [Authorize]
        public IActionResult Index(string buscar = null)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Consulta de productos incluyendo las relaciones necesarias
            var productosQuery = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .Include(p => p.UsuarioCreador)
                .Include(p => p.Inventarios) // Incluir la relación de Inventarios
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                productosQuery = productosQuery.Where(p =>
                    p.NOMBRE.Contains(buscar) ||
                    p.CODIGO.Contains(buscar) ||
                    p.DESCRIPCION.Contains(buscar));
            }

            // Obtener la lista de productos
            var productos = productosQuery.OrderByDescending(p => p.FECHA_CREACION).ToList();
            ViewBag.Buscar = buscar;

            return View(productos);
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

            var producto = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .Include(p => p.UsuarioCreador)
                .Include(p => p.Imagenes)
                .FirstOrDefault(p => p.ID_PRODUCTO == id);

            if (producto == null)
            {
                return NotFound();
            }

            // Obtener información de inventario
            var inventarioPorAlmacen = _context.Inventarios
                .Include(i => i.Almacen)
                .ThenInclude(a => a.Provincia)
                .Where(i => i.ID_PRODUCTO == id)
                .ToList();

            ViewBag.InventarioPorAlmacen = inventarioPorAlmacen;

            return View(producto);
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

            // Asegúrate de que estas listas nunca sean nulas
            var categorias = _context.Categorias.ToList() ?? new List<Categoria>();
            var marcas = _context.Marcas.ToList() ?? new List<Marca>();
            var proveedores = _context.Proveedores.ToList() ?? new List<Proveedor>();

            ViewBag.Categorias = new SelectList(categorias, "ID_CATEGORIA", "NOMBRE");
            ViewBag.Marcas = new SelectList(marcas, "ID_MARCA", "NOMBRE");
            ViewBag.Proveedores = new SelectList(proveedores, "ID_PROVEEDOR", "NOMBRE");

            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Crear(Producto producto, List<IFormFile> imagenes)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // VALIDACIÓN 1: Verificar que el código del producto sea único
            if (!string.IsNullOrEmpty(producto.CODIGO))
            {
                var codigoExiste = await _context.Productos
                    .AnyAsync(p => p.CODIGO == producto.CODIGO);

                if (codigoExiste)
                {
                    ModelState.AddModelError("CODIGO", "El código del producto ya existe. Por favor, ingrese un código único.");
                }
            }

            // VALIDACIÓN 2: Si el producto está activo, debe tener al menos una imagen
            if (producto.ACTIVO && (imagenes == null || !imagenes.Any(i => i.Length > 0)))
            {
                ModelState.AddModelError("", "Los productos activos deben tener al menos una imagen representativa.");
            }

            ModelState.Remove("Marca");
            ModelState.Remove("Categoria");
            ModelState.Remove("Proveedor");
            ModelState.Remove("Inventarios");
            ModelState.Remove("ItemsCarrito");
            ModelState.Remove("DetallesPedido");
            ModelState.Remove("UsuarioCreador");
            ModelState.Remove("MovimientosInventario");

            if (ModelState.IsValid)
            {
                var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
                producto.ID_USUARIO_CREADOR = usuarioId.Value;
                producto.FECHA_CREACION = DateTime.Now;

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                // Guardar imágenes
                if (imagenes != null && imagenes.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "productos");

                    // Crear directorio si no existe
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var imagen in imagenes)
                    {
                        if (imagen.Length > 0)
                        {
                            // Crear nombre de archivo único
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);
                            string filePath = Path.Combine(uploadsFolder, fileName);

                            // Guardar archivo
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await imagen.CopyToAsync(fileStream);
                            }

                            // Guardar referencia en la base de datos
                            var imagenProducto = new ImagenProducto
                            {
                                ID_PRODUCTO = producto.ID_PRODUCTO,
                                RUTA_IMAGEN = "/images/productos/" + fileName
                            };

                            _context.ImagenesProducto.Add(imagenProducto);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Message"] = "Producto creado exitosamente";
                return RedirectToAction(nameof(Index));
            }

            var categorias = _context.Categorias.ToList() ?? new List<Categoria>();
            var marcas = _context.Marcas.ToList() ?? new List<Marca>();
            var proveedores = _context.Proveedores.ToList() ?? new List<Proveedor>();

            ViewBag.Categorias = new SelectList(categorias, "ID_CATEGORIA", "NOMBRE", producto.ID_CATEGORIA);
            ViewBag.Marcas = new SelectList(marcas, "ID_MARCA", "NOMBRE", producto.ID_MARCA);
            ViewBag.Proveedores = new SelectList(proveedores, "ID_PROVEEDOR", "NOMBRE", producto.ID_PROVEEDOR);

            return View(producto);
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

            var producto = _context.Productos
                .Include(p => p.Imagenes)
                .FirstOrDefault(p => p.ID_PRODUCTO == id);

            if (producto == null)
            {
                return NotFound();
            }

            // Corregir los nombres de las propiedades en los SelectList
            ViewBag.Categorias = new SelectList(_context.Categorias, "ID_CATEGORIA", "NOMBRE", producto.ID_CATEGORIA);
            ViewBag.Marcas = new SelectList(_context.Marcas, "ID_MARCA", "NOMBRE", producto.ID_MARCA);
            ViewBag.Proveedores = new SelectList(_context.Proveedores, "ID_PROVEEDOR", "NOMBRE", producto.ID_PROVEEDOR);

            return View(producto);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, Producto producto, List<IFormFile> nuevasImagenes, List<int> imagenesAEliminar)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != producto.ID_PRODUCTO)
            {
                return NotFound();
            }

            // VALIDACIÓN 1: Verificar que el código del producto sea único (excluyendo el producto actual)
            if (!string.IsNullOrEmpty(producto.CODIGO))
            {
                var codigoExiste = await _context.Productos
                    .AnyAsync(p => p.CODIGO == producto.CODIGO && p.ID_PRODUCTO != id);

                if (codigoExiste)
                {
                    ModelState.AddModelError("CODIGO", "El código del producto ya existe. Por favor, ingrese un código único.");
                }
            }

            // VALIDACIÓN 2: Si el producto está activo, verificar que tenga imágenes
            if (producto.ACTIVO)
            {
                // Contar imágenes existentes que no se van a eliminar
                var imagenesExistentes = await _context.ImagenesProducto
                    .Where(i => i.ID_PRODUCTO == id)
                    .CountAsync();

                // Si hay imágenes a eliminar, restarlas del conteo
                if (imagenesAEliminar != null && imagenesAEliminar.Count > 0)
                {
                    imagenesExistentes -= imagenesAEliminar.Count;
                }

                // Contar nuevas imágenes válidas
                var nuevasImagenesValidas = 0;
                if (nuevasImagenes != null)
                {
                    nuevasImagenesValidas = nuevasImagenes.Count(i => i.Length > 0);
                }

                // Total de imágenes después de la operación
                var totalImagenes = imagenesExistentes + nuevasImagenesValidas;

                if (totalImagenes == 0)
                {
                    ModelState.AddModelError("", "Los productos activos deben tener al menos una imagen representativa.");
                }
            }

            // Excluir propiedades de navegación del ModelState
            ModelState.Remove("Marca");
            ModelState.Remove("Categoria");
            ModelState.Remove("Proveedor");
            ModelState.Remove("Inventarios");
            ModelState.Remove("ItemsCarrito");
            ModelState.Remove("DetallesPedido");
            ModelState.Remove("UsuarioCreador");
            ModelState.Remove("MovimientosInventario");
            ModelState.Remove("Imagenes");

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener producto original
                    var productoOriginal = await _context.Productos.FindAsync(id);
                    if (productoOriginal == null)
                    {
                        return NotFound();
                    }

                    // Actualizar solo los campos permitidos
                    productoOriginal.CODIGO = producto.CODIGO;
                    productoOriginal.NOMBRE = producto.NOMBRE;
                    productoOriginal.DESCRIPCION = producto.DESCRIPCION;
                    productoOriginal.PRECIO = producto.PRECIO;
                    productoOriginal.ID_CATEGORIA = producto.ID_CATEGORIA;
                    productoOriginal.ID_MARCA = producto.ID_MARCA;
                    productoOriginal.ID_PROVEEDOR = producto.ID_PROVEEDOR;
                    productoOriginal.ACTIVO = producto.ACTIVO;
                    productoOriginal.FECHA_ACTUALIZACION = DateTime.Now;

                    _context.Update(productoOriginal);
                    await _context.SaveChangesAsync();

                    // Eliminar imágenes seleccionadas
                    if (imagenesAEliminar != null && imagenesAEliminar.Count > 0)
                    {
                        foreach (var idImagen in imagenesAEliminar)
                        {
                            var imagen = await _context.ImagenesProducto.FindAsync(idImagen);
                            if (imagen != null)
                            {
                                // Eliminar archivo físico
                                string webRootPath = _hostEnvironment.WebRootPath;
                                string filePath = webRootPath + imagen.RUTA_IMAGEN;
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }

                                // Eliminar registro de la base de datos
                                _context.ImagenesProducto.Remove(imagen);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Guardar nuevas imágenes
                    if (nuevasImagenes != null && nuevasImagenes.Count > 0)
                    {
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "productos");

                        // Crear directorio si no existe
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        foreach (var imagen in nuevasImagenes)
                        {
                            if (imagen.Length > 0)
                            {
                                // Crear nombre de archivo único
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);
                                string filePath = Path.Combine(uploadsFolder, fileName);

                                // Guardar archivo
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await imagen.CopyToAsync(fileStream);
                                }

                                // Guardar referencia en la base de datos
                                var imagenProducto = new ImagenProducto
                                {
                                    ID_PRODUCTO = productoOriginal.ID_PRODUCTO,
                                    RUTA_IMAGEN = "/images/productos/" + fileName
                                };

                                _context.ImagenesProducto.Add(imagenProducto);
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.ID_PRODUCTO))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                TempData["Message"] = "Producto actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias = new SelectList(_context.Categorias, "ID_CATEGORIA", "NOMBRE", producto.ID_CATEGORIA);
            ViewBag.Marcas = new SelectList(_context.Marcas, "ID_MARCA", "NOMBRE", producto.ID_MARCA);
            ViewBag.Proveedores = new SelectList(_context.Proveedores, "ID_PROVEEDOR", "NOMBRE", producto.ID_PROVEEDOR);

            return View(producto);
        }

        [Authorize]
        public IActionResult Eliminar(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var producto = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Proveedor)
                .FirstOrDefault(p => p.ID_PRODUCTO == id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        [Authorize]
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEliminar(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var producto = await _context.Productos.FindAsync(id);

            // En lugar de eliminar físicamente, desactivar el producto
            producto.ACTIVO = false;
            producto.FECHA_ACTUALIZACION = DateTime.Now;

            _context.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Producto desactivado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.ID_PRODUCTO == id);
        }

        // ==== Administración de Categorías ====
        [Authorize]
        public IActionResult Categorias()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Cargar categorías e incluir productos relacionados
            var categorias = _context.Categorias
                .Include(c => c.Productos) // Incluir productos relacionados
                .ToList();

            return View(categorias);
        }

        [Authorize]
        public IActionResult CrearCategoria()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCategoria(Categoria categoria)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                _context.Add(categoria);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Categoría creada exitosamente";
                return RedirectToAction(nameof(Categorias));
            }
            return View(categoria);
        }

        [Authorize]
        public async Task<IActionResult> EditarCategoria(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            return View(categoria);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCategoria(int id, Categoria categoria)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != categoria.ID_CATEGORIA)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoria);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoriaExists(categoria.ID_CATEGORIA))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["Message"] = "Categoría actualizada exitosamente";
                return RedirectToAction(nameof(Categorias));
            }
            return View(categoria);
        }

        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.ID_CATEGORIA == id);
        }

        // ==== Administración de Marcas ====
        [Authorize]
        public IActionResult Marcas()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Cargar marcas e incluir productos relacionados
            var marcas = _context.Marcas
                .Include(m => m.Productos) // Incluir productos relacionados
                .ToList();

            return View(marcas);
        }

        [Authorize]
        public IActionResult CrearMarca()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearMarca(Marca marca)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                _context.Add(marca);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Marca creada exitosamente";
                return RedirectToAction(nameof(Marcas));
            }
            return View(marca);
        }

        [Authorize]
        public async Task<IActionResult> EditarMarca(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var marca = await _context.Marcas.FindAsync(id);
            if (marca == null)
            {
                return NotFound();
            }
            return View(marca);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMarca(int id, Marca marca)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != marca.ID_MARCA)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(marca);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MarcaExists(marca.ID_MARCA))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["Message"] = "Marca actualizada exitosamente";
                return RedirectToAction(nameof(Marcas));
            }
            return View(marca);
        }

        private bool MarcaExists(int id)
        {
            return _context.Marcas.Any(e => e.ID_MARCA == id);
        }

        // ==== Administración de Proveedores ====
        [Authorize]
        public IActionResult Proveedores()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Cargar proveedores e incluir productos relacionados
            var proveedores = _context.Proveedores
                .Include(p => p.Productos) // Incluir productos relacionados
                .ToList();

            return View(proveedores);
        }

        [Authorize]
        public IActionResult CrearProveedor()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearProveedor(Proveedor proveedor)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                _context.Add(proveedor);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Proveedor creado exitosamente";
                return RedirectToAction(nameof(Proveedores));
            }
            return View(proveedor);
        }

        [Authorize]
        public async Task<IActionResult> EditarProveedor(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }
            return View(proveedor);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarProveedor(int id, Proveedor proveedor)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != proveedor.ID_PROVEEDOR)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExists(proveedor.ID_PROVEEDOR))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["Message"] = "Proveedor actualizado exitosamente";
                return RedirectToAction(nameof(Proveedores));
            }
            return View(proveedor);
        }

        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.ID_PROVEEDOR == id);
        }
    }
}