using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IngeTechCRM.Models;

namespace IngeTechCRM.Controllers
{
    [Authorize]
    public class WhatsAppController : Controller
    {
        private readonly IngeTechDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WhatsAppController(IngeTechDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public IActionResult Index()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener las últimas conversaciones
            var conversaciones = _context.ConversacionesWhatsApp
                .Include(c => c.Mensajes)
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.ULTIMA_ACTUALIZACION)
                .Take(20)
                .ToList();

            return View(conversaciones);
        }

        public IActionResult EnviarMensaje()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarMensaje(string telefono, string mensaje)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(telefono) || string.IsNullOrEmpty(mensaje))
            {
                TempData["Error"] = "El número de teléfono y el mensaje son obligatorios";
                return View();
            }

            // Limpiar el formato del teléfono (eliminar espacios, paréntesis, guiones, etc.)
            telefono = new string(telefono.Where(c => char.IsDigit(c)).ToArray());

            // Agregar el código de país si no está presente
            if (!telefono.StartsWith("506") && telefono.Length == 8)
            {
                telefono = "506" + telefono;
            }

            var resultado = await EnviarMensajeWhatsApp(telefono, mensaje);
            if (resultado.Success)
            {
                // Guardar mensaje en la base de datos
                await GuardarMensajeEnviado(telefono, mensaje);
                TempData["Message"] = "Mensaje enviado exitosamente";
            }
            else
            {
                TempData["Error"] = $"Error al enviar mensaje: {resultado.Message}";
            }

            return View();
        }

        public IActionResult EnviarMensajeMasivo()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Provincias = _context.Provincias.ToList();
            ViewBag.TiposUsuario = _context.TiposUsuario.ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarMensajeMasivo(string mensaje, List<int> provinciasSeleccionadas, List<int> tiposUsuarioSeleccionados)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(mensaje))
            {
                TempData["Error"] = "El mensaje es obligatorio";
                ViewBag.Provincias = _context.Provincias.ToList();
                ViewBag.TiposUsuario = _context.TiposUsuario.ToList();
                return View();
            }

            // Buscar usuarios según los filtros
            var usuariosQuery = _context.Usuarios.AsQueryable();

            if (provinciasSeleccionadas != null && provinciasSeleccionadas.Count > 0)
            {
                usuariosQuery = usuariosQuery.Where(u => provinciasSeleccionadas.Contains(u.ID_PROVINCIA));
            }

            if (tiposUsuarioSeleccionados != null && tiposUsuarioSeleccionados.Count > 0)
            {
                usuariosQuery = usuariosQuery.Where(u => tiposUsuarioSeleccionados.Contains(u.ID_TIPO_USUARIO));
            }

            var usuarios = await usuariosQuery.ToListAsync();

            int totalUsuarios = usuarios.Count;
            int mensajesEnviados = 0;
            int mensajesFallidos = 0;

            foreach (var usuario in usuarios)
            {
                if (!string.IsNullOrEmpty(usuario.TELEFONO))
                {
                    // Limpiar el formato del teléfono
                    string telefono = new string(usuario.TELEFONO.Where(c => char.IsDigit(c)).ToArray());

                    // Agregar el código de país si no está presente
                    if (!telefono.StartsWith("506") && telefono.Length == 8)
                    {
                        telefono = "506" + telefono;
                    }

                    // Personalizar mensaje con el nombre del usuario
                    string mensajePersonalizado = mensaje.Replace("{nombre}", usuario.NOMBRE_COMPLETO);

                    var resultado = await EnviarMensajeWhatsApp(telefono, mensajePersonalizado);
                    if (resultado.Success)
                    {
                        await GuardarMensajeEnviado(telefono, mensajePersonalizado);
                        mensajesEnviados++;
                    }
                    else
                    {
                        mensajesFallidos++;
                    }

                    // Pequeña pausa para no sobrecargar la API
                    await Task.Delay(200);
                }
                else
                {
                    mensajesFallidos++;
                }
            }

            TempData["Message"] = $"Proceso completado: {mensajesEnviados} mensajes enviados, {mensajesFallidos} fallidos de un total de {totalUsuarios} usuarios";
            ViewBag.Provincias = _context.Provincias.ToList();
            ViewBag.TiposUsuario = _context.TiposUsuario.ToList();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> WebhookRecepcion([FromBody] JObject data)
        {
            try
            {
                // Verificar si es una notificación de mensaje entrante
                if (data["entry"] != null && data["entry"].HasValues &&
                    data["entry"][0]["changes"] != null && data["entry"][0]["changes"].HasValues &&
                    data["entry"][0]["changes"][0]["value"]["messages"] != null)
                {
                    var messages = data["entry"][0]["changes"][0]["value"]["messages"];
                    foreach (var message in messages)
                    {
                        // Obtener datos del mensaje
                        string from = message["from"]?.ToString();
                        string messageId = message["id"]?.ToString();
                        string messageType = message["type"]?.ToString();

                        // Solo procesar mensajes de texto por ahora
                        if (messageType == "text" && message["text"] != null)
                        {
                            string messageBody = message["text"]["body"]?.ToString();
                            DateTime timestamp = DateTime.Now;

                            if (message["timestamp"] != null)
                            {
                                // Convertir timestamp de Unix a DateTime
                                long unixTime = long.Parse(message["timestamp"].ToString());
                                timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                            }

                            // Guardar mensaje recibido
                            await GuardarMensajeRecibido(from, messageBody, messageId, timestamp);

                            // Generar respuesta para el usuario
                            string respuesta = await GenerarRespuesta(from, messageBody);

                            // Enviar respuesta si no está vacía
                            if (!string.IsNullOrEmpty(respuesta))
                            {
                                await EnviarMensajeWhatsApp(from, respuesta);
                                await GuardarMensajeEnviado(from, respuesta);
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // Registro del error para diagnóstico
                Console.WriteLine($"Error en webhook: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult WebhookVerificacion([FromQuery] string hub_mode, [FromQuery] string hub_verify_token, [FromQuery] string hub_challenge)
        {
            // Verificar el token
            string tokenVerificacion = _configuration["WhatsAppSettings:VerifyToken"];

            if (hub_mode == "subscribe" && hub_verify_token == tokenVerificacion)
            {
                return Content(hub_challenge);
            }

            return BadRequest();
        }

        public IActionResult Conversacion(string telefono)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Limpiar formato del teléfono
            telefono = new string(telefono.Where(c => char.IsDigit(c)).ToArray());

            // Buscar conversación
            var conversacion = _context.ConversacionesWhatsApp
                .Include(c => c.Mensajes.OrderBy(m => m.FECHA_HORA))
                .Include(c => c.Usuario)
                .FirstOrDefault(c => c.NUMERO_TELEFONO == telefono);

            if (conversacion == null)
            {
                // Si no existe la conversación, crear una nueva
                conversacion = new ConversacionWhatsApp
                {
                    NUMERO_TELEFONO = telefono,
                    FECHA_CREACION = DateTime.Now,
                    ULTIMA_ACTUALIZACION = DateTime.Now,
                    Mensajes = new List<MensajeWhatsApp>()
                };
            }

            return View(conversacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResponderMensaje(string telefono, string mensaje)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(telefono) || string.IsNullOrEmpty(mensaje))
            {
                return BadRequest("El número de teléfono y el mensaje son obligatorios");
            }

            // Limpiar el formato del teléfono
            telefono = new string(telefono.Where(c => char.IsDigit(c)).ToArray());

            var resultado = await EnviarMensajeWhatsApp(telefono, mensaje);
            if (resultado.Success)
            {
                await GuardarMensajeEnviado(telefono, mensaje);
                return Ok(new { success = true, message = "Mensaje enviado exitosamente" });
            }
            else
            {
                return BadRequest(new { success = false, message = $"Error al enviar mensaje: {resultado.Message}" });
            }
        }

        // Método para enviar mensaje a través de la API de WhatsApp
        private async Task<(bool Success, string Message)> EnviarMensajeWhatsApp(string telefono, string mensaje)
        {
            try
            {
                // Obtener configuración de la API de WhatsApp
                string apiUrl = _configuration["WhatsAppSettings:ApiUrl"];
                string phoneNumberId = _configuration["WhatsAppSettings:PhoneNumberId"];
                string accessToken = _configuration["WhatsAppSettings:AccessToken"];

                // En un entorno de desarrollo/prueba, podemos simular el envío
                if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(accessToken))
                {
                    // Simular el envío para pruebas
                    return (true, "Mensaje simulado (entorno de desarrollo)");
                }

                // Construir la URL completa
                string url = $"{apiUrl}{phoneNumberId}/messages";

                // Construir el objeto de datos para la solicitud
                var requestData = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = telefono,
                    type = "text",
                    text = new
                    {
                        body = mensaje
                    }
                };

                // Convertir a JSON
                string jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Configurar encabezados
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                // Enviar solicitud
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Mensaje enviado correctamente");
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return (false, $"Error HTTP {response.StatusCode}: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Excepción: {ex.Message}");
            }
        }

        // Método para guardar mensajes recibidos en la base de datos
        private async Task GuardarMensajeRecibido(string telefono, string texto, string messageId, DateTime timestamp)
        {
            // Limpiar formato del teléfono
            telefono = new string(telefono.Where(c => char.IsDigit(c)).ToArray());

            // Buscar o crear conversación
            var conversacion = await _context.ConversacionesWhatsApp
                .FirstOrDefaultAsync(c => c.NUMERO_TELEFONO == telefono);

            if (conversacion == null)
            {
                // Buscar si existe un usuario con este teléfono
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.TELEFONO.Contains(telefono));

                string nombreContacto = usuario != null ? usuario.NOMBRE_COMPLETO : "Desconocido";
                int? idUsuario = usuario != null ? (int?)usuario.IDENTIFICACION : null;

                conversacion = new ConversacionWhatsApp
                {
                    NUMERO_TELEFONO = telefono,
                    NOMBRE_CONTACTO = nombreContacto,
                    ID_USUARIO = idUsuario,
                    FECHA_CREACION = DateTime.Now,
                    ULTIMA_ACTUALIZACION = DateTime.Now
                };

                _context.ConversacionesWhatsApp.Add(conversacion);
                await _context.SaveChangesAsync();
            }

            // Crear mensaje
            var mensaje = new MensajeWhatsApp
            {
                ID_CONVERSACION = conversacion.ID_CONVERSACION,
                TEXTO = texto,
                FECHA_HORA = timestamp,
                ES_ENTRANTE = true,
                ID_MENSAJE_EXTERNO = messageId
            };

            // Actualizar fecha de última actualización de la conversación
            conversacion.ULTIMA_ACTUALIZACION = DateTime.Now;

            _context.MensajesWhatsApp.Add(mensaje);
            await _context.SaveChangesAsync();
        }

        // Método para guardar mensajes enviados en la base de datos
        private async Task GuardarMensajeEnviado(string telefono, string texto)
        {
            // Limpiar formato del teléfono
            telefono = new string(telefono.Where(c => char.IsDigit(c)).ToArray());

            // Buscar o crear conversación
            var conversacion = await _context.ConversacionesWhatsApp
                .FirstOrDefaultAsync(c => c.NUMERO_TELEFONO == telefono);

            if (conversacion == null)
            {
                // Buscar si existe un usuario con este teléfono
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.TELEFONO.Contains(telefono));

                string nombreContacto = usuario != null ? usuario.NOMBRE_COMPLETO : "Desconocido";
                int? idUsuario = usuario != null ? (int?)usuario.IDENTIFICACION : null;

                conversacion = new ConversacionWhatsApp
                {
                    NUMERO_TELEFONO = telefono,
                    NOMBRE_CONTACTO = nombreContacto,
                    ID_USUARIO = idUsuario,
                    FECHA_CREACION = DateTime.Now,
                    ULTIMA_ACTUALIZACION = DateTime.Now
                };

                _context.ConversacionesWhatsApp.Add(conversacion);
                await _context.SaveChangesAsync();
            }

            // Crear mensaje
            var mensaje = new MensajeWhatsApp
            {
                ID_CONVERSACION = conversacion.ID_CONVERSACION,
                TEXTO = texto,
                FECHA_HORA = DateTime.Now,
                ES_ENTRANTE = false
            };

            // Actualizar fecha de última actualización de la conversación
            conversacion.ULTIMA_ACTUALIZACION = DateTime.Now;

            _context.MensajesWhatsApp.Add(mensaje);
            await _context.SaveChangesAsync();
        }

        // Método para generar respuestas automáticas
        private async Task<string> GenerarRespuesta(string telefono, string mensajeEntrante)
        {
            // Limpiar formato del teléfono
            telefono = new string(telefono.Where(c => char.IsDigit(c)).ToArray());

            // Obtener contexto de la conversación
            var conversacion = await _context.ConversacionesWhatsApp
                .Include(c => c.Mensajes.OrderByDescending(m => m.FECHA_HORA).Take(5))
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.NUMERO_TELEFONO == telefono);

            // Obtener usuario si existe
            var usuario = conversacion?.Usuario ?? await _context.Usuarios
                .FirstOrDefaultAsync(u => u.TELEFONO.Contains(telefono));

            // Convertir mensaje a minúsculas para facilitar la comparación
            string mensajeLower = mensajeEntrante.ToLower();

            // Buscar configuraciones del chatbot
            var configuraciones = await _context.ConfiguracionesChatbot
                .Where(c => c.ACTIVO)
                .ToListAsync();

            // Verificar si es un mensaje de bienvenida
            if (conversacion == null || conversacion.Mensajes.Count <= 1)
            {
                var mensajeBienvenida = configuraciones
                    .FirstOrDefault(c => c.TIPO == "Bienvenida")?.RESPUESTA;

                if (!string.IsNullOrEmpty(mensajeBienvenida) && usuario != null)
                {
                    mensajeBienvenida = mensajeBienvenida.Replace("{nombre}", usuario.NOMBRE_COMPLETO);
                }
                else if (!string.IsNullOrEmpty(mensajeBienvenida))
                {
                    mensajeBienvenida = mensajeBienvenida.Replace("{nombre}", "");
                }
                else
                {
                    mensajeBienvenida = "¡Hola! Bienvenido al chatbot de IngeTech. ¿En qué podemos ayudarte hoy?";
                }

                return mensajeBienvenida;
            }

            // Buscar respuestas preprogramadas
            foreach (var config in configuraciones.Where(c => c.TIPO == "PalabraClave"))
            {
                if (!string.IsNullOrEmpty(config.PALABRA_CLAVE) &&
                    mensajeLower.Contains(config.PALABRA_CLAVE.ToLower()))
                {
                    string respuesta = config.RESPUESTA;
                    if (usuario != null)
                    {
                        respuesta = respuesta.Replace("{nombre}", usuario.NOMBRE_COMPLETO);
                    }
                    return respuesta;
                }
            }

            // Responder a preguntas sobre horarios
            if (mensajeLower.Contains("horario") || mensajeLower.Contains("abierto") ||
                mensajeLower.Contains("cerrado") || mensajeLower.Contains("atienden"))
            {
                return "Nuestro horario de atención es de lunes a viernes de 8:00 AM a 5:00 PM.";
            }

            // Responder a preguntas sobre ubicación
            if (mensajeLower.Contains("dónde") || mensajeLower.Contains("donde") ||
                mensajeLower.Contains("ubicación") || mensajeLower.Contains("ubicacion") ||
                mensajeLower.Contains("dirección") || mensajeLower.Contains("direccion"))
            {
                return "Estamos ubicados en San José, Costa Rica. Puede visitarnos en nuestra oficina central o contactarnos por este medio para más información.";
            }

            // Responder a preguntas sobre servicios
            if (mensajeLower.Contains("servicio") || mensajeLower.Contains("ofrece") ||
                mensajeLower.Contains("vende") || mensajeLower.Contains("producto"))
            {
                return "Ofrecemos servicios de desarrollo de software, consultoría tecnológica y soluciones a medida. ¿Hay algún servicio específico que le interese?";
            }

            // Responder a saludos
            if (mensajeLower.Contains("hola") || mensajeLower.Contains("buenos días") ||
                mensajeLower.Contains("buenas tardes") || mensajeLower.Contains("buenas noches"))
            {
                string saludo = "¡Hola! ";
                if (usuario != null)
                {
                    saludo += $"Hola {usuario.NOMBRE_COMPLETO}. ";
                }
                saludo += "¿En qué podemos ayudarte hoy?";
                return saludo;
            }

            // Responder a agradecimientos
            if (mensajeLower.Contains("gracias") || mensajeLower.Contains("thank") ||
                mensajeLower.Contains("genial") || mensajeLower.Contains("excelente"))
            {
                return "¡De nada! Estamos para servirle. ¿Hay algo más en lo que podamos ayudarte?";
            }

            // Responder a despedidas
            if (mensajeLower.Contains("adiós") || mensajeLower.Contains("adios") ||
                mensajeLower.Contains("hasta luego") || mensajeLower.Contains("bye") ||
                mensajeLower.Contains("chao"))
            {
                return "¡Hasta pronto! Gracias por contactarnos. Si necesitas algo más, no dudes en escribirnos.";
            }

            // Mensaje por defecto si no se reconoce la intención
            var mensajeDefault = configuraciones
                .FirstOrDefault(c => c.TIPO == "Default")?.RESPUESTA;

            if (string.IsNullOrEmpty(mensajeDefault))
            {
                mensajeDefault = "Gracias por tu mensaje. Un asesor se pondrá en contacto contigo a la brevedad posible.";
            }

            return mensajeDefault;
        }

        public IActionResult ConfigurarChatbot()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var configuraciones = _context.ConfiguracionesChatbot.ToList();
            return View(configuraciones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracion(ConfiguracionChatbot configuracion)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (configuracion.ID_CONFIGURACION > 0)
            {
                // Actualizar configuración existente
                var configExistente = await _context.ConfiguracionesChatbot.FindAsync(configuracion. ID_CONFIGURACION);
                if (configExistente != null)
                {
                    configExistente.TIPO = configuracion.TIPO;
                    configExistente.PALABRA_CLAVE = configuracion.PALABRA_CLAVE;
                    configExistente.RESPUESTA = configuracion.RESPUESTA;
                    configExistente.ACTIVO = configuracion.ACTIVO;
                    _context.Update(configExistente);
                }
            }
            else
            {
                // Crear nueva configuración
                _context.ConfiguracionesChatbot.Add(configuracion);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Configuración guardada exitosamente";
            return RedirectToAction(nameof(ConfigurarChatbot));
        }

        [HttpPost]
        public async Task<IActionResult> EliminarConfiguracion(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var configuracion = await _context.ConfiguracionesChatbot.FindAsync(id);
            if (configuracion != null)
            {
                _context.ConfiguracionesChatbot.Remove(configuracion);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }

            return NotFound();
        }

        public IActionResult Estadisticas()
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener estadísticas generales
            var hoy = DateTime.Today;
            var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek);
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            var estadisticas = new Dictionary<string, int>
            {
                { "TotalConversaciones", _context.ConversacionesWhatsApp.Count() },
                { "ConversacionesHoy", _context.ConversacionesWhatsApp.Count(c => c.FECHA_CREACION.Date == hoy) },
                { "ConversacionesSemana", _context.ConversacionesWhatsApp.Count(c => c.FECHA_CREACION >= inicioSemana) },
                { "ConversacionesMes", _context.ConversacionesWhatsApp.Count(c => c.FECHA_CREACION >= inicioMes) },
                { "TotalMensajes", _context.MensajesWhatsApp.Count() },
                { "MensajesEntrantes", _context.MensajesWhatsApp.Count(m => m.ES_ENTRANTE) },
                { "MensajesSalientes", _context.MensajesWhatsApp.Count(m => !m.ES_ENTRANTE) },
                { "MensajesHoy", _context.MensajesWhatsApp.Count(m => m.FECHA_HORA.Date == hoy) }
            };

            // Usuarios más activos (por cantidad de mensajes enviados)
            var usuariosActivos = _context.ConversacionesWhatsApp
                .Where(c => c.ID_USUARIO != null)
                .OrderByDescending(c => c.Mensajes.Count)
                .Take(10)
                .Select(c => new
                {
                    Usuario = c.Usuario.NOMBRE_COMPLETO,
                    Telefono = c.NUMERO_TELEFONO,
                    TotalMensajes = c.Mensajes.Count,
                    UltimoMensaje = c.ULTIMA_ACTUALIZACION
                })
                .ToList();

            ViewBag.Estadisticas = estadisticas;
            ViewBag.UsuariosActivos = usuariosActivos;

            return View();
        }

        public IActionResult ExportarConversacion(int id)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Buscar conversación
            var conversacion = _context.ConversacionesWhatsApp
                .Include(c => c.Mensajes.OrderBy(m => m.FECHA_HORA))
                .Include(c => c.Usuario)
                .FirstOrDefault(c => c.ID_CONVERSACION == id);

            if (conversacion == null)
            {
                return NotFound();
            }

            // Crear contenido del archivo de exportación
            var sb = new StringBuilder();
            sb.AppendLine($"Conversación con: {conversacion.NOMBRE_CONTACTO} ({conversacion.NUMERO_TELEFONO})");
            sb.AppendLine($"Inicio: {conversacion.FECHA_CREACION}");
            sb.AppendLine($"Usuario en sistema: {(conversacion.Usuario != null ? conversacion.Usuario.NOMBRE_COMPLETO : "No vinculado")}");
            sb.AppendLine("-------------------------------------------------------");
            sb.AppendLine();

            foreach (var mensaje in conversacion.Mensajes)
            {
                string autor = mensaje.ES_ENTRANTE ? conversacion.NOMBRE_CONTACTO : "IngeTech";
                sb.AppendLine($"[{mensaje.FECHA_HORA}] {autor}:");
                sb.AppendLine(mensaje.TEXTO);
                sb.AppendLine();
            }

            // Generar nombre del archivo
            string nombreArchivo = $"Conversacion_{conversacion.NUMERO_TELEFONO}_{DateTime.Now:yyyyMMdd}.txt";
            byte[] fileBytes = Encoding.UTF8.GetBytes(sb.ToString());

            // Devolver archivo para descarga
            return File(fileBytes, "text/plain", nombreArchivo);
        }

        public IActionResult BuscarConversaciones(string termino)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(termino))
            {
                return View(new List<ConversacionWhatsApp>());
            }

            // Buscar conversaciones que coincidan con el término de búsqueda
            var conversaciones = _context.ConversacionesWhatsApp
                .Include(c => c.Mensajes.OrderByDescending(m => m.FECHA_HORA).Take(1))
                .Include(c => c.Usuario)
                .Where(c => c.NUMERO_TELEFONO.Contains(termino) ||
                            c.NOMBRE_CONTACTO.Contains(termino) ||
                            (c.Usuario != null && c.Usuario.NOMBRE_COMPLETO.Contains(termino)) ||
                            c.Mensajes.Any(m => m.TEXTO.Contains(termino)))
                .OrderByDescending(c => c.ULTIMA_ACTUALIZACION)
                .Take(50)
                .ToList();

            return View(conversaciones);
        }

        // API para verificar nuevos mensajes (para actualización en tiempo real)
        [HttpGet]
        public IActionResult VerificarNuevosMensajes(int idConversacion, DateTime ultimaActualizacion)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return Unauthorized();
            }

            // Buscar nuevos mensajes
            var nuevosMensajes = _context.MensajesWhatsApp
                .Where(m => m.ID_CONVERSACION == idConversacion && m.FECHA_HORA > ultimaActualizacion)
                .OrderBy(m => m.FECHA_HORA)
                .ToList();

            return Ok(new
            {
                mensajes = nuevosMensajes.Select(m => new {
                    id = m.ID_MENSAJE,
                    texto = m.TEXTO,
                    fechaHora = m.FECHA_HORA,
                    esEntrante = m.ES_ENTRANTE
                }),
                cantidadNuevos = nuevosMensajes.Count
            });
        }

        // Método para actualizar datos de una conversación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarConversacion(int id, string nombreContacto, string notas)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var conversacion = await _context.ConversacionesWhatsApp.FindAsync(id);
            if (conversacion == null)
            {
                return NotFound();
            }

            conversacion.NOMBRE_CONTACTO = nombreContacto;
            conversacion.NOTAS = notas;

            _context.Update(conversacion);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Información de la conversación actualizada correctamente";
            return RedirectToAction(nameof(Conversacion), new { telefono = conversacion.NUMERO_TELEFONO });
        }

        // Método para vincular una conversación con un usuario existente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VincularUsuario(int idConversacion, int idUsuario)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var conversacion = await _context.ConversacionesWhatsApp.FindAsync(idConversacion);
            if (conversacion == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null)
            {
                return NotFound();
            }

            conversacion.ID_USUARIO = idUsuario;
            conversacion.NOMBRE_CONTACTO = usuario.NOMBRE_COMPLETO;

            _context.Update(conversacion);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Conversación vinculada correctamente al usuario";
            return RedirectToAction(nameof(Conversacion), new { telefono = conversacion.NUMERO_TELEFONO });
        }

        // Método para marcar una conversación como resuelta o activa
        [HttpPost]
        public async Task<IActionResult> CambiarEstadoConversacion(int id, string estado)
        {
            // Verificar si el usuario es administrador
            var tipoUsuarioId = HttpContext.Session.GetInt32("TipoUsuarioId");
            if (tipoUsuarioId != 1)
            {
                return Unauthorized();
            }

            var conversacion = await _context.ConversacionesWhatsApp.FindAsync(id);
            if (conversacion == null)
            {
                return NotFound();
            }

            // Validar que el estado sea válido
            if (estado != "Activo" && estado != "Resuelto" && estado != "Pausado")
            {
                return BadRequest("Estado no válido");
            }

            conversacion.ESTADO = estado;
            _context.Update(conversacion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Conversación marcada como {estado}" });
        }
    }
}
