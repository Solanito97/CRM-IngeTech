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
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace IngeTechCRM.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly IngeTechDbContext _context;

        public UsuarioController(IngeTechDbContext context)
        {
            _context = context;
        }

        // Acción para mostrar la lista de usuarios con filtros
        // Acción para mostrar la lista de usuarios con filtros
        public async Task<IActionResult> Index(string buscar, int? provinciaId, int? tipoUsuarioId)
        {
            // Verificar si el usuario es administrador usando un método centralizado
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("Index", "Home");
            }

            var usuariosQuery = _context.Usuarios
                .Include(u => u.Provincia)
                .Include(u => u.TipoUsuario)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(buscar))
            {
                buscar = buscar.Trim();
                usuariosQuery = usuariosQuery.Where(u =>
                    u.NOMBRE_COMPLETO.Contains(buscar) ||
                    u.NOMBRE_USUARIO.Contains(buscar) ||
                    u.CORREO_ELECTRONICO.Contains(buscar) ||
                    u.IDENTIFICACION.ToString().Contains(buscar));
            }

            if (provinciaId.HasValue)
            {
                usuariosQuery = usuariosQuery.Where(u => u.ID_PROVINCIA == provinciaId.Value);
            }

            if (tipoUsuarioId.HasValue)
            {
                usuariosQuery = usuariosQuery.Where(u => u.ID_TIPO_USUARIO == tipoUsuarioId.Value);
            }

            var usuarios = await usuariosQuery
                .OrderBy(u => u.NOMBRE_COMPLETO)
                .ToListAsync();

            // Generar el HTML para los select manualmente en vez de usar SelectList
            var provincias = await _context.Provincias.ToListAsync();
            var tiposUsuario = await _context.TiposUsuario.ToListAsync();

            var provinciasHtml = new System.Text.StringBuilder();
            foreach (var provincia in provincias)
            {
                var selected = provinciaId.HasValue && provinciaId.Value == provincia.ID_PROVINCIA ? "selected" : "";
                provinciasHtml.AppendLine($"<option value=\"{provincia.ID_PROVINCIA}\" {selected}>{provincia.NOMBRE}</option>");
            }

            var tiposUsuarioHtml = new System.Text.StringBuilder();
            foreach (var tipo in tiposUsuario)
            {
                var selected = tipoUsuarioId.HasValue && tipoUsuarioId.Value == tipo.ID_TIPO_USUARIO ? "selected" : "";
                tiposUsuarioHtml.AppendLine($"<option value=\"{tipo.ID_TIPO_USUARIO}\" {selected}>{tipo.DESCRIPCION}</option>");
            }

            ViewBag.Provincias = provinciasHtml.ToString();
            ViewBag.TiposUsuario = tiposUsuarioHtml.ToString();
            ViewBag.Buscar = buscar;

            return View(usuarios);
        }

        // Acción para mostrar los detalles de un usuario
        public async Task<IActionResult> Detalles(int id)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Provincia)
                .Include(u => u.TipoUsuario)
                .FirstOrDefaultAsync(u => u.IDENTIFICACION == id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Obtener pedidos del usuario (utilizar consulta más eficiente)
            var pedidos = await _context.Pedidos
                .Where(p => p.ID_USUARIO == id)
                .OrderByDescending(p => p.FECHA_PEDIDO)
                .Take(5)
                .ToListAsync();

            // Obtener comunicados enviados al usuario (utilizar consulta más eficiente)
            var comunicados = await _context.EnviosComunicado
                .Include(e => e.Comunicado)
                .Where(e => e.ID_USUARIO_DESTINATARIO == id)
                .OrderByDescending(e => e.FECHA_ENVIO)
                .Take(5)
                .Select(e => e.Comunicado)
                .ToListAsync();

            ViewBag.Pedidos = pedidos;
            ViewBag.Comunicados = comunicados;

            return View(usuario);
        }

        // Acción para mostrar el formulario de creación de usuario
        public async Task<IActionResult> Crear()
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            // Generar el HTML para las opciones de los select manualmente
            var provincias = await _context.Provincias.ToListAsync();
            var tiposUsuario = await _context.TiposUsuario.ToListAsync();

            var provinciasHtml = new System.Text.StringBuilder();
            foreach (var provincia in provincias)
            {
                provinciasHtml.AppendLine($"<option value=\"{provincia.ID_PROVINCIA}\">{provincia.NOMBRE}</option>");
            }

            var tiposUsuarioHtml = new System.Text.StringBuilder();
            foreach (var tipo in tiposUsuario)
            {
                tiposUsuarioHtml.AppendLine($"<option value=\"{tipo.ID_TIPO_USUARIO}\">{tipo.DESCRIPCION}</option>");
            }

            ViewBag.Provincias = provinciasHtml.ToString();
            ViewBag.TiposUsuario = tiposUsuarioHtml.ToString();

            return View();
        }

        // Acción para procesar la creación de un usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Usuario usuario, string confirmarContrasena)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }
            ModelState.Remove("Pedidos");
            ModelState.Remove("Carritos");
            ModelState.Remove("Provincia");
            ModelState.Remove("TipoUsuario");
            ModelState.Remove("ProductosCreados");
            ModelState.Remove("ComunicadosCreados");
            ModelState.Remove("ComunicadosRecibidos");
            ModelState.Remove("MovimientosInventario");

            if (ModelState.IsValid)
            {
                if (usuario.CONTRASENA != confirmarContrasena)
                {
                    ModelState.AddModelError("ConfirmarContrasena", "Las contraseñas no coinciden");

                    // Regenerar HTML para los select
                    await RegenerarSelectsHtml(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
                    return View(usuario);
                }

                // Verificar si la identificación ya existe
                var existeIdentificacion = await _context.Usuarios.AnyAsync(u => u.IDENTIFICACION == usuario.IDENTIFICACION);
                if (existeIdentificacion)
                {
                    ModelState.AddModelError("IDENTIFICACION", "Esta identificación ya está registrada");
                    await RegenerarSelectsHtml(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
                    return View(usuario);
                }

                // Verificar si el correo electrónico ya existe
                var existeCorreo = await _context.Usuarios.AnyAsync(u => u.CORREO_ELECTRONICO == usuario.CORREO_ELECTRONICO);
                if (existeCorreo)
                {
                    ModelState.AddModelError("CORREO_ELECTRONICO", "Este correo electrónico ya está registrado");
                    await RegenerarSelectsHtml(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
                    return View(usuario);
                }

                usuario.FECHA_REGISTRO = DateTime.Now;
                usuario.ULTIMO_ACCESO = DateTime.Now;

                // Aplicar hash a la contraseña
                usuario.CONTRASENA = HashPassword(usuario.CONTRASENA);

                _context.Add(usuario);
                await _context.SaveChangesAsync();

                // Si el usuario es cliente, crear un carrito
                if (usuario.ID_TIPO_USUARIO == 2) // Asumiendo que 2 es el ID del tipo Cliente
                {
                    var carrito = new Carrito
                    {
                        ID_USUARIO = usuario.IDENTIFICACION,
                        FECHA_CREACION = DateTime.Now,
                        ACTIVO = true
                    };

                    _context.Carritos.Add(carrito);
                    await _context.SaveChangesAsync();
                }

                TempData["Message"] = "Usuario creado exitosamente";
                return RedirectToAction(nameof(Index));
            }

            await RegenerarSelectsHtml(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
            return View(usuario);
        }

        // Método auxiliar para regenerar el HTML de los select
        private async Task RegenerarSelectsHtml(int? provinciaId = null, int? tipoUsuarioId = null)
        {
            var provincias = await _context.Provincias.ToListAsync();
            var tiposUsuario = await _context.TiposUsuario.ToListAsync();

            var provinciasHtml = new System.Text.StringBuilder();
            foreach (var provincia in provincias)
            {
                var selected = provinciaId.HasValue && provinciaId.Value == provincia.ID_PROVINCIA ? "selected" : "";
                provinciasHtml.AppendLine($"<option value=\"{provincia.ID_PROVINCIA}\" {selected}>{provincia.NOMBRE}</option>");
            }

            var tiposUsuarioHtml = new System.Text.StringBuilder();
            foreach (var tipo in tiposUsuario)
            {
                var selected = tipoUsuarioId.HasValue && tipoUsuarioId.Value == tipo.ID_TIPO_USUARIO ? "selected" : "";
                tiposUsuarioHtml.AppendLine($"<option value=\"{tipo.ID_TIPO_USUARIO}\" {selected}>{tipo.DESCRIPCION}</option>");
            }

            ViewBag.Provincias = provinciasHtml.ToString();
            ViewBag.TiposUsuario = tiposUsuarioHtml.ToString();
        }

        // Acción para mostrar el formulario de edición de usuario
        public async Task<IActionResult> Editar(int id)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            // Generar el HTML para las opciones de los select manualmente
            var provincias = await _context.Provincias.ToListAsync();
            var tiposUsuario = await _context.TiposUsuario.ToListAsync();

            var provinciasHtml = new System.Text.StringBuilder();
            foreach (var provincia in provincias)
            {
                var selected = usuario.ID_PROVINCIA == provincia.ID_PROVINCIA ? "selected" : "";
                provinciasHtml.AppendLine($"<option value=\"{provincia.ID_PROVINCIA}\" {selected}>{provincia.NOMBRE}</option>");
            }

            var tiposUsuarioHtml = new System.Text.StringBuilder();
            foreach (var tipo in tiposUsuario)
            {
                var selected = usuario.ID_TIPO_USUARIO == tipo.ID_TIPO_USUARIO ? "selected" : "";
                tiposUsuarioHtml.AppendLine($"<option value=\"{tipo.ID_TIPO_USUARIO}\" {selected}>{tipo.DESCRIPCION}</option>");
            }

            ViewBag.Provincias = provinciasHtml.ToString();
            ViewBag.TiposUsuario = tiposUsuarioHtml.ToString();

            return View(usuario);
        }

        // Acción para procesar la edición de un usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Usuario usuario, string nuevaContrasena)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            if (id != usuario.IDENTIFICACION)
            {
                return NotFound();
            }
            ModelState.Remove("nuevaContrasena");
            ModelState.Remove("CONTRASENA");
            ModelState.Remove("Pedidos");
            ModelState.Remove("Carritos");
            ModelState.Remove("Provincia");
            ModelState.Remove("TipoUsuario");
            ModelState.Remove("ProductosCreados");
            ModelState.Remove("ComunicadosCreados");
            ModelState.Remove("ComunicadosRecibidos");
            ModelState.Remove("MovimientosInventario");
            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener el usuario original para comparar cambios
                    var usuarioOriginal = await _context.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.IDENTIFICACION == id);

                    if (usuarioOriginal == null)
                    {
                        return NotFound();
                    }

                    // Verificar si el correo electrónico ya existe y no es del mismo usuario
                    if (usuario.CORREO_ELECTRONICO != usuarioOriginal.CORREO_ELECTRONICO)
                    {
                        var existeCorreo = await _context.Usuarios
                            .AnyAsync(u => u.CORREO_ELECTRONICO == usuario.CORREO_ELECTRONICO && u.IDENTIFICACION != id);

                        if (existeCorreo)
                        {
                            ModelState.AddModelError("CORREO_ELECTRONICO", "Este correo electrónico ya está registrado");
                            await CargarListasDesplegables(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
                            return View(usuario);
                        }
                    }

                    // Si se proporciona una nueva contraseña, actualizarla con hash
                    if (!string.IsNullOrEmpty(nuevaContrasena))
                    {
                        usuario.CONTRASENA = HashPassword(nuevaContrasena);
                    }
                    else
                    {
                        // Mantener la contraseña original
                        usuario.CONTRASENA = usuarioOriginal.CONTRASENA;
                    }

                    // Mantener fechas originales
                    usuario.FECHA_REGISTRO = usuarioOriginal.FECHA_REGISTRO;
                    usuario.ULTIMO_ACCESO = usuarioOriginal.ULTIMO_ACCESO;

                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.IDENTIFICACION))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["Message"] = "Usuario actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            await CargarListasDesplegables(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
            return View(usuario);
        }

        // Acción para mostrar la confirmación de eliminación
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Provincia)
                .Include(u => u.TipoUsuario)
                .FirstOrDefaultAsync(m => m.IDENTIFICACION == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // Acción para procesar la eliminación
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEliminar(int id)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            var usuarioActualId = HttpContext.Session.GetInt32("UsuarioId");
            if (id == usuarioActualId)
            {
                TempData["Error"] = "No puedes eliminar tu propio usuario";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Verificar si hay pedidos relacionados
                var tienePedidos = await _context.Pedidos.AnyAsync(p => p.ID_USUARIO == id);
                if (tienePedidos)
                {
                    TempData["Error"] = "No se puede eliminar el usuario porque tiene pedidos asociados";
                    return RedirectToAction(nameof(Index));
                }

                // Eliminar carrito si existe
                var carrito = await _context.Carritos
                    .FirstOrDefaultAsync(c => c.ID_USUARIO == id && c.ACTIVO);
                if (carrito != null)
                {
                    _context.Carritos.Remove(carrito);
                }

                // Eliminar envíos de comunicados
                var enviosComunicado = await _context.EnviosComunicado
                    .Where(e => e.ID_USUARIO_DESTINATARIO == id)
                    .ToListAsync();
                _context.EnviosComunicado.RemoveRange(enviosComunicado);

                // Eliminar usuario
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario != null)
                {
                    _context.Usuarios.Remove(usuario);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Usuario eliminado exitosamente";
                }
            }
            catch (Exception ex)
            {
                // Registrar la excepción y mostrar mensaje de error
                // logger.LogError(ex, "Error al eliminar usuario {Id}", id);
                TempData["Error"] = "Ocurrió un error al eliminar el usuario: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #region Métodos auxiliares

        // Método para verificar si el usuario actual es administrador
        private bool EsUsuarioAdministrador()
        {
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            return tipoUsuarioId == 1; // Asumiendo que 1 es el ID para administradores
        }

        // Método para cargar las listas desplegables
        private async Task CargarListasDesplegables(int? provinciaId = null, int? tipoUsuarioId = null)
        {
            ViewBag.Provincias = new SelectList(await _context.Provincias.ToListAsync(), "IdProvincia", "Nombre", provinciaId);
            ViewBag.TiposUsuario = new SelectList(await _context.TiposUsuario.ToListAsync(), "IdTipoUsuario", "Descripcion", tipoUsuarioId);
        }

        // Método para verificar si un usuario existe
        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IDENTIFICACION == id);
        }

        // Método para aplicar hash a las contraseñas
        private string HashPassword(string password)
        {
            // Generar salt aleatorio
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derivar una subkey de 256 bits (usar HMACSHA256 con 10,000 iteraciones)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Formato: {algorithm}${iterations}${base64salt}${base64hash}
            return $"PBKDF2${10000}${Convert.ToBase64String(salt)}${hashed}";
        }

        // Método para verificar una contraseña hasheada
        private bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            // Separar los componentes del hash almacenado
            var parts = hashedPassword.Split('$');
            if (parts.Length != 4)
            {
                return false; // Formato inválido
            }

            var algorithm = parts[0];
            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var hash = parts[3];

            // Calcular hash de la contraseña proporcionada
            string newHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: 256 / 8));

            // Comparar los hashes
            return newHash == hash;
        }

        #endregion
    }
}