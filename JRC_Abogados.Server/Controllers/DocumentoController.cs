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
    public class DocumentoController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public DocumentoController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet("documentsByExpedient/{id}")]
        public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos(int id)
        {
            var documentos = await _context.Documento
                                          .Where(e => e.ExpedienteId == id)
                                          .ToListAsync();

            foreach (var documento in documentos)
            {
                documento.Cliente = await _context.Cliente.FindAsync(documento.ClienteId);
            }

            return documentos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Documento>> GetDocumento(int id)
        {
            var documento = await _context.Documento.FindAsync(id);

            if (documento == null)
            {
                return NotFound();
            }

            documento.Cliente = await _context.Cliente.FindAsync(documento.ClienteId);

            return documento;
        }

        [HttpPost, DisableRequestSizeLimit]
        public async Task<ActionResult<Documento>> PostDocumento([FromForm] DocumentoDTO documentoDTO, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("El archivo es requerido.");
            }

            foreach (var property in documentoDTO.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(documentoDTO) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            var documento = new Documento
            {
                Nombre = documentoDTO.Nombre,
                Path = "",
                ExpedienteId = documentoDTO.ExpedienteId,
                Descripcion = documentoDTO.Descripcion,
                FechaInicio = DateTime.Now,
                EmpleadoId = documentoDTO.EmpleadoId,
            };

            var expediente = await _context.Expediente.FindAsync(documentoDTO.ExpedienteId);
            documento.ClienteId = expediente.ClienteId;

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            documento.Path = $"/uploads/{uniqueFileName}";

            try
            {
                _context.Documento.Add(documento);
                await _context.SaveChangesAsync();

                var auditoria = new DocumentoAudit
                {
                    DocumentoId = documento.Id,
                    Nombre = documento.Nombre,
                    Descripcion = documento.Descripcion,
                    Path = documento.Path,
                    FechaInicio = documento.FechaInicio,
                    ExpedienteId = documento.ExpedienteId,
                    ClienteId = documento.ClienteId,
                    EmpleadoId = documento.EmpleadoId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "CREAR",
                    EmpleadoAccionId = documento.EmpleadoId,
                    DetallesAccion = "Documento creado"
                };

                _context.DocumentoAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch
            {
                return StatusCode(500, $"Error al subir el documento.");
            }

            return CreatedAtAction("GetDocumento", new { id = documento.Id }, documento);
        }




        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutDocumento(int id, int empleadoId, DocumentoDTO documentoDTO)
        {
            if (id != documentoDTO.Id)
            {
                return BadRequest("El ID proporcionado no coincide con el documento.");
            }

            var documento = await _context.Documento.FindAsync(id);
            if (documento == null)
            {
                return NotFound("El documento no existe.");
            }

            foreach (var property in documentoDTO.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(documentoDTO) as string;

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
            if (documento.Nombre != documentoDTO.Nombre)
            {
                detallesAccion.AppendLine($"Nombre cambiado de '{documento.Descripcion}' a '{documentoDTO.Descripcion}'");
            }
            if (documento.Descripcion != documentoDTO.Descripcion)
            {
                detallesAccion.AppendLine($"Descripción cambiado de '{documento.Descripcion}' a '{documentoDTO.Descripcion}'");
            }

            documento.Nombre = documentoDTO.Nombre;
            documento.Descripcion = documentoDTO.Descripcion;

            try
            {
                _context.Entry(documento).State = EntityState.Modified;
            }
            catch
            {
                return StatusCode(500, $"Error al actualizar el documento.");
            }

            try
            {
                await _context.SaveChangesAsync();

                var auditoria = new DocumentoAudit
                {
                    DocumentoId = documento.Id,
                    Nombre = documentoDTO.Nombre,
                    Descripcion = documentoDTO.Descripcion,
                    Path = documento.Path,
                    FechaInicio = documento.FechaInicio,
                    ExpedienteId = documento.ExpedienteId,
                    ClienteId = documento.ClienteId,
                    EmpleadoId = documento.EmpleadoId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "ACTUALIZAR",
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = detallesAccion.ToString()
                };

                _context.DocumentoAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DocumentoExists(id))
                {
                    return NotFound("El documento ya no existe o fue modificado por otro usuario.");
                }
                else
                {
                    return StatusCode(500, $"Error al actualizar el documento.");
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteDocumento(int id, int empleadoId)
        {
            var documento = await _context.Documento.FindAsync(id);
            if (documento == null)
            {
                return NotFound();
            }

            var auditoria = new DocumentoAudit
            {
                DocumentoId = documento.Id,
                Nombre = documento.Nombre,
                Descripcion = documento.Descripcion,
                Path = documento.Path,
                FechaInicio = documento.FechaInicio,
                ExpedienteId = documento.ExpedienteId,
                ClienteId = documento.ClienteId,
                EmpleadoId = documento.EmpleadoId,
                FechaAccion = DateTime.Now,
                TipoAccion = "ACTUALIZAR",
                EmpleadoAccionId = empleadoId,
                DetallesAccion = "Documento eliminado"
            };

            _context.Documento.Remove(documento);
            _context.DocumentoAudit.Add(auditoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DocumentoExists(int id)
        {
            return _context.Documento.Any(e => e.Id == id);
        }
    }
}
