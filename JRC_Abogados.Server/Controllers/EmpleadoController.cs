using JRC_Abogados.Server.DataBaseContext;
using JRC_Abogados.Server.Models;
using JRC_Abogados.Server.Models.audits;
using JRC_Abogados.Server.Models.EmailHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace JRC_Abogados.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmpleadoController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        private readonly PasswordHasher<Empleado> _passwordHasher;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public EmpleadoController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
            _passwordHasher = new PasswordHasher<Empleado>();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empleado>>> GetUsuarios()
        {
            var empleados = await _context.Empleado.ToListAsync();

            foreach (var empleado in empleados)
            {
                empleado.Rol = await _context.Rol.FindAsync(empleado.RolId);
            }

            return empleados;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Empleado>> GetUsuario(int id)
        {
            var empleado = await _context.Empleado.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

            empleado.Rol = await _context.Rol.FindAsync(empleado.RolId);

            return empleado;
        }

        [HttpPost("{empleadoId}")]
        public async Task<ActionResult<Empleado>> PostUsuario(int empleadoId, Empleado usuario)
        {
            var existingEmpleado = await _context.Empleado.FirstOrDefaultAsync(c => c.CorreoElectronico == usuario.CorreoElectronico);

            if (existingEmpleado != null)
            {
                return Conflict(new { message = "El usuario ya está registrado con este correo electrónico." });
            }

            foreach (var property in usuario.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(usuario) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            string emailSubject = "Bienvenido a JRC Abogados!";
            string emailBody = $"Estamos felices de que trabajes con nosotros!";

            bool resultado = await _emailSender.SendEmailAsync(usuario.CorreoElectronico, emailSubject, emailBody);

            if (resultado)
            {
                usuario.Contraseña = _passwordHasher.HashPassword(usuario, usuario.Contraseña);
                usuario.Rol = null;
                _context.Empleado.Add(usuario);
                await _context.SaveChangesAsync();

                var auditoria = new EmpleadoAudit
                {
                    Id = 0,
                    EmpleadoId = usuario.Id,
                    Nombre = usuario.Nombre,
                    CorreoElectronico = usuario.CorreoElectronico,
                    Contraseña = usuario.Contraseña,
                    RolId = usuario.RolId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "CREAR",
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = "Empleado creado"
                };

                _context.EmpleadoAudit.Add(auditoria);
                await _context.SaveChangesAsync();

                usuario.Contraseña = null;
                return CreatedAtAction("GetUsuario", new { id = usuario.Id }, usuario);
            }

            return BadRequest();
        }

        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutUsuario(int id, int empleadoId, Empleado empleado)
        {
            var empleadoActual = await _context.Empleado.FindAsync(id);
            if (empleadoActual == null)
            {
                return NotFound();
            }

            foreach (var property in empleado.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(empleado) as string;

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

            if (empleadoActual.Nombre != empleado.Nombre)
            {
                detallesAccion.AppendLine($"Nombre cambiado de '{empleadoActual.Nombre}' a '{empleado.Nombre}'");
            }
            if (empleadoActual.CorreoElectronico != empleado.CorreoElectronico)
            {
                detallesAccion.AppendLine($"Email cambiado de '{empleadoActual.CorreoElectronico}' a '{empleado.CorreoElectronico}'");
            }
            if (empleadoActual.RolId != empleado.RolId)
            {
                empleadoActual.Rol = await _context.Rol.FindAsync(empleadoActual.RolId);
                empleado.Rol = await _context.Rol.FindAsync(empleado.RolId);

                detallesAccion.AppendLine($"Rol cambiado de '{empleadoActual.Rol.Nombre}' a '{empleado.Rol.Nombre}'");
            }
            if (empleadoActual.Contraseña != empleado.Contraseña)
            {
                detallesAccion.AppendLine($"Contraseña cambiada de '{empleadoActual.Contraseña}' a '{empleado.Contraseña}'");
            }

            _context.Entry(empleadoActual).CurrentValues.SetValues(empleado);

            try
            {
                await _context.SaveChangesAsync();

                var auditoria = new EmpleadoAudit
                {
                    EmpleadoId = empleado.Id,
                    Nombre = empleado.Nombre,
                    CorreoElectronico = empleado.CorreoElectronico,
                    Contraseña = empleado.Contraseña,
                    RolId = empleado.RolId,
                    FechaAccion = DateTime.Now,
                    TipoAccion = "ACTUALIZAR",
                    EmpleadoAccionId = empleadoId,
                    DetallesAccion = detallesAccion.ToString()
                };

                _context.EmpleadoAudit.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteUsuario(int id, int empleadoId)
        {
            var empleado = await _context.Empleado.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

            var auditoria = new EmpleadoAudit
            {
                EmpleadoId = empleado.Id,
                Nombre = empleado.Nombre,
                CorreoElectronico = empleado.CorreoElectronico,
                Contraseña = empleado.Contraseña,
                RolId = empleado.RolId,
                FechaAccion = DateTime.Now,
                TipoAccion = "ACTUALIZAR",
                EmpleadoAccionId = empleadoId,
                DetallesAccion = "Empleado Eliminado"
            };

            _context.Empleado.Remove(empleado);
            _context.EmpleadoAudit.Add(auditoria);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuarioExists(int id)
        {
            return _context.Empleado.Any(e => e.Id == id);
        }
    }
}
