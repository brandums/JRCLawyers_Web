using JRC_Abogados.Server.DataBaseContext;
using JRC_Abogados.Server.Models;
using JRC_Abogados.Server.Models.audits;
using JRC_Abogados.Server.Models.EmailHelper;
using JRC_Abogados.Server.ModelsDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace JRC_Abogados.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RecordatorioController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public RecordatorioController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recordatorio>>> GetRecordatorios()
        {
            var recordatorios = await _context.Recordatorio.ToListAsync();

            foreach (var recordatorio in recordatorios)
            {
                recordatorio.Cliente = await _context.Cliente.FindAsync(recordatorio.ClienteId);
            }

            return recordatorios;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Recordatorio>> GetRecordatorio(int id)
        {
            var recordatorio = await _context.Recordatorio.FindAsync(id);

            if (recordatorio == null)
            {
                return NotFound();
            }

            return recordatorio;
        }

        [HttpPost("CheckReminders")]
        public async Task<IActionResult> CheckAndSendReminders()
        {
            var now = DateTime.Now;
            var recordatorios = await _context.Recordatorio
            .Where(r => r.Fecha <= now.Date && r.Hora != "Expirado" && r.Hora.CompareTo(now.ToString("HH:mm")) <= 0)
            .Include(r => r.Cliente)
            .ToListAsync();

            foreach (var recordatorio in recordatorios)
            {
                try
                {
                    string emailSubject = "Recordatorio vencido!";
                    string emailBody = $"El recordatorio '{recordatorio.Titulo}' ha vencido.";

                    var cliente = await _context.Cliente.FindAsync(recordatorio.ClienteId);
                    await _emailSender.SendEmailAsync(cliente.CorreoElectronico, emailSubject, emailBody);

                    recordatorio.Hora = "Expirado";
                    _context.Entry(recordatorio).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email for recordatorio ID {recordatorio.Id}: {ex.Message}");
                }
            }

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<Recordatorio>> PostRecordatorio(RecordatorioDTO recordatorioDTO)
        {
            var cliente = await _context.Cliente.FindAsync(recordatorioDTO.ClienteId);
            if (cliente == null)
            {
                return BadRequest("El cliente especificado no existe.");
            }

            foreach (var property in recordatorioDTO.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(recordatorioDTO) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            string emailSubject = "Nuevo Recordatorio!";
            string emailBody = $@"
                <p>Asunto: Recordatorio Importante de JRC Abogados</p>
                <p>Estimado/a {cliente.Nombre},</p>
                <p>Esperamos que se encuentre bien. Nos dirigimos a usted para recordarle que {recordatorioDTO.Descripcion}. La fecha programada es el {recordatorioDTO.Fecha.Date.ToShortDateString()} a las {recordatorioDTO.Hora}.</p>
                <p>Le recomendamos que revise toda la documentación necesaria y se asegure de que todo esté en orden antes de esa fecha. Si necesita asistencia adicional o tiene alguna pregunta, no dude en ponerse en contacto con nosotros.</p>
                <p>Estamos aquí para ayudarle en todo lo que necesite.</p>
                <p>Gracias por su atención a este asunto.</p>
                <p>Saludos cordiales,</p>
                <p>Equipo JRC Abogados.</p>
            ";

            await _emailSender.SendEmailAsync(cliente.CorreoElectronico, emailSubject, emailBody);

            Recordatorio recordatorio = new Recordatorio();
            recordatorio.Titulo = recordatorioDTO.Titulo;
            recordatorio.Descripcion = recordatorioDTO.Descripcion;
            recordatorio.Fecha = recordatorioDTO.Fecha;
            recordatorio.Hora = recordatorioDTO.Hora;
            recordatorio.ClienteId = recordatorioDTO.ClienteId;
            recordatorio.EmpleadoId = recordatorioDTO.EmpleadoId;

            try
            {
                _context.Recordatorio.Add(recordatorio);
                await _context.SaveChangesAsync();

                var auditoria = new RecordatorioAudit
                {
                    RecordatorioId = recordatorio.Id,
                    Titulo = recordatorio.Titulo,
                    Descripcion = recordatorio.Descripcion,
                    Fecha = recordatorio.Fecha,
                    Hora = recordatorio.Hora,
                    ClienteId = recordatorio.ClienteId,
                    EmpleadoId = recordatorio.EmpleadoId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "CREAR",
                    EmpleadoAccionId = recordatorio.EmpleadoId,
                    DetallesAccion = "Recordatorio creado"
                };

                _context.RecordatorioAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar el recordatorio: {ex.Message}");
            }

            return CreatedAtAction("GetRecordatorio", new { id = recordatorio.Id }, recordatorio);
        }

        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutRecordatorio(int id, int empleadoId, Recordatorio recordatorio)
        {
            if (id != recordatorio.Id)
            {
                return BadRequest("El ID del recordatorio no coincide con el ID proporcionado.");
            }

            var recordatorioExisting = await _context.Recordatorio.FindAsync(id);
            if (recordatorioExisting == null)
            {
                return NotFound("El recordatorio especificado no existe.");
            }

            foreach (var property in recordatorio.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(recordatorio) as string;

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

            if (recordatorioExisting.Titulo != recordatorio.Titulo)
            {
                detallesAccion.AppendLine($"Titulo cambiado de '{recordatorioExisting.Titulo}' a '{recordatorio.Titulo}'");
            }
            if (recordatorioExisting.Descripcion != recordatorio.Descripcion)
            {
                detallesAccion.AppendLine($"Descripción cambiada de '{recordatorioExisting.Descripcion}' a '{recordatorio.Descripcion}'");
            }
            if (recordatorioExisting.Fecha != recordatorio.Fecha)
            {
                detallesAccion.AppendLine($"Fecha cambiada de '{recordatorioExisting.Fecha}' a '{recordatorio.Fecha}'");
            }
            if (recordatorioExisting.Hora != recordatorio.Hora)
            {
                detallesAccion.AppendLine($"Hora cambiado de '{recordatorioExisting.Hora}' a '{recordatorio.Hora}'");
            }
            if (recordatorioExisting.ClienteId != recordatorio.ClienteId)
            {
                var clienteActual = await _context.Cliente.FindAsync(recordatorioExisting.ClienteId);
                var cliente = await _context.Estado.FindAsync(recordatorio.ClienteId);
                detallesAccion.AppendLine($"Cliente cambiado de '{clienteActual.Nombre}' a '{cliente.Nombre}'");
            }

            try
            {
                _context.Entry(recordatorioExisting).CurrentValues.SetValues(recordatorio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar el recordatorio: {ex.Message}");
            }


            try
            {
                await _context.SaveChangesAsync();

                var auditoria = new RecordatorioAudit
                {
                    RecordatorioId = recordatorio.Id,
                    Titulo = recordatorio.Titulo,
                    Descripcion = recordatorio.Descripcion,
                    Fecha = recordatorio.Fecha,
                    Hora = recordatorio.Hora,
                    ClienteId = recordatorio.ClienteId,
                    EmpleadoId = recordatorio.EmpleadoId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "ACTUALIZAR",
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = detallesAccion.ToString()
                };

                _context.RecordatorioAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RecordatorioExists(id))
                {
                    return StatusCode(500, $"Error al actualizar el recordatorio.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteRecordatorio(int id, int empleadoId)
        {
            var recordatorio = await _context.Recordatorio.FindAsync(id);
            if (recordatorio == null)
            {
                return NotFound();
            }

            var auditoria = new RecordatorioAudit
            {
                RecordatorioId = recordatorio.Id,
                Titulo = recordatorio.Titulo,
                Descripcion = recordatorio.Descripcion,
                Fecha = recordatorio.Fecha,
                Hora = recordatorio.Hora,
                ClienteId = recordatorio.ClienteId,
                EmpleadoId = recordatorio.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "ELIMINAR",
                EmpleadoAccionId = empleadoId,
                DetallesAccion = "Recordatorio eliminado"
            };

            _context.Recordatorio.Remove(recordatorio);
            _context.RecordatorioAudit.Add(auditoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RecordatorioExists(int id)
        {
            return _context.Recordatorio.Any(e => e.Id == id);
        }
    }
}
