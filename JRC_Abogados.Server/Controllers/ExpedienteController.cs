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
    public class ExpedienteController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public ExpedienteController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Expediente>>> GetExpedientes()
        {
            var expedientes = await _context.Expediente.ToListAsync();

            foreach (var expediente in expedientes)
            {
                if (expediente.CasoId != null)
                {
                    expediente.Caso = await _context.Caso.FindAsync(expediente.CasoId);
                    expediente.Caso.TipoCaso = await _context.TipoCaso.FindAsync(expediente.Caso.TipoCasoId);
                    expediente.Caso.Juzgado = await _context.Juzgado.FindAsync(expediente.Caso.JuzgadoId);
                    expediente.Caso.Ubicacion = await _context.Ubicacion.FindAsync(expediente.Caso.UbicacionId);
                    expediente.Caso.Cliente = await _context.Cliente.FindAsync(expediente.ClienteId);
                }

                expediente.TipoExpediente = await _context.TipoExpediente.FindAsync(expediente.TipoExpedienteId);
                expediente.Cliente = await _context.Cliente.FindAsync(expediente.ClienteId);
            }

            return expedientes;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Expediente>> GetExpediente(int id)
        {
            var expediente = await _context.Expediente.FindAsync(id);

            if (expediente == null)
            {
                return NotFound();
            }

            if (expediente.CasoId != null)
            {
                expediente.Caso = await _context.Caso.FindAsync(expediente.CasoId);
            }

            expediente.Cliente = await _context.Cliente.FindAsync(expediente.ClienteId);

            return expediente;
        }

        [HttpGet("sendPDF/{id}/{mail}")]
        public async Task<ActionResult<Documento>> SendPDF(int id, string mail)
        {
            var documentos = await _context.Documento.Where(c => c.ExpedienteId == id).ToListAsync();

            if (documentos == null || !documentos.Any())
            {
                return NotFound();
            }

            string emailSubject = "Documento JRC Abogados.";
            string emailBody = $"<p>Por favor, haga clic en el siguiente enlace para ver el documento:</p>";

            foreach (var documento in documentos)
            {
                emailBody += $"<p>{documento.Nombre}: <a href='https://jrcweb-001-site1.atempurl.com{documento.Path}'>Ver documento</a></p>";
            }

            await _emailSender.SendEmailAsync(mail, emailSubject, emailBody);

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<Expediente>> PostExpediente(ExpedienteDTO expedienteDTO)
        {
            if (expedienteDTO.TipoExpedienteId == 1)
            {
                var existingExpediente = await _context.Expediente.FirstOrDefaultAsync(c => c.ClienteId == expedienteDTO.ClienteId && c.TipoExpedienteId == 1);

                if (existingExpediente != null)
                {
                    return Conflict(new { message = "El expediente ya está registrado." });
                }
            }

            foreach (var property in expedienteDTO.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(expedienteDTO) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            var expediente = new Expediente();
            expediente.Nombre = expedienteDTO.Nombre;
            expediente.TipoExpedienteId = expedienteDTO.TipoExpedienteId;
            expediente.ClienteId = expedienteDTO.ClienteId;
            expediente.CasoId = expedienteDTO.CasoId;
            expediente.FechaInicio = DateTime.Now;
            expediente.EmpleadoId = expedienteDTO.EmpleadoId;

            try
            {
                _context.Expediente.Add(expediente);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar los cambios en la base de datos." });
            }

            var auditoria = new ExpedienteAudit
            {
                ExpedienteId = expediente.Id,
                Nombre = expediente.Nombre,
                TipoExpedienteId = expediente.TipoExpedienteId,
                FechaInicio = expediente.FechaInicio,
                ClienteId = expediente.ClienteId,
                CasoId = expediente.CasoId,
                EmpleadoId = expediente.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "CREAR",
                EmpleadoAccionId = expediente.EmpleadoId,
                DetallesAccion = "Expediente creado"
            };


            try
            {
                _context.ExpedienteAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar los cambios en la base de datos." });
            }

            return CreatedAtAction("GetExpediente", new { id = expediente.Id }, expediente);
        }


        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutExpediente(int id, int empleadoId, ExpedienteDTO expedienteDTO)
        {
            if (id != expedienteDTO.Id)
            {
                return BadRequest(new { message = "El ID del expediente no coincide con el ID del DTO." });
            }

            var expediente = await _context.Expediente.FindAsync(id);
            if (expediente == null)
            {
                return NotFound(new { message = "El expediente no fue encontrado." });
            }

            foreach (var property in expedienteDTO.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(expedienteDTO) as string;

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

            if (expediente.Nombre != expedienteDTO.Nombre)
            {
                detallesAccion.AppendLine($"Nombre cambiado de '{expediente.Nombre}' a '{expedienteDTO.Nombre}'");
            }
            if (expediente.TipoExpedienteId != expedienteDTO.TipoExpedienteId)
            {
                expediente.TipoExpediente = await _context.TipoExpediente.FindAsync(expediente.TipoExpedienteId);
                var tipoExpediente = await _context.TipoExpediente.FindAsync(expedienteDTO.TipoExpedienteId);
                detallesAccion.AppendLine($"Tipo de expediente cambiado de '{expediente.TipoExpedienteId}' a '{tipoExpediente}'");
            }
            if (expediente.TipoExpedienteId == 1 && expediente.CasoId != expedienteDTO.CasoId)
            {
                var casoActual = await _context.Caso.FindAsync(expediente.CasoId);
                var caso = await _context.Caso.FindAsync(expedienteDTO.CasoId);
                casoActual.Juzgado = await _context.Juzgado.FindAsync(casoActual.JuzgadoId);
                caso.Juzgado = await _context.Juzgado.FindAsync(caso.JuzgadoId);
                detallesAccion.AppendLine($"Caso cambiado del numero de expediente '{casoActual.Juzgado.NumeroExpediente}' a '{caso.Juzgado.NumeroExpediente}'");
            }

            expediente.Nombre = expedienteDTO.Nombre;
            expediente.TipoExpedienteId = expedienteDTO.TipoExpedienteId;
            expediente.ClienteId = expedienteDTO.ClienteId;
            expediente.CasoId = expedienteDTO.CasoId;

            try
            {
                _context.Entry(expediente).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocurrió un error al actualizar el recordatorio." });
            }


            try
            {
                await _context.SaveChangesAsync();

                var auditoria = new ExpedienteAudit
                {
                    ExpedienteId = expediente.Id,
                    Nombre = expediente.Nombre,
                    TipoExpedienteId = expediente.TipoExpedienteId,
                    FechaInicio = expediente.FechaInicio,
                    ClienteId = expediente.ClienteId,
                    CasoId = expediente.CasoId,
                    EmpleadoId = expediente.EmpleadoId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "ACTUALIZAR",
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = detallesAccion.ToString()
                };

                _context.ExpedienteAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExpedienteExists(id))
                {
                    return NotFound(new { message = "El expediente no existe." });
                }
                else
                {
                    return StatusCode(500, new { message = "Ocurrió un error inesperado." });
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteExpediente(int id, int empleadoId)
        {
            var expediente = await _context.Expediente.FindAsync(id);
            if (expediente == null)
            {
                return NotFound();
            }

            var documentos = await _context.Documento.Where(c => c.ExpedienteId == id).ToListAsync();
            var documentoController = new DocumentoController(_context, _emailSender);

            foreach (var documento in documentos)
            {
                await documentoController.DeleteDocumento(documento.Id, empleadoId);
            }

            var auditoria = new ExpedienteAudit
            {
                ExpedienteId = expediente.Id,
                Nombre = expediente.Nombre,
                TipoExpedienteId = expediente.TipoExpedienteId,
                FechaInicio = expediente.FechaInicio,
                ClienteId = expediente.ClienteId,
                CasoId = expediente.CasoId,
                EmpleadoId = expediente.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "ELIMINAR",
                EmpleadoAccionId = empleadoId,
                DetallesAccion = "Expediente eliminado"
            };

            _context.Expediente.Remove(expediente);
            _context.ExpedienteAudit.Add(auditoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ExpedienteExists(int id)
        {
            return _context.Expediente.Any(e => e.Id == id);
        }
    }
}
