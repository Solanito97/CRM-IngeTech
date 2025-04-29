using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IngeTechCRM.Models;

namespace IngeTechCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly IngeTechDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(IngeTechDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            // Si ya está autenticado, redirigir al inicio
            if (HttpContext.Session.GetInt32("UsuarioId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string correo, string contrasena)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrasena))
            {
                ViewBag.Error = "Por favor ingrese correo y contraseña";
                return View();
            }

            // Buscar usuario por correo
            var usuario = await _context.Usuarios
                .Include(u => u.TipoUsuario)
                .FirstOrDefaultAsync(u => u.CORREO_ELECTRONICO == correo);

            if (usuario == null)
            {
                ViewBag.Error = "Usuario no encontrado";
                return View();
            }

            // Verificar la contraseña hasheada
            if (!VerifyPassword(contrasena, usuario.CONTRASENA))
            {
                ViewBag.Error = "Contraseña incorrecta";
                return View();
            }

            // Actualizar último acceso
            usuario.ULTIMO_ACCESO = DateTime.Now;
            _context.Update(usuario);
            await _context.SaveChangesAsync();

            // Crear claims para la autenticación
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.NOMBRE_USUARIO),
                new Claim(ClaimTypes.Email, usuario.CORREO_ELECTRONICO),
                new Claim(ClaimTypes.NameIdentifier, usuario.IDENTIFICACION.ToString()),
                new Claim(ClaimTypes.Role, usuario.TipoUsuario.DESCRIPCION)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Guardar información del usuario en sesión
            HttpContext.Session.SetInt32("UsuarioId", usuario.IDENTIFICACION);
            HttpContext.Session.SetString("NombreUsuario", usuario.NOMBRE_USUARIO);
            HttpContext.Session.SetInt32("TipoUsuarioId", usuario.ID_TIPO_USUARIO);
            HttpContext.Session.SetString("TipoUsuario", usuario.TipoUsuario.DESCRIPCION);

            // Redirigir según el tipo de usuario
            if (usuario.ID_TIPO_USUARIO == 1) // Administrador
            {
                return RedirectToAction("Dashboard", "Home");
            }
            else // Cliente
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Register()
        {
            // Si ya está autenticado, redirigir al inicio
            if (HttpContext.Session.GetInt32("UsuarioId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Provincias = _context.Provincias.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Usuario usuario, string confirmarContrasena)
        {
            ModelState.Remove("ProductosCreados");
            ModelState.Remove("ComunicadosCreados");
            ModelState.Remove("MovimientosInventario");
            ModelState.Remove("Carritos");
            ModelState.Remove("Pedidos");
            ModelState.Remove("ComunicadosRecibidos");
            ModelState.Remove("Provincia");
            ModelState.Remove("TipoUsuario");
            ViewBag.Provincias = _context.Provincias.ToList();

            if (!ModelState.IsValid)
            {
                return View(usuario);
            }

            if (usuario.CONTRASENA != confirmarContrasena)
            {
                ModelState.AddModelError("ConfirmarContrasena", "Las contraseñas no coinciden");
                return View(usuario);
            }

            // Verificar si el correo ya está registrado
            var existeCorreo = await _context.Usuarios.AnyAsync(u => u.CORREO_ELECTRONICO == usuario.CORREO_ELECTRONICO);
            if (existeCorreo)
            {
                ModelState.AddModelError("CorreoElectronico", "Este correo electrónico ya está registrado");
                return View(usuario);
            }

            // Verificar si la identificación ya está registrada
            var existeIdentificacion = await _context.Usuarios.AnyAsync(u => u.IDENTIFICACION == usuario.IDENTIFICACION);
            if (existeIdentificacion)
            {
                ModelState.AddModelError("Identificacion", "Esta identificación ya está registrada");
                return View(usuario);
            }

            // Por defecto, los nuevos usuarios son clientes (tipo usuario 2)
            usuario.ID_TIPO_USUARIO = 2;
            usuario.FECHA_REGISTRO = DateTime.Now;
            usuario.ULTIMO_ACCESO = DateTime.Now;

            // Hashear la contraseña antes de guardarla
            string plainPassword = usuario.CONTRASENA;
            usuario.CONTRASENA = HashPassword(plainPassword);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Crear un carrito inicial para el usuario
            var carrito = new Carrito
            {
                ID_USUARIO = usuario.IDENTIFICACION,
                FECHA_CREACION = DateTime.Now,
                ACTIVO = true
            };

            _context.Carritos.Add(carrito);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Registro exitoso. Por favor inicie sesión.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            // Limpiar la sesión
            HttpContext.Session.Clear();

            // Cerrar sesión de autenticación
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> MiPerfil()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Provincia)
                .Include(u => u.TipoUsuario)
                .FirstOrDefaultAsync(u => u.IDENTIFICACION == usuarioId);

            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.Provincias = _context.Provincias.ToList();
            return View(usuario);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost]
        public async Task<IActionResult> ActualizarPerfil(Usuario usuarioActualizado, string nuevaContrasena)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (!usuarioId.HasValue)
            {
                return RedirectToAction("Login");
            }

            // Obtener el usuario actual
            var usuario = await _context.Usuarios.FindAsync(usuarioId.Value);
            if (usuario == null)
            {
                return NotFound();
            }

            // Actualizar solo los campos permitidos
            usuario.NOMBRE_USUARIO = usuarioActualizado.NOMBRE_USUARIO;
            usuario.NOMBRE_COMPLETO = usuarioActualizado.NOMBRE_COMPLETO;
            usuario.TELEFONO = usuarioActualizado.TELEFONO;
            usuario.DIRECCION_COMPLETA = usuarioActualizado.DIRECCION_COMPLETA;
            usuario.ID_PROVINCIA = usuarioActualizado.ID_PROVINCIA;

            // Actualizar la contraseña si se proporciona una nueva
            if (!string.IsNullOrEmpty(nuevaContrasena))
            {
                usuario.CONTRASENA = HashPassword(nuevaContrasena);
            }

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Perfil actualizado correctamente";
            return RedirectToAction("MiPerfil");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // Método para hacer hash de contraseñas
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
                
                // Formato: PBKDF2$iteraciones$salt==$hash
                return $"PBKDF2${iterations}${saltString}==${hashString}";
            }
        }

        // Método para verificar contraseñas
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


        public IActionResult OlvidarContrasena()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OlvidarContrasena(string correo)
        {
            if (string.IsNullOrEmpty(correo))
            {
                ViewBag.Error = "Por favor ingrese su correo electrónico";
                return View();
            }

            // Buscar si el correo existe en la base de datos
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.CORREO_ELECTRONICO == correo);
            if (usuario == null)
            {
                // No informamos al usuario que el correo no existe por seguridad
                ViewBag.Message = "Si su correo está registrado, recibirá un enlace para restablecer su contraseña";
                return View();
            }

            // Generar token único para restablecer contraseña (válido por 24 horas)
            string token = GenerateResetToken();

            // Almacenar token en la base de datos
            var resetToken = new ResetPasswordToken
            {
                TOKEN = token,
                ID_USUARIO = usuario.IDENTIFICACION,
                FECHA_EXPIRACION = DateTime.Now.AddHours(24),
                UTILIZADO = false
            };

            _context.ResetPasswordTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // Construir el enlace para restablecer contraseña
            var callbackUrl = Url.Action("RestablecerContrasena", "Account",
                new { usuarioId = usuario.IDENTIFICACION, token = token },
                protocol: HttpContext.Request.Scheme);

            // Enviar correo con el enlace
            await EnviarCorreoRecuperacion(usuario.CORREO_ELECTRONICO, callbackUrl);

            ViewBag.Message = "Si su correo está registrado, recibirá un enlace para restablecer su contraseña";
            return View();
        }

        public async Task<IActionResult> RestablecerContrasena(int usuarioId, string token)
        {
            // Verificar si el token es válido
            var resetToken = await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.ID_USUARIO == usuarioId &&
                                          t.TOKEN == token &&
                                          t.FECHA_EXPIRACION > DateTime.Now &&
                                          !t.UTILIZADO);

            if (resetToken == null)
            {
                return RedirectToAction("TokenInvalido");
            }

            var model = new ResetPasswordViewModel
            {
                UsuarioId = usuarioId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RestablecerContrasena(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar si el token es válido
            var resetToken = await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.ID_USUARIO == model.UsuarioId &&
                                          t.TOKEN == model.Token &&
                                          t.FECHA_EXPIRACION > DateTime.Now &&
                                          !t.UTILIZADO);

            if (resetToken == null)
            {
                return RedirectToAction("TokenInvalido");
            }

            // Obtener el usuario
            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null)
            {
                return NotFound();
            }

            // Actualizar la contraseña
            usuario.CONTRASENA = HashPassword(model.NuevaContrasena);
            _context.Update(usuario);

            // Marcar el token como utilizado
            resetToken.UTILIZADO = true;
            _context.Update(resetToken);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Su contraseña ha sido restablecida exitosamente";
            return RedirectToAction("Login");
        }

        public IActionResult TokenInvalido()
        {
            return View();
        }

        // Método para generar un token aleatorio
        private string GenerateResetToken()
        {
            byte[] tokenData = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenData);
            }
            return Convert.ToBase64String(tokenData).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        // Método para enviar el correo de recuperación
        private async Task EnviarCorreoRecuperacion(string email, string resetLink)
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
                        Subject = "Recuperación de Contraseña - IngeTech CRM",
                        Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #4f46e5; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .button-container {{ text-align: center; margin: 20px 0; }}
                            .button {{ 
                                background-color: #4f46e5; 
                                color: #ffffff !important; 
                                font-weight: bold;
                                padding: 12px 24px; 
                                text-decoration: none; 
                                border-radius: 5px; 
                                display: inline-block;
                            }}
                            .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6b7280; }}
                
                            /* Asegurar que la URL sea visible si el botón falla */
                            .fallback-link {{ 
                                display: block; 
                                margin-top: 10px; 
                                word-break: break-all; 
                                font-size: 12px;
                                color: #4f46e5;
                            }}
                        </style>
                    </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Recuperación de Contraseña</h2>
                        </div>
                        <div class='content'>
                            <p>Ha solicitado restablecer su contraseña en IngeTech CRM.</p>
                            <p>Para crear una nueva contraseña, haga clic en el siguiente botón:</p>
                            <div class='button-container'>
                                <a class='button' href='{resetLink}' style='color: #ffffff !important;'>Restablecer Contraseña</a>
                            </div>
                            <p class='fallback-link'>Si el botón no funciona, copie y pegue esta URL en su navegador: <br>{resetLink}</p>
                            <p>Este enlace expirará en 24 horas.</p>
                            <p>Si no solicitó restablecer su contraseña, puede ignorar este correo.</p>
                        </div>
                        <div class='footer'>
                            <p>Este es un correo automático, por favor no responda a este mensaje.</p>
                        </div>
                    </div>
                </body>
                </html>
            ",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Registrar el error (puedes usar un sistema de logging apropiado)
                Console.WriteLine($"Error al enviar correo de recuperación: {ex.Message}");
            }
        }



    }
}