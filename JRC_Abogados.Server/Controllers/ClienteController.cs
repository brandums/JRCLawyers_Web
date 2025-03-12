using JRC_Abogados.Server.DataBaseContext;
using JRC_Abogados.Server.Models;
using JRC_Abogados.Server.Models.audits;
using JRC_Abogados.Server.Models.EmailHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace JRC_Abogados.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public ClienteController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            var clientes = await _context.Cliente.ToListAsync();

            foreach (var cliente in clientes)
            {
                cliente.Ubicacion = await _context.Ubicacion.FindAsync(cliente.UbicacionId);
            }

            return clientes;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            var cliente = await _context.Cliente.FindAsync(id);
            cliente.Ubicacion = await _context.Ubicacion.FindAsync(cliente.UbicacionId);

            if (cliente == null)
            {
                return NotFound();
            }

            return cliente;
        }

        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            var existingCliente = await _context.Cliente.FirstOrDefaultAsync(c => c.CorreoElectronico == cliente.CorreoElectronico);

            if (existingCliente != null)
            {
                return Conflict(new { message = "El cliente ya está registrado con este correo electrónico." });
            }

            foreach (var property in cliente.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(cliente) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            try
            {
                SendEmail(cliente);

                TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                cliente.Nombre = textInfo.ToTitleCase(cliente.Nombre.ToLower());
                cliente.Apellido = textInfo.ToTitleCase(cliente.Apellido.ToLower());
                cliente.Ubicacion = null;
                _context.Cliente.Add(cliente);
                await _context.SaveChangesAsync();

                var auditoria = new ClienteAudit
                {
                    Id = 0,
                    ClienteId = cliente.Id,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido,
                    FechaNacimiento = cliente.FechaNacimiento,
                    Telefono = cliente.Telefono,
                    CorreoElectronico = cliente.CorreoElectronico,
                    UbicacionId = cliente.UbicacionId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "CREAR",
                    EmpleadoId = cliente.EmpleadoId,
                    EmpleadoAccionId = cliente.EmpleadoId,
                    DetallesAccion = "Cliente creado"
                };

                _context.ClienteAudit.Add(auditoria);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetCliente", new { id = cliente.Id }, cliente);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = ex.Message, innerException = ex.InnerException?.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpGet("sendCreationEmail")]
        public async void SendEmail(Cliente cliente)
        {
            string emailSubject = "¡Bienvenido a JRC Abogados!";
            string emailBody = $"<p>Estimado/a {cliente.Nombre},</p>" +
                   "<p>Es un placer para nosotros darle la más cordial bienvenida a JRC Abogados. Agradecemos la confianza que ha depositado en nuestro equipo para atender sus necesidades legales.</p>" +
                   "<p>En JRC Abogados, nos enorgullece ofrecer un servicio jurídico de excelencia, respaldado por años de experiencia y un compromiso inquebrantable con la justicia y la satisfacción de nuestros clientes.</p>" +
                   "<p>A partir de este momento, cuenta con un equipo de profesionales dedicados que estarán a su disposición para asesorarlo y representarlo en todo lo que necesite. Nuestro objetivo es brindarle soluciones eficientes y personalizadas, siempre manteniendo una comunicación abierta y transparente.</p>" +
                   "<p>Nuevamente, agradecemos su confianza y esperamos construir una relación duradera basada en la excelencia y el respeto mutuo.</p>" +
                   "<p>Atentamente,<br>" +
                   "Equipo de JRC Abogados</p>";


            await _emailSender.SendEmailAsync(cliente.CorreoElectronico, emailSubject, emailBody);
        }


        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutCliente(int id, int empleadoId, Cliente cliente)
        {
            if (id != cliente.Id)
            {
                return BadRequest(new { mensaje = "El ID no coincide con el ID del cliente proporcionado en el cuerpo de la solicitud." });
            }

            var clienteActual = await _context.Cliente.FindAsync(id);
            if (clienteActual == null)
            {
                return NotFound(new { mensaje = $"No se encontró un cliente con el ID {id}." });
            }

            foreach (var property in cliente.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(cliente) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            var detallesAccion = new StringBuilder();

            if (clienteActual.Nombre != cliente.Nombre)
            {
                detallesAccion.AppendLine($"Nombre cambiado de '{clienteActual.Nombre}' a '{cliente.Nombre}'");
            }
            if (clienteActual.Apellido != cliente.Apellido)
            {
                detallesAccion.AppendLine($"Apellido cambiado de '{clienteActual.Apellido}' a '{cliente.Apellido}'");
            }
            if (clienteActual.FechaNacimiento != cliente.FechaNacimiento)
            {
                detallesAccion.AppendLine($"Fecha de nacimiento cambiada de '{clienteActual.FechaNacimiento}' a '{cliente.FechaNacimiento}'");
            }
            if (clienteActual.Telefono != cliente.Telefono)
            {
                detallesAccion.AppendLine($"Teléfono cambiado de '{clienteActual.Telefono}' a '{cliente.Telefono}'");
            }
            if (clienteActual.CorreoElectronico != cliente.CorreoElectronico)
            {
                detallesAccion.AppendLine($"Correo electrónico cambiado de '{clienteActual.CorreoElectronico}' a '{cliente.CorreoElectronico}'");
            }
            if (clienteActual.UbicacionId != cliente.UbicacionId)
            {
                detallesAccion.AppendLine($"Ubicación cambiada de '{clienteActual.UbicacionId}' a '{cliente.UbicacionId}'");
            }

            _context.Entry(clienteActual).CurrentValues.SetValues(cliente);

            try
            {
                await _context.SaveChangesAsync();

                var auditoria = new ClienteAudit
                {
                    ClienteId = clienteActual.Id,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido,
                    FechaNacimiento = cliente.FechaNacimiento,
                    Telefono = cliente.Telefono,
                    CorreoElectronico = cliente.CorreoElectronico,
                    UbicacionId = cliente.UbicacionId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "ACTUALIZAR",
                    EmpleadoId = cliente.EmpleadoId,
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = detallesAccion.ToString()
                };

                _context.ClienteAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(id))
                {
                    return NotFound(new { mensaje = $"El cliente con ID {id} ya no existe en el sistema." });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteCliente(int id, int empleadoId)
        {
            var cliente = await _context.Cliente.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }

            var casoController = new CasoController(_context, _emailSender);
            var citaController = new CitaController(_context, _emailSender);
            var expedienteController = new ExpedienteController(_context, _emailSender);
            var citas = await _context.Cita.Where(c => c.ClienteId == id).ToListAsync();

            foreach (var cita in citas)
            {
                await citaController.DeleteCita(cita.Id, empleadoId);
            }
            var casos = await _context.Caso.Where(c => c.ClienteId == id).ToListAsync();
            foreach (var caso in casos)
            {
                await casoController.DeleteCaso(caso.Id, empleadoId);
            }
            var expedientes = await _context.Expediente.Where(c => c.ClienteId == id).ToListAsync();
            foreach (var expediente in expedientes)
            {
                await expedienteController.DeleteExpediente(expediente.Id, empleadoId);
            }
            var recordatorios = await _context.Recordatorio.Where(c => c.ClienteId == id).ToListAsync();
            foreach (var recordatorio in recordatorios)
            {
                await expedienteController.DeleteExpediente(recordatorio.Id, empleadoId);
            }

            var auditoria = new ClienteAudit
            {
                ClienteId = cliente.Id,
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                FechaNacimiento = cliente.FechaNacimiento,
                Telefono = cliente.Telefono,
                CorreoElectronico = cliente.CorreoElectronico,
                UbicacionId = cliente.UbicacionId,
                FechaAccion = DateTime.Now,
                TipoAccion = "ELIMINAR",
                EmpleadoId = cliente.EmpleadoId,
                EmpleadoAccionId = empleadoId,
                DetallesAccion = "Cliente eliminado"
            };

            _context.Cliente.Remove(cliente);
            _context.ClienteAudit.Add(auditoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ClienteExists(int id)
        {
            return _context.Cliente.Any(e => e.Id == id);
        }
    }
}
