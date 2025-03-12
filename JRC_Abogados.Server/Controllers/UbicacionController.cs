using JRC_Abogados.Server.DataBaseContext;
using JRC_Abogados.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JRC_Abogados.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UbicacionController : ControllerBase
    {
        private readonly DBaseContext _context;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public UbicacionController(DBaseContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ubicacion>>> GetUbicaciones()
        {
            return await _context.Ubicacion.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Ubicacion>> GetUbicacion(int id)
        {
            var ubicacion = await _context.Ubicacion.FindAsync(id);

            if (ubicacion == null)
            {
                return NotFound();
            }

            return ubicacion;
        }

        [HttpPost]
        public async Task<ActionResult<Ubicacion>> PostUbicacion(Ubicacion ubicacion)
        {
            ubicacion.Id = 0;
            foreach (var property in ubicacion.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(ubicacion) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            _context.Ubicacion.Add(ubicacion);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar la ubicación: {ex.Message}");
            }

            return CreatedAtAction("GetUbicacion", new { id = ubicacion.Id }, ubicacion);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUbicacion(int id, Ubicacion ubicacion)
        {
            if (id != ubicacion.Id)
            {
                return BadRequest();
            }

            foreach (var property in ubicacion.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(ubicacion) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            _context.Entry(ubicacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UbicacionExists(id))
                {
                    return NotFound("No se encontró la ubicación con el ID especificado.");
                }
                else
                {
                    return StatusCode(409, "Ocurrió un error de concurrencia al intentar actualizar la ubicación.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar la ubicación: {ex.Message}");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUbicacion(int id)
        {
            var ubicacion = await _context.Ubicacion.FindAsync(id);
            if (ubicacion == null)
            {
                return NotFound();
            }

            _context.Ubicacion.Remove(ubicacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UbicacionExists(int id)
        {
            return _context.Ubicacion.Any(e => e.Id == id);
        }
    }
}
