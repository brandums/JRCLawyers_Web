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
    public class JuzgadoController : ControllerBase
    {
        private readonly DBaseContext _context;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public JuzgadoController(DBaseContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Juzgado>>> GetJuzgados()
        {
            return await _context.Juzgado.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Juzgado>> GetJuzgado(int id)
        {
            var juzgado = await _context.Juzgado.FindAsync(id);

            if (juzgado == null)
            {
                return NotFound();
            }

            return juzgado;
        }

        [HttpPost]
        public async Task<ActionResult<Juzgado>> PostJuzgado(Juzgado juzgado)
        {
            if (juzgado == null)
            {
                return BadRequest("El objeto 'Juzgado' no puede ser nulo.");
            }

            foreach (var property in juzgado.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(juzgado) as string;

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
                juzgado.Id = 0;
                _context.Juzgado.Add(juzgado);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetJuzgado", new { id = juzgado.Id }, juzgado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error al crear el juzgado: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutJuzgado(int id, Juzgado juzgado)
        {
            if (id != juzgado.Id)
            {
                return BadRequest("El ID del 'Juzgado' no coincide con el ID proporcionado.");
            }

            foreach (var property in juzgado.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(juzgado) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            _context.Entry(juzgado).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JuzgadoExists(id))
                {
                    return NotFound($"El 'Juzgado' con ID {id} no fue encontrado.");
                }
                else
                {
                    return StatusCode(500, "Ocurrió un error al intentar actualizar el 'Juzgado'.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado: {ex.Message}");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJuzgado(int id)
        {
            var juzgado = await _context.Juzgado.FindAsync(id);
            if (juzgado == null)
            {
                return NotFound();
            }

            _context.Juzgado.Remove(juzgado);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool JuzgadoExists(int id)
        {
            return _context.Juzgado.Any(e => e.Id == id);
        }
    }
}
