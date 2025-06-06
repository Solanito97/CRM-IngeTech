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

        private readonly IConfiguration _configuration;

        
        public UsuarioController(IngeTechDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
            // Generar una sal aleatoria
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Número de iteraciones (más iteraciones = más seguro pero más lento)
            int iterations = 10000;

            // Derivar la clave usando PBKDF2
            using (var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256))
            {
                byte[] hash = deriveBytes.GetBytes(32); // 256 bits
                string saltString = Convert.ToBase64String(salt);
                string hashString = Convert.ToBase64String(hash);

                // Formato: PBKDF2$iteraciones$salt==$hash (MISMO formato que AccountController)
                return $"PBKDF2${iterations}${saltString}==${hashString}";
            }
        }

        // Método para verificar una contraseña hasheada (CORREGIDO - mismo que AccountController)
        private bool VerifyPassword(string password, string hashedPassword)
        {
            // Verificar primero si la contraseña almacenada ya está hasheada
            if (!hashedPassword.StartsWith("PBKDF2$"))
            {
                // Si la contraseña no está hasheada, comparar directamente (para usuarios antiguos)
                return password == hashedPassword;
            }

            // El formato es PBKDF2$iteraciones$salt==$hash
            string[] parts = hashedPassword.Split('$');
            if (parts.Length != 4)
                return false;

            int iterations;
            if (!int.TryParse(parts[1], out iterations))
                return false;

            string saltBase64 = parts[2];
            string storedHashBase64 = parts[3];

            try
            {
                // Extraer la sal (puede tener '==' al final)
                string actualSaltBase64 = saltBase64;
                if (saltBase64.EndsWith("=="))
                {
                    actualSaltBase64 = saltBase64.Substring(0, saltBase64.Length - 2);
                }

                byte[] salt = Convert.FromBase64String(actualSaltBase64);

                // Recrear el hash con la misma sal y el mismo número de iteraciones
                using (var deriveBytes = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256))
                {
                    byte[] hash = deriveBytes.GetBytes(32); // 256 bits
                    string computedHashBase64 = Convert.ToBase64String(hash);

                    // Comparar el hash calculado con el hash almacenado
                    return computedHashBase64 == storedHashBase64;
                }
            }
            catch
            {
                // Si hay algún error en la conversión, la verificación falla
                return false;
            }
        }

        #endregion


        #region Métodos auxiliares para correo

        // Método para enviar correo personalizado desde administrador a usuario
        private async Task<bool> EnviarCorreoPersonalizado(string destinatario, string nombreDestinatario, string asunto, string mensaje, string nombreAdmin)
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
                        From = new System.Net.Mail.MailAddress(_configuration["Email:FromAddress"], "IngeTech CRM"),
                        Subject = asunto,
                        Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                            .header {{ background-color: #4f46e5; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
                            .content {{ background-color: white; padding: 30px; border-radius: 0 0 8px 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                            .mensaje {{ 
                                background-color: #f8f9fa; 
                                border-left: 4px solid #4f46e5; 
                                padding: 20px; 
                                margin: 20px 0; 
                                border-radius: 4px;
                            }}
                            .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6b7280; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>✉️ Mensaje de IngeTech CRM</h2>
                            </div>
                            <div class='content'>
                                <p>Estimado/a <strong>{nombreDestinatario}</strong>,</p>
                                
                                <div class='mensaje'>
                                    {mensaje.Replace("\n", "<br>")}
                                </div>
                                
                                <p>Este mensaje fue enviado por: <strong>{nombreAdmin}</strong></p>
                                
                                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                                
                                <p>Si tiene alguna pregunta o necesita asistencia adicional, no dude en contactarnos.</p>
                                
                                <p>Saludos cordiales,<br>
                                <strong>Equipo IngeTech CRM</strong></p>
                            </div>
                            <div class='footer'>
                                <p>Este es un correo automático enviado desde el panel de administración de IngeTech CRM.</p>
                                <p>Fecha de envío: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(destinatario);
                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log del error si tienes un sistema de logging configurado
                Console.WriteLine($"Error al enviar correo personalizado: {ex.Message}");
                return false;
            }
        }

        // Método para enviar correo de notificación de cambio de contraseña
        private async Task EnviarCorreoNotificacionCambioContrasena(string destinatario, string nombreUsuario)
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
                        From = new System.Net.Mail.MailAddress(_configuration["Email:FromAddress"], "IngeTech CRM"),
                        Subject = "🔐 Su contraseña ha sido restablecida",
                        Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                            .header {{ background-color: #10b981; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
                            .content {{ background-color: white; padding: 30px; border-radius: 0 0 8px 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                            .alert {{ 
                                background-color: #fef3cd; 
                                border: 1px solid #ffeaa7; 
                                color: #856404; 
                                padding: 15px; 
                                border-radius: 4px; 
                                margin: 20px 0; 
                            }}
                            .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6b7280; }}
                            .security-icon {{ font-size: 24px; color: #10b981; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <span class='security-icon'>🔐</span>
                                <h2>Contraseña Restablecida</h2>
                            </div>
                            <div class='content'>
                                <p>Estimado/a <strong>{nombreUsuario}</strong>,</p>
                                
                                <p>Le informamos que su contraseña ha sido <strong>restablecida exitosamente</strong> por un administrador del sistema.</p>
                                
                                <div class='alert'>
                                    <strong>⚠️ Recomendación de seguridad:</strong><br>
                                    Por su seguridad, le recomendamos cambiar su contraseña tan pronto como inicie sesión nuevamente en el sistema.
                                </div>
                                
                                <p><strong>¿Qué hacer ahora?</strong></p>
                                <ol>
                                    <li>Inicie sesión con su nueva contraseña</li>
                                    <li>Vaya a su perfil de usuario</li>
                                    <li>Cambie su contraseña por una de su preferencia</li>
                                    <li>Asegúrese de que sea segura (mínimo 6 caracteres, incluya letras y números)</li>
                                </ol>
                                
                                <p>Si usted no solicitó este cambio o tiene alguna preocupación de seguridad, contacte inmediatamente al administrador del sistema.</p>
                                
                                <p>Saludos cordiales,<br>
                                <strong>Equipo IngeTech CRM</strong></p>
                            </div>
                            <div class='footer'>
                                <p>Este es un correo automático de seguridad. No responda a este mensaje.</p>
                                <p>Fecha del cambio: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(destinatario);
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo de notificación: {ex.Message}");
            }
        }

        // Método para enviar correo con contraseña temporal
        private async Task EnviarCorreoContrasenaTemporal(string destinatario, string nombreUsuario, string contrasenaTemporal)
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
                        From = new System.Net.Mail.MailAddress(_configuration["Email:FromAddress"], "IngeTech CRM"),
                        Subject = "🔑 Su nueva contraseña temporal",
                        Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                            .header {{ background-color: #f59e0b; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
                            .content {{ background-color: white; padding: 30px; border-radius: 0 0 8px 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                            .password-box {{ 
                                background-color: #f3f4f6; 
                                border: 2px solid #f59e0b; 
                                padding: 20px; 
                                text-align: center; 
                                border-radius: 8px; 
                                margin: 20px 0; 
                            }}
                            .password {{ 
                                font-family: 'Courier New', monospace; 
                                font-size: 24px; 
                                font-weight: bold; 
                                color: #1f2937; 
                                letter-spacing: 2px;
                            }}
                            .alert {{ 
                                background-color: #fee2e2; 
                                border: 1px solid #fecaca; 
                                color: #991b1b; 
                                padding: 15px; 
                                border-radius: 4px; 
                                margin: 20px 0; 
                            }}
                            .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6b7280; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <span style='font-size: 24px;'>🔑</span>
                                <h2>Contraseña Temporal Generada</h2>
                            </div>
                            <div class='content'>
                                <p>Estimado/a <strong>{nombreUsuario}</strong>,</p>
                                
                                <p>Se ha generado una <strong>contraseña temporal</strong> para su cuenta en IngeTech CRM.</p>
                                
                                <div class='password-box'>
                                    <p style='margin: 0; color: #f59e0b; font-weight: bold;'>Su nueva contraseña temporal es:</p>
                                    <div class='password'>{contrasenaTemporal}</div>
                                </div>
                                
                                <div class='alert'>
                                    <strong>🚨 IMPORTANTE - Acción requerida:</strong><br>
                                    Esta es una contraseña temporal. Por seguridad, debe cambiarla inmediatamente después de iniciar sesión.
                                </div>
                                
                                <p><strong>Pasos a seguir:</strong></p>
                                <ol>
                                    <li>Inicie sesión con esta contraseña temporal</li>
                                    <li>Vaya inmediatamente a su perfil</li>
                                    <li>Cambie su contraseña por una nueva y segura</li>
                                    <li>La nueva contraseña debe tener al menos 6 caracteres e incluir letras y números</li>
                                </ol>
                                
                                <p><strong>⚠️ Consideraciones de seguridad:</strong></p>
                                <ul>
                                    <li>No comparta esta contraseña con nadie</li>
                                    <li>Cámbiela tan pronto como sea posible</li>
                                    <li>Si no solicitó este cambio, contacte al administrador inmediatamente</li>
                                </ul>
                                
                                <p>Saludos cordiales,<br>
                                <strong>Equipo IngeTech CRM</strong></p>
                            </div>
                            <div class='footer'>
                                <p>Este es un correo automático de seguridad. No responda a este mensaje.</p>
                                <p>Contraseña generada el: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(destinatario);
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo con contraseña temporal: {ex.Message}");
            }
        }

        #endregion

        #region Métodos para Restablecer Contraseña y Enviar Correo

        // Acción para mostrar el formulario de restablecimiento de contraseña
        public async Task<IActionResult> RestablecerContrasena(int id)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            var usuario = await _context.Usuarios
                .Select(u => new { u.IDENTIFICACION, u.NOMBRE_COMPLETO, u.CORREO_ELECTRONICO })
                .FirstOrDefaultAsync(u => u.IDENTIFICACION == id);

            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.Usuario = usuario;
            return View();
        }

        // Acción para procesar el restablecimiento de contraseña
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(int id, string nuevaContrasena, string confirmarContrasena)
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

            // Validaciones
            if (string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                ModelState.AddModelError("nuevaContrasena", "La nueva contraseña es requerida");
            }
            else if (nuevaContrasena.Length < 6)
            {
                ModelState.AddModelError("nuevaContrasena", "La contraseña debe tener al menos 6 caracteres");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(nuevaContrasena, @"^(?=.*[a-zA-Z])(?=.*\d).+$"))
            {
                ModelState.AddModelError("nuevaContrasena", "La contraseña debe contener al menos una letra y un número");
            }

            if (nuevaContrasena != confirmarContrasena)
            {
                ModelState.AddModelError("confirmarContrasena", "Las contraseñas no coinciden");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Usuario = new
                {
                    usuario.IDENTIFICACION,
                    usuario.NOMBRE_COMPLETO,
                    usuario.CORREO_ELECTRONICO
                };
                return View();
            }

            try
            {
                // Actualizar la contraseña
                usuario.CONTRASENA = HashPassword(nuevaContrasena);
                _context.Update(usuario);
                await _context.SaveChangesAsync();

                // Enviar correo notificando el cambio de contraseña
                await EnviarCorreoNotificacionCambioContrasena(usuario.CORREO_ELECTRONICO, usuario.NOMBRE_COMPLETO);

                TempData["Message"] = $"Contraseña restablecida exitosamente para {usuario.NOMBRE_COMPLETO}";
                return RedirectToAction("Detalles", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al restablecer la contraseña";
                ViewBag.Usuario = new
                {
                    usuario.IDENTIFICACION,
                    usuario.NOMBRE_COMPLETO,
                    usuario.CORREO_ELECTRONICO
                };
                return View();
            }
        }

        // Acción para mostrar el formulario de envío de correo
        public async Task<IActionResult> EnviarCorreo(int id)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            var usuario = await _context.Usuarios
                .Select(u => new { u.IDENTIFICACION, u.NOMBRE_COMPLETO, u.CORREO_ELECTRONICO })
                .FirstOrDefaultAsync(u => u.IDENTIFICACION == id);

            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.Usuario = usuario;
            return View();
        }

        // Acción para procesar el envío de correo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarCorreo(int id, string asunto, string mensaje)
        {
            if (!EsUsuarioAdministrador())
            {
                return RedirectToAction("AccesoDenegado", "Home");
            }

            var usuario = await _context.Usuarios
                .Select(u => new { u.IDENTIFICACION, u.NOMBRE_COMPLETO, u.CORREO_ELECTRONICO })
                .FirstOrDefaultAsync(u => u.IDENTIFICACION == id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(asunto))
            {
                ModelState.AddModelError("asunto", "El asunto es requerido");
            }
            else if (asunto.Length > 200)
            {
                ModelState.AddModelError("asunto", "El asunto no puede tener más de 200 caracteres");
            }

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                ModelState.AddModelError("mensaje", "El mensaje es requerido");
            }
            else if (mensaje.Length > 5000)
            {
                ModelState.AddModelError("mensaje", "El mensaje no puede tener más de 5000 caracteres");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Usuario = usuario;
                return View();
            }

            try
            {
                // Obtener información del administrador que envía el correo
                var adminId = HttpContext.Session.GetInt32("UsuarioId");
                var adminNombre = HttpContext.Session.GetString("NombreUsuario") ?? "Administrador";

                // Enviar el correo
                bool correoEnviado = await EnviarCorreoPersonalizado(
                    usuario.CORREO_ELECTRONICO,
                    usuario.NOMBRE_COMPLETO,
                    asunto,
                    mensaje,
                    adminNombre
                );

                if (correoEnviado)
                {
                    TempData["Message"] = $"Correo enviado exitosamente a {usuario.NOMBRE_COMPLETO} ({usuario.CORREO_ELECTRONICO})";
                }
                else
                {
                    TempData["Error"] = "Ocurrió un error al enviar el correo. Verifique la configuración del servidor de correo.";
                }

                return RedirectToAction("Detalles", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al enviar el correo: {ex.Message}";
                ViewBag.Usuario = usuario;
                return View();
            }
        }

        // Acción para generar contraseña temporal automáticamente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarContrasenaTemp(int id)
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

            try
            {
                // Generar contraseña temporal
                var contrasenaTemporal = GenerarContrasenaTemporal();

                // Actualizar en la base de datos
                usuario.CONTRASENA = HashPassword(contrasenaTemporal);
                _context.Update(usuario);
                await _context.SaveChangesAsync();

                // Enviar correo con la contraseña temporal
                await EnviarCorreoContrasenaTemporal(usuario.CORREO_ELECTRONICO, usuario.NOMBRE_COMPLETO, contrasenaTemporal);

                // Mostrar la contraseña temporal al administrador
                TempData["ContrasenaTemporal"] = contrasenaTemporal;
                TempData["Message"] = $"Contraseña temporal generada y enviada por correo a {usuario.NOMBRE_COMPLETO}";

                return RedirectToAction("Detalles", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al generar la contraseña temporal";
                return RedirectToAction("Detalles", new { id = id });
            }
        }

        #endregion

        #region Métodos auxiliares adicionales

        // Método auxiliar para generar contraseña temporal
        private string GenerarContrasenaTemporal()
        {
            // Usar caracteres que sean fáciles de distinguir (sin 0, O, l, I, etc.)
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var random = new Random();
            var resultado = new char[8];

            // Asegurar que tenga al menos una letra mayúscula, una minúscula y un número
            resultado[0] = "ABCDEFGHJKLMNPQRSTUVWXYZ"[random.Next(23)]; // Mayúscula
            resultado[1] = "abcdefghijkmnopqrstuvwxyz"[random.Next(23)]; // Minúscula  
            resultado[2] = "23456789"[random.Next(8)]; // Número

            // Llenar el resto aleatoriamente
            for (int i = 3; i < resultado.Length; i++)
            {
                resultado[i] = chars[random.Next(chars.Length)];
            }

            // Mezclar el array para que no sea predecible
            for (int i = resultado.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                char temp = resultado[i];
                resultado[i] = resultado[j];
                resultado[j] = temp;
            }

            return new string(resultado);
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