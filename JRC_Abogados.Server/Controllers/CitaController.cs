using JRC_Abogados.Server.DataBaseContext;
using JRC_Abogados.Server.Models;
using JRC_Abogados.Server.Models.audits;
using JRC_Abogados.Server.Models.EmailHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace JRC_Abogados.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CitaController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public CitaController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cita>>> GetCitas()
        {
            var citas = await _context.Cita.ToListAsync();

            foreach (var cita in citas)
            {
                cita.Ubicacion = await _context.Ubicacion.FindAsync(cita.UbicacionId);
                cita.Estado = await _context.Estado.FindAsync(cita.EstadoId);
                cita.Cliente = await _context.Cliente.FindAsync(cita.ClienteId);
            }

            return citas;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Cita>> GetCita(int id)
        {
            var cita = await _context.Cita.FindAsync(id);
#pragma warning disable CS8602 // Desreferencia de una referencia posiblemente NULL.
            cita.Ubicacion = await _context.Ubicacion.FindAsync(cita.UbicacionId);
#pragma warning restore CS8602 // Desreferencia de una referencia posiblemente NULL.
            cita.Estado = await _context.Estado.FindAsync(cita.EstadoId);
            cita.Cliente = await _context.Cliente.FindAsync(cita.ClienteId);

            if (cita == null)
            {
                return NotFound();
            }

            return cita;
        }


        [HttpPost]
        public async Task<ActionResult<Cita>> PostCita(Cita cita)
        {
            cita.Cliente = await _context.Cliente.FindAsync(cita.ClienteId);
            if (cita.Cliente == null)
            {
                return BadRequest("El cliente especificado no existe.");
            }

            foreach (var property in cita.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(cita) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            cita.Estado = await _context.Estado.FindAsync(cita.EstadoId);

            try
            {
                _context.Cita.Add(cita);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error al crear la cita: {ex.Message}");
            }

            string confirmUrl = $"https://jrcweb-001-site1.atempurl.com/api/Cita/confirmar/{cita.Id}";
            string cancelUrl = $"https://jrcweb-001-site1.atempurl.com/api/Cita/cancelar/{cita.Id}";

            string emailSubject = "Confirmación de Cita";
            string emailBody = $@"
                <p>Estimado/a {cita.Cliente.Nombre},</p>
                <p>Esperamos que se encuentre bien.</p>
                <p>Le escribimos para confirmar su cita programada con nosotros para el día {cita.FechaInicio.Date.ToShortDateString()} a las {cita.Hora}. </p>
                <p>Para confirmar su cita, haga clic en el siguiente botón:</p>
                <a href='{confirmUrl}' style='padding: 10px 20px; color: white; background-color: blue; text-decoration: none;'>Confirmar</a>
                <p>Si desea cancelar la cita, puede hacerlo haciendo clic en el siguiente botón:</p>
                <a href='{cancelUrl}' style='padding: 10px 20px; color: white; background-color: red; text-decoration: none;'>Cancelar</a>
                <p>En caso de necesitar reprogramar su cita, le agradeceríamos que nos lo notificara lo antes posible para poder reprogramarla a una fecha y hora más conveniente para usted.</p>
                <p>Quedamos atentos a su respuesta.</p>
                <p>Gracias por su atención.</p>
                <p>Saludos cordiales,</p>
                <p>Equipo JRC Abogados.</p>
            ";

            var auditoria = new CitaAudit
            {
                CitaId = cita.Id,
                TipoCita = cita.TipoCita,
                FechaInicio = cita.FechaInicio,
                Hora = cita.Hora,
                UbicacionId = cita.UbicacionId,
                EstadoId = cita.EstadoId,
                Notas = cita.Notas,
                ClienteId = cita.ClienteId,
                EmpleadoId = cita.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "CREAR",
                EmpleadoAccionId = cita.EmpleadoId,
                DetallesAccion = "Cita creada"
            };

            try
            {
                _context.CitaAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error al crear la cita: {ex.Message}");
            }

            bool resultado = await _emailSender.SendEmailAsync(cita.Cliente.CorreoElectronico, emailSubject, emailBody);

            if (resultado)
            {
                return CreatedAtAction("GetCita", new { id = cita.Id }, cita);
            }

            return BadRequest();
        }

        [AllowAnonymous]
        [HttpGet("confirmar/{id}")]
        public async Task<IActionResult> ConfirmarCita(int id)
        {
            var cita = await _context.Cita.FindAsync(id);
            if (cita == null)
            {
                return NotFound(new { message = "Cita no encontrada." });
            }

            var cliente = await _context.Cliente.FindAsync(cita.ClienteId);

            string emailSubject = "Estado de su Cita";
            string emailBody = $@"
                <p>Estimado/a {cliente.Nombre},</p>
                <p>Gracias por su respuesta. Le informamos que su cita para el día {cita.FechaInicio.Date.ToShortDateString()} a las {cita.FechaInicio.ToShortTimeString()} ha sido confirmada.</p>
                <p>Atentamente,</p>
                <p>Equipo JRC Abogados.</p>
            ";

            cita.EstadoId = 2;
            await _context.SaveChangesAsync();

            return Redirect("https://jrcweb-001-site1.atempurl.com/pagina-confirmacion/1");
        }

        [AllowAnonymous]
        [HttpGet("cancelar/{id}")]
        public async Task<IActionResult> CancelarCita(int id)
        {
            var cita = await _context.Cita.FindAsync(id);
            if (cita == null)
            {
                return NotFound(new { message = "Cita no encontrada." });
            }

            cita.EstadoId = 3;
            await _context.SaveChangesAsync();

            return Redirect("https://jrcweb-001-site1.atempurl.com/pagina-confirmacion/1");
        }


        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutCita(int id, int empleadoId, Cita cita)
        {
            if (id != cita.Id)
            {
                return BadRequest("El ID de la cita no coincide con el proporcionado.");
            }

            var citaActual = await _context.Cita.FindAsync(id);
            if (citaActual == null)
            {
                return NotFound($"No se encontró una cita con el ID {id}.");
            }

            foreach (var property in cita.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(cita) as string;

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

            if (citaActual.TipoCita != cita.TipoCita)
            {
                detallesAccion.AppendLine($"Tipo de cita cambiado de '{citaActual.TipoCita}' a '{cita.TipoCita}'");
            }
            if (citaActual.FechaInicio != cita.FechaInicio)
            {
                detallesAccion.AppendLine($"Fecha de inicio cambiado de '{citaActual.FechaInicio}' a '{cita.FechaInicio}'");
            }
            if (citaActual.Hora != cita.Hora)
            {
                detallesAccion.AppendLine($"Hora cambiada de '{citaActual.Hora}' a '{cita.Hora}'");
            }
            if (citaActual.UbicacionId != cita.UbicacionId)
            {
                citaActual.Ubicacion = await _context.Ubicacion.FindAsync(citaActual.UbicacionId);
                cita.Ubicacion = await _context.Ubicacion.FindAsync(cita.UbicacionId);
                if (citaActual.Ubicacion.Direccion != cita.Ubicacion.Direccion)
                {
                    detallesAccion.AppendLine($"Direccion cambiada de '{citaActual.Ubicacion.Direccion}' a '{cita.Ubicacion.Direccion}'");
                }
                if (citaActual.Ubicacion.Estado != cita.Ubicacion.Estado)
                {
                    detallesAccion.AppendLine($"Estado cambiada de '{citaActual.Ubicacion.Estado}' a '{cita.Ubicacion.Estado}'");
                }
                if (citaActual.Ubicacion.Ciudad != cita.Ubicacion.Ciudad)
                {
                    detallesAccion.AppendLine($"Ciudad cambiada de '{citaActual.Ubicacion.Ciudad}' a '{cita.Ubicacion.Ciudad}'");
                }
                if (citaActual.Ubicacion.CodigoPostal != cita.Ubicacion.CodigoPostal)
                {
                    detallesAccion.AppendLine($"Codigo Postal cambiada de '{citaActual.Ubicacion.CodigoPostal}' a '{cita.Ubicacion.CodigoPostal}'");
                }
            }
            if (citaActual.EstadoId != cita.EstadoId)
            {
                var estadoActual = await _context.Estado.FindAsync(citaActual.EstadoId);
                var estado = await _context.Estado.FindAsync(cita.EstadoId);
                string emailSubject = "Actualización de la Cita!";
                string emailBody = $"El estado del caso es {estado.Nombre}.!";
                await _emailSender.SendEmailAsync(cita.Cliente.CorreoElectronico, emailSubject, emailBody);
                detallesAccion.AppendLine($"Estado cambiado de '{estadoActual.Nombre}' a '{estado.Nombre}'");
            }
            if (citaActual.Notas != cita.Notas)
            {
                detallesAccion.AppendLine($"Descripción cambiado de '{citaActual.Notas}' a '{cita.Notas}'");
            }
            if (citaActual.ClienteId != cita.ClienteId)
            {
                var clienteActual = await _context.Cliente.FindAsync(citaActual.ClienteId);
                var cliente = await _context.Estado.FindAsync(cita.ClienteId);
                detallesAccion.AppendLine($"Cliente cambiado de '{clienteActual.Nombre}' a '{cliente.Nombre}'");
            }

            cita.Cliente = null;
            cita.Estado = null;
            cita.Ubicacion = null;

            try
            {
                _context.Entry(citaActual).CurrentValues.SetValues(cita);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error de concurrencia al actualizar la cita.");
            }


            try
            {
                await _context.SaveChangesAsync();

                var auditoria = new CitaAudit
                {
                    CitaId = citaActual.Id,
                    TipoCita = cita.TipoCita,
                    FechaInicio = cita.FechaInicio,
                    Hora = cita.Hora,
                    UbicacionId = cita.UbicacionId,
                    EstadoId = cita.EstadoId,
                    Notas = cita.Notas,
                    ClienteId = cita.ClienteId,
                    EmpleadoId = cita.EmpleadoId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "ACTUALIZAR",
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = detallesAccion.ToString()
                };

                _context.CitaAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CitaExists(id))
                {
                    return StatusCode(500, "Error de concurrencia al actualizar la cita.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteCita(int id, int empleadoId)
        {
            var cita = await _context.Cita.FindAsync(id);
            if (cita == null)
            {
                return NotFound();
            }

            var auditoria = new CitaAudit
            {
                CitaId = cita.Id,
                TipoCita = cita.TipoCita,
                FechaInicio = cita.FechaInicio,
                Hora = cita.Hora,
                UbicacionId = cita.UbicacionId,
                EstadoId = cita.EstadoId,
                Notas = cita.Notas,
                ClienteId = cita.ClienteId,
                EmpleadoId = cita.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "ELIMINAR",
                EmpleadoAccionId = empleadoId,
                DetallesAccion = "Cita eliminada"
            };

            _context.Cita.Remove(cita);
            _context.CitaAudit.Add(auditoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CitaExists(int id)
        {
            return _context.Cita.Any(e => e.Id == id);
        }
    }
}
