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

            // Generar el HTML para los select manualmente
            await CargarListasDesplegablesHTML(provinciaId, tipoUsuarioId);
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

            await CargarListasDesplegablesHTML();
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

            // Remover validaciones de propiedades de navegación
            ModelState.Remove("Pedidos");
            ModelState.Remove("Carritos");
            ModelState.Remove("Provincia");
            ModelState.Remove("TipoUsuario");
            ModelState.Remove("ProductosCreados");
            ModelState.Remove("ComunicadosCreados");
            ModelState.Remove("ComunicadosRecibidos");
            ModelState.Remove("MovimientosInventario");

            if (!string.IsNullOrEmpty(usuario.NOMBRE_COMPLETO))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(usuario.NOMBRE_COMPLETO, @"^[a-zA-ZÀ-ÿ\u00f1\u00d1\s]+$"))
                {
                    ModelState.AddModelError("NOMBRE_COMPLETO",
                        "El nombre completo solo puede contener letras, espacios y tildes. No se permiten números.");
                }
            }
            if (!string.IsNullOrEmpty(usuario.TELEFONO) && !usuario.TELEFONO.EsTelefonoCostaRicaValido())
            {
                ModelState.AddModelError("TELEFONO",
                    "Ingrese un número de teléfono válido de Costa Rica (8 dígitos, debe empezar con 2, 6, 7 u 8)");
            }

            // Permitir campos opcionales como nulos o vacíos
            if (string.IsNullOrWhiteSpace(usuario.TELEFONO))
            {
                ModelState.Remove("TELEFONO");
                usuario.TELEFONO = null;
            }

            if (string.IsNullOrWhiteSpace(usuario.DIRECCION_COMPLETA))
            {
                ModelState.Remove("DIRECCION_COMPLETA");
                usuario.DIRECCION_COMPLETA = null;
            }

            if (ModelState.IsValid)
            {
                if (usuario.CONTRASENA != confirmarContrasena)
                {
                    ModelState.AddModelError("ConfirmarContrasena", "Las contraseñas no coinciden");
                    await CargarListasDesplegablesHTML(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
                    return View(usuario);
                }

                // Verificar si la identificación ya existe
                var existeIdentificacion = await _context.Usuarios.AnyAsync(u => u.IDENTIFICACION == usuario.IDENTIFICACION);
                if (existeIdentificacion)
                {
                    ModelState.AddModelError("IDENTIFICACION", "Esta identificación ya está registrada");
                    await CargarListasDesplegablesHTML(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
                    return View(usuario);
                }

                // Verificar si el correo electrónico ya existe
                var existeCorreo = await _context.Usuarios.AnyAsync(u => u.CORREO_ELECTRONICO == usuario.CORREO_ELECTRONICO);
                if (existeCorreo)
                {
                    ModelState.AddModelError("CORREO_ELECTRONICO", "Este correo electrónico ya está registrado");
                    await CargarListasDesplegablesHTML(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
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

            await CargarListasDesplegablesHTML(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
            return View(usuario);
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

            await CargarListasDesplegablesHTML(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
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

            // SOLO remover validaciones de campos que no son parte del formulario
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

            // NO remover validaciones de TELEFONO y DIRECCION_COMPLETA
            // Dejar que ModelState.IsValid haga su trabajo normal

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
                            await CargarListasDesplegablesHTML(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
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

                    TempData["Message"] = "Usuario actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
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
            }

            // Si hay errores de validación, recargar las listas y mostrar la vista con errores
            await CargarListasDesplegablesHTML(usuario.ID_PROVINCIA, usuario.ID_TIPO_USUARIO);
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
                TempData["Error"] = "Ocurrió un error al eliminar el usuario";
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

        // MÉTODO CORREGIDO: Cargar las listas desplegables como HTML
        private async Task CargarListasDesplegablesHTML(int? provinciaId = null, int? tipoUsuarioId = null)
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

    public static class ValidacionTelefono
    {
        public static bool EsTelefonoCostaRicaValido(this string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return false;

            // Remover espacios y guiones si los hay
            string telefonoLimpio = telefono.Replace(" ", "").Replace("-", "");

            // Verificar que solo contenga números
            if (!System.Text.RegularExpressions.Regex.IsMatch(telefonoLimpio, @"^[0-9]+$"))
                return false;

            // Verificar longitud (8 dígitos para Costa Rica)
            if (telefonoLimpio.Length != 8)
                return false;

            // Verificar que empiece con números válidos para Costa Rica
            // Móviles: 6, 7, 8
            // Fijos: 2
            string primerDigito = telefonoLimpio.Substring(0, 1);
            return primerDigito == "2" || primerDigito == "6" || primerDigito == "7" || primerDigito == "8";
        }
    }
}