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
    public class CasoController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public CasoController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Caso>>> GetCasos()
        {
            var casos = await _context.Caso.ToListAsync();

            foreach (var caso in casos)
            {
                caso.TipoCaso = await _context.TipoCaso.FindAsync(caso.TipoCasoId);
                caso.Juzgado = await _context.Juzgado.FindAsync(caso.JuzgadoId);
                caso.Ubicacion = await _context.Ubicacion.FindAsync(caso.UbicacionId);
                caso.Estado = await _context.Estado.FindAsync(caso.EstadoId);
                caso.Cliente = await _context.Cliente.FindAsync(caso.ClienteId);
            }

            return casos;
        }

        [HttpGet("expedientesByClient/{ClienteId}/{casoId}")]
        public async Task<ActionResult<IEnumerable<Caso>>> GetCasesByClient(int clienteId, int casoId)
        {
            var expedienteCasoIds = await _context.Expediente
                                          .Select(e => e.CasoId)
                                          .ToListAsync();

            var casos = await _context.Caso
                                          .Where(c => c.ClienteId == clienteId && !expedienteCasoIds.Contains(c.Id))
                                          .ToListAsync();

            if (casoId != 0)
            {
                var caso = await _context.Caso.FindAsync(casoId);
                casos.Add(caso);
            }

            foreach (var caso in casos)
            {
                caso.TipoCaso = await _context.TipoCaso.FindAsync(caso.TipoCasoId);
                caso.Juzgado = await _context.Juzgado.FindAsync(caso.JuzgadoId);
                caso.Ubicacion = await _context.Ubicacion.FindAsync(caso.UbicacionId);
                caso.Estado = await _context.Estado.FindAsync(caso.EstadoId);
                caso.Cliente = await _context.Cliente.FindAsync(caso.ClienteId);
            }

            return casos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Caso>> GetCaso(int id)
        {
            var caso = await _context.Caso.FindAsync(id);
#pragma warning disable CS8602 // Desreferencia de una referencia posiblemente NULL.
            caso.TipoCaso = await _context.TipoCaso.FindAsync(caso.UbicacionId);
#pragma warning restore CS8602 // Desreferencia de una referencia posiblemente NULL.
            caso.Juzgado = await _context.Juzgado.FindAsync(caso.JuzgadoId);
            caso.Ubicacion = await _context.Ubicacion.FindAsync(caso.UbicacionId);
            caso.Estado = await _context.Estado.FindAsync(caso.EstadoId);
            caso.Cliente = await _context.Cliente.FindAsync(caso.ClienteId);

            if (caso == null)
            {
                return NotFound();
            }

            return caso;
        }

        [HttpPost]
        public async Task<ActionResult<Caso>> PostCaso(Caso caso)
        {
            var existingCaso = await _context.Caso.FirstOrDefaultAsync(c => c.Juzgado.NumeroExpediente == caso.Juzgado.NumeroExpediente);

            if (existingCaso != null)
            {
                return Conflict(new { message = "El Caso ya está registrado con el mismo número de expediente." });
            }

            foreach (var property in caso.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(caso) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            caso.Cliente = await _context.Cliente.FindAsync(caso.ClienteId);
            caso.TipoCaso = await _context.TipoCaso.FindAsync(caso.TipoCasoId);
            caso.Estado = await _context.Estado.FindAsync(caso.EstadoId);
            SendCreateEmail(caso);

            caso.TipoCaso = null;
            caso.Juzgado = null;
            caso.Ubicacion = null;
            caso.Estado = null;
            caso.Cliente = null;

            _context.Caso.Add(caso);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al crear el caso: {ex.Message}" });
            }

            var auditoria = new CasoAudit
            {
                CasoId = caso.Id,
                TipoCasoId = caso.TipoCasoId,
                JuzgadoId = caso.JuzgadoId,
                UbicacionId = caso.UbicacionId,
                Descripcion = caso.Descripcion,
                FechaInicio = caso.FechaInicio,
                FechaTermino = caso.FechaTermino,
                EstadoId = caso.EstadoId,
                ClienteId = caso.ClienteId,
                EmpleadoId = caso.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "CREAR",
                EmpleadoAccionId = caso.EmpleadoId,
                DetallesAccion = "Caso creado"
            };

            _context.CasoAudit.Add(auditoria);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCaso", new { id = caso.Id }, caso);
        }

        [HttpGet("sendCreationEmail")]
        private async void SendCreateEmail(Caso caso)
        {
            string emailSubject = "Nuevo Caso!";
            string emailBody = $"<p> Estimado/a {caso.Cliente.Nombre},</p>" +
                    $"<p>Nos ponemos en contacto para informarle sobre el estado actual de su caso en JRC Abogados.A continuación, encontrará los detalles de su expediente:</p>" +
                    $"<p><strong>Rol:</strong> {caso.TipoCaso.Nombre}</p>" +
                    $"<p><strong>Juzgado:</strong> {caso.Juzgado.Nombre}</p>" +
                    $"<p><strong>Número de Expediente:</strong> {caso.Juzgado.NumeroExpediente}</p>" +
                    $"<p><strong>Estado Actual del Caso:</strong> {caso.Estado.Nombre}</p>" +
                    $"<p>Nos comprometemos a mantenerlo/a informado/a sobre cualquier novedad o cambio en su caso. Si tiene alguna pregunta o necesita más información, no dude en ponerse en contacto con nosotros.</p>" +
                    $"<p>Gracias por confiar en JRC Abogados.</p>" +
                    $"<p>Atentamente,<br> Equipo de JRC Abogados. </p>";

            await _emailSender.SendEmailAsync(caso.Cliente.CorreoElectronico, emailSubject, emailBody);
        }

        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutCaso(int id, int empleadoId, Caso caso)
        {
            if (id != caso.Id)
            {
                return BadRequest(new { message = "El ID del caso proporcionado no coincide con el ID del caso a actualizar." });
            }

            var casoActual = await _context.Caso.FindAsync(id);
            if (casoActual == null)
            {
                return NotFound(new { message = "El caso no se encontró." });
            }

            foreach (var property in caso.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(caso) as string;

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

            if (casoActual.TipoCasoId != caso.TipoCasoId)
            {
                var tipoCasoActual = await _context.TipoCaso.FindAsync(casoActual.TipoCasoId);
                var tipoCaso = await _context.TipoCaso.FindAsync(caso.TipoCasoId);
                detallesAccion.AppendLine($"Tipo de caso cambiado de '{tipoCasoActual.Nombre}' a '{tipoCaso.Nombre}'");
            }
            if (casoActual.JuzgadoId != caso.JuzgadoId)
            {
                casoActual.Juzgado = await _context.Juzgado.FindAsync(casoActual.JuzgadoId);
                caso.Juzgado = await _context.Juzgado.FindAsync(caso.JuzgadoId);
                if (casoActual.Juzgado.Nombre != caso.Juzgado.Nombre)
                {
                    detallesAccion.AppendLine($"Nombre de Juzgado cambiada de '{casoActual.Juzgado.Nombre}' a '{caso.Juzgado.Nombre}'");
                }
                if (casoActual.Juzgado.NumeroExpediente != caso.Juzgado.NumeroExpediente)
                {
                    detallesAccion.AppendLine($"Numero de Expediente cambiado de '{casoActual.Juzgado.NumeroExpediente}' a '{caso.Juzgado.NumeroExpediente}'");
                }
            }
            if (casoActual.UbicacionId != caso.UbicacionId)
            {
                casoActual.Ubicacion = await _context.Ubicacion.FindAsync(casoActual.UbicacionId);
                caso.Ubicacion = await _context.Ubicacion.FindAsync(caso.UbicacionId);
                if (casoActual.Ubicacion.Direccion != caso.Ubicacion.Direccion)
                {
                    detallesAccion.AppendLine($"Direccion cambiada de '{casoActual.Ubicacion.Direccion}' a '{caso.Ubicacion.Direccion}'");
                }
                if (casoActual.Ubicacion.Estado != caso.Ubicacion.Estado)
                {
                    detallesAccion.AppendLine($"Estado cambiada de '{casoActual.Ubicacion.Estado}' a '{caso.Ubicacion.Estado}'");
                }
                if (casoActual.Ubicacion.Ciudad != caso.Ubicacion.Ciudad)
                {
                    detallesAccion.AppendLine($"Ciudad cambiada de '{casoActual.Ubicacion.Ciudad}' a '{caso.Ubicacion.Ciudad}'");
                }
                if (casoActual.Ubicacion.CodigoPostal != caso.Ubicacion.CodigoPostal)
                {
                    detallesAccion.AppendLine($"Codigo Postal cambiada de '{casoActual.Ubicacion.CodigoPostal}' a '{caso.Ubicacion.CodigoPostal}'");
                }
            }
            if (casoActual.Descripcion != caso.Descripcion)
            {
                detallesAccion.AppendLine($"Descripción cambiado de '{casoActual.Descripcion}' a '{caso.Descripcion}'");
            }
            if (casoActual.EstadoId != caso.EstadoId)
            {
                var estadoActual = await _context.Estado.FindAsync(casoActual.EstadoId);
                var estado = await _context.Estado.FindAsync(caso.EstadoId);
                caso.Cliente = await _context.Cliente.FindAsync(caso.ClienteId);
                caso.Estado = estado;
                SendStatusEmail(caso);
                detallesAccion.AppendLine($"Estado cambiado de '{estadoActual.Nombre}' a '{estado.Nombre}'");
            }
            if (casoActual.ClienteId != caso.ClienteId)
            {
                var clienteActual = await _context.Cliente.FindAsync(casoActual.ClienteId);
                var cliente = await _context.Estado.FindAsync(caso.ClienteId);
                detallesAccion.AppendLine($"Cliente cambiado de '{clienteActual.Nombre}' a '{cliente.Nombre}'");
            }

            caso.Cliente = null;
            caso.Estado = null;
            caso.Juzgado = null;
            caso.TipoCaso = null;
            caso.Ubicacion = null;
            _context.Entry(casoActual).CurrentValues.SetValues(caso);

            try
            {
                await _context.SaveChangesAsync();

                var auditoria = new CasoAudit
                {
                    CasoId = casoActual.Id,
                    TipoCasoId = caso.TipoCasoId,
                    JuzgadoId = caso.JuzgadoId,
                    UbicacionId = caso.UbicacionId,
                    Descripcion = caso.Descripcion,
                    FechaInicio = caso.FechaInicio,
                    FechaTermino = caso.FechaTermino,
                    EstadoId = caso.EstadoId,
                    ClienteId = caso.ClienteId,
                    EmpleadoId = caso.EmpleadoId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "ACTUALIZAR",
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = detallesAccion.ToString()
                };

                _context.CasoAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CasoExists(id))
                {
                    return NotFound(new { message = "El caso no existe para la actualización." });
                }
                else
                {
                    return StatusCode(409, new { message = "Ocurrió un error de concurrencia al actualizar el caso." });
                }
            }

            return Ok(new { message = "Caso actualizado correctamente." });
        }

        [HttpGet("sendStatusEmail")]
        private async void SendStatusEmail(Caso caso)
        {
            string emailSubject = " Actualización de Estado de su Caso";
            string emailBody = $"<p> Estimado/a {caso.Cliente.Nombre},</p>" +
                $"<p>Le informamos que el estado de su caso ha sido actualizado a {caso.Estado.Nombre}</p>" +
                $"<p>Si tiene alguna pregunta, estamos a su disposición.</p>" +
                $"<p>Atentamente,<br> Equipo de JRC Abogados. </p>";

            await _emailSender.SendEmailAsync(caso.Cliente.CorreoElectronico, emailSubject, emailBody);
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteCaso(int id, int empleadoId)
        {
            var caso = await _context.Caso.FindAsync(id);
            if (caso == null)
            {
                return NotFound();
            }

            var auditoria = new CasoAudit
            {
                CasoId = caso.Id,
                TipoCasoId = caso.TipoCasoId,
                JuzgadoId = caso.JuzgadoId,
                UbicacionId = caso.UbicacionId,
                Descripcion = caso.Descripcion,
                FechaInicio = caso.FechaInicio,
                FechaTermino = caso.FechaTermino,
                EstadoId = caso.EstadoId,
                ClienteId = caso.ClienteId,
                EmpleadoId = caso.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "ELIMINAR",
                EmpleadoAccionId = empleadoId,
                DetallesAccion = "Caso eliminado"
            };

            _context.Caso.Remove(caso);
            _context.CasoAudit.Add(auditoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CasoExists(int id)
        {
            return _context.Caso.Any(e => e.Id == id);
        }
    }
}
