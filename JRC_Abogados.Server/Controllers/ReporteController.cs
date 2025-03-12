using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using JRC_Abogados.Server.DataBaseContext;
using JRC_Abogados.Server.Models;
using JRC_Abogados.Server.Models.audits;
using JRC_Abogados.Server.Models.EmailHelper;
using JRC_Abogados.Server.ModelsDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JRC_Abogados.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReporteController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IEmailSender _emailSender;
        List<string> palabrasProhibidas = new List<string>
        {
            "<", ">", "\"", "'", ";", "--", "/*", "*/", "script", "onerror", "onclick"
        };

        public ReporteController(DBaseContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reporte>>> GetReportes()
        {
            var reportes = await _context.Reporte.ToListAsync();

            foreach (var reporte in reportes)
            {
                if (reporte.ClienteId != null)
                {
                    reporte.Cliente = await _context.Cliente.FindAsync(reporte.ClienteId);
                }
                if (reporte.EmpleadoId != null)
                {
                    reporte.Empleado = await _context.Empleado.FindAsync(reporte.EmpleadoId);
                }
                reporte.EmpleadoAccion = await _context.Empleado.FindAsync(reporte.EmpleadoAccionId);
            }

            return reportes;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Reporte>> GetReporte(int id)
        {
            var reporte = await _context.Reporte.FindAsync(id);

            if (reporte == null)
            {
                return NotFound();
            }

            if (reporte.ClienteId != 0)
            {
                reporte.Cliente = await _context.Cliente.FindAsync(reporte.ClienteId);
            }
            if (reporte.EmpleadoId != 0)
            {
                reporte.Empleado = await _context.Empleado.FindAsync(reporte.EmpleadoId);
            }
            reporte.EmpleadoAccion = await _context.Empleado.FindAsync(reporte.EmpleadoAccionId);

            return reporte;
        }

        [HttpGet("sendPDF/{id}/{mail}")]
        public async Task<ActionResult<Reporte>> SendPDF(int id, string mail)
        {
            var reporte = await _context.Reporte.FindAsync(id);

            if (reporte == null)
            {
                return NotFound();
            }

            string emailSubject = "Reporte JRC Abogados.";
            string emailBody = $"<p>Por favor, haga clic en el siguiente enlace para ver su reporte:</p><a href='https://jrcweb-001-site1.atempurl.com{reporte.Path}'>Ver Reporte</a>";

            await _emailSender.SendEmailAsync(mail, emailSubject, emailBody);

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<Recordatorio>> PostReporte(ReporteDTO reporteDTO)
        {
            var existingCliente = await _context.Reporte.FirstOrDefaultAsync(c => c.Nombre == reporteDTO.Nombre);

            if (existingCliente != null)
            {
                return Conflict(new { message = "Ya existe un reporte con el mismo nombre." });
            }

            foreach (var property in reporteDTO.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(reporteDTO) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            var cliente = await _context.Cliente.FindAsync(reporteDTO.ClienteId);

            Reporte reporte = new Reporte();
            reporte.Nombre = reporteDTO.Nombre;
            reporte.Descripcion = reporteDTO.Descripcion;
            reporte.FechaGeneracion = DateTime.Now;
            reporte.Path = "";
            reporte.TablasSeleccionadas = reporteDTO.TablasSeleccionadas;
            reporte.EmpleadoAccionId = (int)reporteDTO.EmpleadoAccionId;
            if (reporteDTO.ClienteId != null)
            {
                reporte.ClienteId = reporteDTO.ClienteId;
            }
            if (reporteDTO.EmpleadoId != null)
            {
                reporte.EmpleadoId = reporteDTO.EmpleadoId;
            }
            if (reporteDTO.FechaInicio != null)
            {
                reporte.FechaInicio = reporteDTO.FechaInicio;
                reporte.FechaFin = reporteDTO.FechaFin;
            }

            var auditoriaResultado = await generarAuditorias(reporteDTO);
            string pdfPath;
            try
            {
                pdfPath = await GenerarPDF(auditoriaResultado);
            }
            catch (Exception ex)
            {
                return BadRequest("Error al generar el PDF: " + ex.Message);
            }

            reporte.Path = pdfPath;
            _context.Reporte.Add(reporte);

            try
            {
                _context.SaveChanges();

                return Ok();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is Empleado)
                    {
                        var databaseValues = entry.GetDatabaseValues();
                        if (databaseValues == null)
                        {
                            return BadRequest("El registro ya no existe en la base de datos.");
                        }
                        else
                        {
                            return BadRequest("Concurrencia detectada. Valores en la base de datos: {0}");
                        }
                    }
                }
            }

            return NotFound();
        }

        [HttpGet("generar-audotorias")]
        public async Task<AuditoriaResultado> generarAuditorias(ReporteDTO reporte)
        {
            var auditoriaResultado = new AuditoriaResultado();
            auditoriaResultado.Reporte = reporte;

            var casoAudit = _context.CasoAudit.AsQueryable();
            var clienteAudit = _context.ClienteAudit.AsQueryable();
            var citaAudit = _context.CitaAudit.AsQueryable();
            var recordatorioAudit = _context.RecordatorioAudit.AsQueryable();
            var expedienteAudit = _context.ExpedienteAudit.AsQueryable();
            var documentoAudit = _context.DocumentoAudit.AsQueryable();

            var clientesList = await _context.Cliente.ToListAsync();
            var clientesAuditList = await _context.ClienteAudit.ToListAsync();
            var empleadosList = await _context.Empleado.ToListAsync();
            var empleadosAuditList = await _context.EmpleadoAudit.ToListAsync();
            var ubicacionesList = await _context.Ubicacion.ToListAsync();
            var estadosList = await _context.Estado.ToListAsync();

            if (reporte.FechaInicio != null)
            {
                casoAudit = casoAudit.Where(c => c.FechaAccion >= reporte.FechaInicio && c.FechaAccion <= reporte.FechaFin);
                citaAudit = citaAudit.Where(c => c.FechaAccion >= reporte.FechaInicio && c.FechaAccion <= reporte.FechaFin);
                clienteAudit = clienteAudit.Where(c => c.FechaAccion >= reporte.FechaInicio && c.FechaAccion <= reporte.FechaFin);
                recordatorioAudit = recordatorioAudit.Where(c => c.FechaAccion >= reporte.FechaInicio && c.FechaAccion <= reporte.FechaFin);
                expedienteAudit = expedienteAudit.Where(c => c.FechaAccion >= reporte.FechaInicio && c.FechaAccion <= reporte.FechaFin);
                documentoAudit = documentoAudit.Where(c => c.FechaAccion >= reporte.FechaInicio && c.FechaAccion <= reporte.FechaFin);
            }

            if (reporte.ClienteId != null)
            {
                var cliente = await _context.Cliente.FindAsync(reporte.ClienteId);

                casoAudit = casoAudit.Where(c => c.ClienteId == reporte.ClienteId);
                citaAudit = citaAudit.Where(c => c.ClienteId == reporte.ClienteId);
                clienteAudit = clienteAudit.Where(c => c.ClienteId == reporte.ClienteId);
                recordatorioAudit = recordatorioAudit.Where(c => c.ClienteId >= reporte.ClienteId);
                expedienteAudit = expedienteAudit.Where(c => c.ClienteId == reporte.ClienteId);
                documentoAudit = documentoAudit.Where(c => c.ClienteId == reporte.ClienteId);
            }

            if (reporte.EmpleadoId != null)
            {
                casoAudit = casoAudit.Where(c => c.EmpleadoAccionId == reporte.EmpleadoId);
                citaAudit = citaAudit.Where(c => c.EmpleadoAccionId == reporte.EmpleadoId);
                clienteAudit = clienteAudit.Where(c => c.EmpleadoAccionId == reporte.EmpleadoId);
                recordatorioAudit = recordatorioAudit.Where(c => c.EmpleadoAccionId == reporte.EmpleadoId);
                expedienteAudit = expedienteAudit.Where(c => c.EmpleadoAccionId == reporte.EmpleadoId);
                documentoAudit = documentoAudit.Where(c => c.EmpleadoAccionId == reporte.EmpleadoId);
            }



            if (reporte.TablasSeleccionadas.Contains("Caso"))
            {
                auditoriaResultado.CasosAuditados = await casoAudit.ToListAsync();

                foreach (var caso in auditoriaResultado.CasosAuditados)
                {
                    caso.TipoCaso = await _context.TipoCaso.FindAsync(caso.TipoCasoId);
                    caso.Juzgado = await _context.Juzgado.FindAsync(caso.JuzgadoId);
                    caso.Ubicacion = ubicacionesList.FirstOrDefault(ubicacion => ubicacion.Id == caso.UbicacionId);
                    caso.Estado = estadosList.FirstOrDefault(estado => estado.Id == caso.EstadoId);

                    caso.Cliente = clientesList.FirstOrDefault(cliente => cliente.Id == caso.ClienteId);
                    if (caso.Cliente == null)
                    {
                        var clienteAudit2 = clientesAuditList.FirstOrDefault(c => c.ClienteId == caso.ClienteId);

                        if (clienteAudit2 != null)
                        {
                            caso.Cliente = new Cliente
                            {
                                Id = clienteAudit2.ClienteId,
                                Nombre = clienteAudit2.Nombre,
                                Apellido = clienteAudit2.Apellido,
                                FechaNacimiento = clienteAudit2.FechaNacimiento,
                                Telefono = clienteAudit2.Telefono,
                                CorreoElectronico = clienteAudit2.CorreoElectronico,
                                UbicacionId = clienteAudit2.UbicacionId,
                                EmpleadoId = clienteAudit2.EmpleadoId
                            };
                        }
                    }
                    caso.Empleado = empleadosList.FirstOrDefault(c => c.Id == caso.EmpleadoId);
                    if (caso.Empleado == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == caso.EmpleadoId);

                        if (empleadoAudit2 != null)
                        {
                            caso.Empleado = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                    caso.EmpleadoAccion = empleadosList.FirstOrDefault(c => c.Id == caso.EmpleadoAccionId);
                    if (caso.EmpleadoAccion == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == caso.EmpleadoAccionId);

                        if (empleadoAudit2 != null)
                        {
                            caso.EmpleadoAccion = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                }
            }
            if (reporte.TablasSeleccionadas.Contains("Cliente"))
            {
                auditoriaResultado.ClientesAuditados = await clienteAudit.ToListAsync();
                foreach (var cliente in auditoriaResultado.ClientesAuditados)
                {
                    cliente.Ubicacion = ubicacionesList.FirstOrDefault(c => c.Id == cliente.UbicacionId);
                    cliente.Empleado = empleadosList.FirstOrDefault(c => c.Id == cliente.EmpleadoId);
                    if (cliente.Empleado == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == cliente.EmpleadoId);

                        if (empleadoAudit2 != null)
                        {
                            cliente.Empleado = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                    cliente.EmpleadoAccion = empleadosList.FirstOrDefault(c => c.Id == cliente.EmpleadoAccionId);
                    if (cliente.EmpleadoAccion == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == cliente.EmpleadoAccionId);

                        if (empleadoAudit2 != null)
                        {
                            cliente.EmpleadoAccion = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                }
            }
            if (reporte.TablasSeleccionadas.Contains("Cita"))
            {
                auditoriaResultado.CitasAuditadas = await citaAudit.ToListAsync();

                foreach (var cita in auditoriaResultado.CitasAuditadas)
                {
                    cita.Ubicacion = ubicacionesList.FirstOrDefault(c => c.Id == cita.UbicacionId);
                    cita.Estado = estadosList.FirstOrDefault(c => c.Id == cita.EstadoId);

                    cita.Cliente = clientesList.FirstOrDefault(c => c.Id == cita.ClienteId);
                    if (cita.Cliente == null)
                    {
                        var clienteAudit2 = clientesAuditList.FirstOrDefault(c => c.ClienteId == cita.ClienteId);

                        if (clienteAudit2 != null)
                        {
                            cita.Cliente = new Cliente
                            {
                                Id = clienteAudit2.ClienteId,
                                Nombre = clienteAudit2.Nombre,
                                Apellido = clienteAudit2.Apellido,
                                FechaNacimiento = clienteAudit2.FechaNacimiento,
                                Telefono = clienteAudit2.Telefono,
                                CorreoElectronico = clienteAudit2.CorreoElectronico,
                                UbicacionId = clienteAudit2.UbicacionId,
                                EmpleadoId = clienteAudit2.EmpleadoId
                            };
                        }
                    }
                    cita.Empleado = empleadosList.FirstOrDefault(c => c.Id == cita.EmpleadoId);
                    if (cita.Empleado == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == cita.EmpleadoId);

                        if (empleadoAudit2 != null)
                        {
                            cita.Empleado = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                    cita.EmpleadoAccion = empleadosList.FirstOrDefault(c => c.Id == cita.EmpleadoAccionId);
                    if (cita.EmpleadoAccion == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == cita.EmpleadoAccionId);

                        if (empleadoAudit2 != null)
                        {
                            cita.EmpleadoAccion = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                }
            }
            if (reporte.TablasSeleccionadas.Contains("Recordatorio"))
            {
                auditoriaResultado.RecordatoriosAuditados = await recordatorioAudit.ToListAsync();

                foreach (var recordatorio in auditoriaResultado.RecordatoriosAuditados)
                {
                    recordatorio.Cliente = clientesList.FirstOrDefault(c => c.Id == recordatorio.ClienteId);
                    if (recordatorio.Cliente == null)
                    {
                        var clienteAudit2 = clientesAuditList.FirstOrDefault(c => c.ClienteId == recordatorio.ClienteId);

                        if (clienteAudit2 != null)
                        {
                            recordatorio.Cliente = new Cliente
                            {
                                Id = clienteAudit2.ClienteId,
                                Nombre = clienteAudit2.Nombre,
                                Apellido = clienteAudit2.Apellido,
                                FechaNacimiento = clienteAudit2.FechaNacimiento,
                                Telefono = clienteAudit2.Telefono,
                                CorreoElectronico = clienteAudit2.CorreoElectronico,
                                UbicacionId = clienteAudit2.UbicacionId,
                                EmpleadoId = clienteAudit2.EmpleadoId
                            };
                        }
                    }
                    recordatorio.Empleado = empleadosList.FirstOrDefault(c => c.Id == recordatorio.EmpleadoId);
                    if (recordatorio.Empleado == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == recordatorio.EmpleadoId);

                        if (empleadoAudit2 != null)
                        {
                            recordatorio.Empleado = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                    recordatorio.EmpleadoAccion = empleadosList.FirstOrDefault(c => c.Id == recordatorio.EmpleadoAccionId);
                    if (recordatorio.EmpleadoAccion == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == recordatorio.EmpleadoAccionId);

                        if (empleadoAudit2 != null)
                        {
                            recordatorio.EmpleadoAccion = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                }
            }
            if (reporte.TablasSeleccionadas.Contains("Expediente"))
            {
                auditoriaResultado.ExpedientesAuditados = await expedienteAudit.ToListAsync();

                foreach (var expediente in auditoriaResultado.ExpedientesAuditados)
                {
                    expediente.TipoExpediente = await _context.TipoExpediente.FindAsync(expediente.TipoExpedienteId);
                    expediente.Cliente = clientesList.FirstOrDefault(c => c.Id == expediente.ClienteId);
                    if (expediente.Cliente == null)
                    {
                        var clienteAudit2 = clientesAuditList.FirstOrDefault(c => c.ClienteId == expediente.ClienteId);

                        if (clienteAudit2 != null)
                        {
                            expediente.Cliente = new Cliente
                            {
                                Id = clienteAudit2.ClienteId,
                                Nombre = clienteAudit2.Nombre,
                                Apellido = clienteAudit2.Apellido,
                                FechaNacimiento = clienteAudit2.FechaNacimiento,
                                Telefono = clienteAudit2.Telefono,
                                CorreoElectronico = clienteAudit2.CorreoElectronico,
                                UbicacionId = clienteAudit2.UbicacionId,
                                EmpleadoId = clienteAudit2.EmpleadoId
                            };
                        }
                    }
                    if (expediente.CasoId != null)
                    {
                        expediente.Caso = await _context.Caso.FindAsync(expediente.CasoId);
                        if (expediente.Caso == null)
                        {
                            var casoAudit2 = await _context.CasoAudit.FirstOrDefaultAsync(c => c.CasoId == expediente.CasoId);

                            if (casoAudit2 != null)
                            {
                                expediente.Caso = new Caso
                                {
                                    Id = casoAudit2.CasoId,
                                    TipoCasoId = casoAudit2.TipoCasoId,
                                    JuzgadoId = casoAudit2.JuzgadoId,
                                    UbicacionId = casoAudit2.UbicacionId,
                                    Descripcion = casoAudit2.Descripcion,
                                    FechaInicio = casoAudit2.FechaInicio,
                                    FechaTermino = casoAudit2.FechaTermino,
                                    EstadoId = casoAudit2.EstadoId,
                                    ClienteId = casoAudit2.ClienteId,
                                    EmpleadoId = casoAudit2.EmpleadoId,
                                };
                            }
                        }
                    }
                    expediente.Empleado = empleadosList.FirstOrDefault(c => c.Id == expediente.EmpleadoId);
                    if (expediente.Empleado == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == expediente.EmpleadoId);

                        if (empleadoAudit2 != null)
                        {
                            expediente.Empleado = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                    expediente.EmpleadoAccion = empleadosList.FirstOrDefault(c => c.Id == expediente.EmpleadoAccionId);
                    if (expediente.EmpleadoAccion == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == expediente.EmpleadoAccionId);

                        if (empleadoAudit2 != null)
                        {
                            expediente.EmpleadoAccion = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                }
            }
            if (reporte.TablasSeleccionadas.Contains("Documento"))
            {
                auditoriaResultado.DocumentosAuditados = await documentoAudit.ToListAsync();

                foreach (var documento in auditoriaResultado.DocumentosAuditados)
                {
                    documento.Expediente = await _context.Expediente.FindAsync(documento.ExpedienteId);
                    if (documento.Expediente == null)
                    {
                        var expedienteAudit2 = await _context.ExpedienteAudit.FirstOrDefaultAsync(c => c.ExpedienteId == documento.ExpedienteId);

                        if (expedienteAudit2 != null)
                        {
                            documento.Expediente = new Expediente
                            {
                                Id = expedienteAudit2.ExpedienteId,
                                Nombre = expedienteAudit2.Nombre,
                                TipoExpedienteId = expedienteAudit2.TipoExpedienteId,
                                FechaInicio = expedienteAudit2.FechaInicio,
                                ClienteId = expedienteAudit2.ClienteId,
                                CasoId = expedienteAudit2.CasoId,
                                EmpleadoId = expedienteAudit2.EmpleadoId
                            };
                        }
                    }
                    documento.Cliente = clientesList.FirstOrDefault(c => c.Id == documento.ClienteId);
                    if (documento.Cliente == null)
                    {
                        var clienteAudit2 = clientesAuditList.FirstOrDefault(c => c.ClienteId == documento.ClienteId);

                        if (clienteAudit2 != null)
                        {
                            documento.Cliente = new Cliente
                            {
                                Id = clienteAudit2.ClienteId,
                                Nombre = clienteAudit2.Nombre,
                                Apellido = clienteAudit2.Apellido,
                                FechaNacimiento = clienteAudit2.FechaNacimiento,
                                Telefono = clienteAudit2.Telefono,
                                CorreoElectronico = clienteAudit2.CorreoElectronico,
                                UbicacionId = clienteAudit2.UbicacionId,
                                EmpleadoId = clienteAudit2.EmpleadoId
                            };
                        }
                    }
                    documento.Empleado = empleadosList.FirstOrDefault(c => c.Id == documento.EmpleadoId);
                    if (documento.Empleado == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == documento.EmpleadoId);

                        if (empleadoAudit2 != null)
                        {
                            documento.Empleado = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                    documento.EmpleadoAccion = empleadosList.FirstOrDefault(c => c.Id == documento.EmpleadoAccionId);
                    if (documento.EmpleadoAccion == null)
                    {
                        var empleadoAudit2 = empleadosAuditList.FirstOrDefault(c => c.EmpleadoId == documento.EmpleadoAccionId);

                        if (empleadoAudit2 != null)
                        {
                            documento.EmpleadoAccion = new Empleado
                            {
                                Id = empleadoAudit2.EmpleadoId,
                                Nombre = empleadoAudit2.Nombre,
                                CorreoElectronico = empleadoAudit2.CorreoElectronico,
                                Contraseña = empleadoAudit2.Contraseña,
                                RolId = empleadoAudit2.RolId
                            };
                        }
                    }
                }
            }

            return auditoriaResultado;
        }

        [HttpPost("generar-pdf")]
        public async Task<string> GenerarPDF([FromBody] AuditoriaResultado auditoriaResultado)
        {
            try
            {
                var ms = new MemoryStream();
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdfDoc = new PdfDocument(writer);
                Document doc = new Document(pdfDoc);

                doc.SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);
                doc.Add(new Paragraph("Reporte de Auditoría")).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                doc.Add(new Paragraph($"{auditoriaResultado.Reporte.Nombre}")).SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT);
                doc.Add(new Paragraph($"Generado el: {DateTime.Now.ToString("dd/MM/yyyy")}"));
                var empleadoAccion = await _context.Empleado.FindAsync(auditoriaResultado.Reporte.EmpleadoAccionId);
                doc.Add(new Paragraph($"Generado por: {empleadoAccion.Nombre}"));

                if (auditoriaResultado.Reporte.FechaInicio != null)
                {
                    doc.Add(new Paragraph($"Datos obtenidos de: {auditoriaResultado.Reporte.FechaInicio?.ToString("dd/MM/yyyy")} - {auditoriaResultado.Reporte.FechaFin?.ToString("dd/MM/yyyy")}"));
                }

                if (auditoriaResultado.Reporte.ClienteId != null)
                {
                    var cliente = await _context.Cliente.FindAsync(auditoriaResultado.Reporte.ClienteId);
                    doc.Add(new Paragraph($"Filtrado por Cliente: {cliente.Nombre + " " + cliente.Apellido}"));
                }
                if (auditoriaResultado.Reporte.EmpleadoId != null)
                {
                    var empleado = await _context.Empleado.FindAsync(auditoriaResultado.Reporte.EmpleadoId);
                    doc.Add(new Paragraph($"Filtrado por Empleado: {empleado.Nombre}"));
                }
                doc.Add(new Paragraph($"Fitro de tablas: {auditoriaResultado.Reporte.TablasSeleccionadas}")).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);


                if (auditoriaResultado.CasosAuditados != null && auditoriaResultado.CasosAuditados.Count > 0)
                {
                    doc.Add(new Paragraph("\nTabla: Caso"));

                    Table table = new Table(6);
                    table.AddCell(new Cell().Add(new Paragraph("Tipo Caso")));
                    table.AddCell(new Cell().Add(new Paragraph("N° exp. Juzgado")));
                    table.AddCell(new Cell().Add(new Paragraph("Cliente")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha")));
                    table.AddCell(new Cell().Add(new Paragraph("Acción")));
                    table.AddCell(new Cell().Add(new Paragraph("Empleado")));

                    foreach (var caso in auditoriaResultado.CasosAuditados)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(caso.TipoCaso.Nombre)));
                        table.AddCell(new Cell().Add(new Paragraph(caso.Juzgado.NumeroExpediente)));
                        table.AddCell(new Cell().Add(new Paragraph(caso.Cliente.Nombre + " " + caso.Cliente.Apellido)));
                        table.AddCell(new Cell().Add(new Paragraph(caso.FechaAccion.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(caso.DetallesAccion)));
                        table.AddCell(new Cell().Add(new Paragraph(caso.EmpleadoAccion.Nombre)));
                    }

                    doc.Add(table);
                }
                if (auditoriaResultado.ClientesAuditados != null && auditoriaResultado.ClientesAuditados.Count > 0)
                {
                    doc.Add(new Paragraph("\nTabla: Cliente"));

                    Table table = new Table(6);
                    table.AddCell(new Cell().Add(new Paragraph("Nombre")));
                    table.AddCell(new Cell().Add(new Paragraph("Email")));
                    table.AddCell(new Cell().Add(new Paragraph("Telefono")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha")));
                    table.AddCell(new Cell().Add(new Paragraph("Acción")));
                    table.AddCell(new Cell().Add(new Paragraph("Empleado")));

                    foreach (var cliente in auditoriaResultado.ClientesAuditados)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(cliente.Nombre + " " + cliente.Apellido)));
                        table.AddCell(new Cell().Add(new Paragraph(cliente.CorreoElectronico)));
                        table.AddCell(new Cell().Add(new Paragraph(cliente.Telefono)));
                        table.AddCell(new Cell().Add(new Paragraph(cliente.FechaAccion.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(cliente.DetallesAccion)));
                        table.AddCell(new Cell().Add(new Paragraph(cliente.EmpleadoAccion.Nombre)));
                    }

                    doc.Add(table);
                }
                if (auditoriaResultado.CitasAuditadas != null && auditoriaResultado.CitasAuditadas.Count > 0)
                {
                    doc.Add(new Paragraph("\nTabla: Cita"));

                    Table table = new Table(6);
                    table.AddCell(new Cell().Add(new Paragraph("Tipo Cita")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha de Cita")));
                    table.AddCell(new Cell().Add(new Paragraph("Cliente")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha")));
                    table.AddCell(new Cell().Add(new Paragraph("Acción")));
                    table.AddCell(new Cell().Add(new Paragraph("Empleado")));

                    foreach (var cita in auditoriaResultado.CitasAuditadas)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(cita.TipoCita)));
                        table.AddCell(new Cell().Add(new Paragraph(cita.FechaInicio.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(cita.Cliente.Nombre + " " + cita.Cliente.Apellido)));
                        table.AddCell(new Cell().Add(new Paragraph(cita.FechaAccion.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(cita.DetallesAccion)));
                        table.AddCell(new Cell().Add(new Paragraph(cita.EmpleadoAccion.Nombre)));
                    }

                    doc.Add(table);
                }
                if (auditoriaResultado.RecordatoriosAuditados != null && auditoriaResultado.RecordatoriosAuditados.Count > 0)
                {
                    doc.Add(new Paragraph("\nTabla: Recordatorio"));

                    Table table = new Table(6);
                    table.AddCell(new Cell().Add(new Paragraph("Titulo")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha Recordatorio")));
                    table.AddCell(new Cell().Add(new Paragraph("Cliente")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha")));
                    table.AddCell(new Cell().Add(new Paragraph("Acción")));
                    table.AddCell(new Cell().Add(new Paragraph("Empleado")));

                    foreach (var recordatorio in auditoriaResultado.RecordatoriosAuditados)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(recordatorio.Titulo)));
                        table.AddCell(new Cell().Add(new Paragraph(recordatorio.Fecha.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(recordatorio.Cliente.Nombre + " " + recordatorio.Cliente.Apellido)));
                        table.AddCell(new Cell().Add(new Paragraph(recordatorio.FechaAccion.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(recordatorio.DetallesAccion)));
                        table.AddCell(new Cell().Add(new Paragraph(recordatorio.EmpleadoAccion.Nombre)));
                    }

                    doc.Add(table);
                }
                if (auditoriaResultado.ExpedientesAuditados != null && auditoriaResultado.ExpedientesAuditados.Count > 0)
                {
                    doc.Add(new Paragraph("\nTabla: Expediente"));

                    Table table = new Table(6);
                    table.AddCell(new Cell().Add(new Paragraph("Nombre")));
                    table.AddCell(new Cell().Add(new Paragraph("Tipo Expediente")));
                    table.AddCell(new Cell().Add(new Paragraph("Cliente")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha")));
                    table.AddCell(new Cell().Add(new Paragraph("Acción")));
                    table.AddCell(new Cell().Add(new Paragraph("Empleado")));

                    foreach (var expediente in auditoriaResultado.ExpedientesAuditados)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(expediente.Nombre)));
                        table.AddCell(new Cell().Add(new Paragraph(expediente.TipoExpediente.Nombre)));
                        table.AddCell(new Cell().Add(new Paragraph(expediente.Cliente.Nombre + " " + expediente.Cliente.Apellido)));
                        table.AddCell(new Cell().Add(new Paragraph(expediente.FechaAccion.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(expediente.DetallesAccion)));
                        table.AddCell(new Cell().Add(new Paragraph(expediente.EmpleadoAccion.Nombre)));
                    }

                    doc.Add(table);
                }
                if (auditoriaResultado.DocumentosAuditados != null && auditoriaResultado.DocumentosAuditados.Count > 0)
                {
                    doc.Add(new Paragraph("\nTabla: Documento"));

                    Table table = new Table(6);
                    table.AddCell(new Cell().Add(new Paragraph("Nombre")));
                    table.AddCell(new Cell().Add(new Paragraph("Expediente")));
                    table.AddCell(new Cell().Add(new Paragraph("Cliente")));
                    table.AddCell(new Cell().Add(new Paragraph("Fecha")));
                    table.AddCell(new Cell().Add(new Paragraph("Acción")));
                    table.AddCell(new Cell().Add(new Paragraph("Empleado")));

                    foreach (var documento in auditoriaResultado.DocumentosAuditados)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(documento.Nombre)));
                        table.AddCell(new Cell().Add(new Paragraph(documento.Expediente.Nombre)));
                        table.AddCell(new Cell().Add(new Paragraph(documento.Cliente.Nombre + " " + documento.Cliente.Apellido)));
                        table.AddCell(new Cell().Add(new Paragraph(documento.FechaAccion.ToString("dd/MM/yyyy"))));
                        table.AddCell(new Cell().Add(new Paragraph(documento.DetallesAccion)));
                        table.AddCell(new Cell().Add(new Paragraph(documento.EmpleadoAccion.Nombre)));
                    }

                    doc.Add(table);
                }

                doc.Close();

                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
                //string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "MisReportes");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = $"{Guid.NewGuid()}.pdf";
                string filePath = Path.Combine(folderPath, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, ms.ToArray());

                return $"/pdfs/{fileName}";
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el PDF: " + ex.Message);
            }
        }


        [HttpPut("{id}/{empleadoId}")]
        public async Task<IActionResult> PutReporte(int id, int empleadoId, ReporteDTO reporteDTO)
        {
            var reporteActual = await _context.Reporte.FindAsync(id);

            foreach (var property in reporteDTO.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(reporteDTO) as string;

                    if (!string.IsNullOrEmpty(value) && palabrasProhibidas.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new
                        {
                            Error = $"El campo '{property.Name}' contiene caracteres o palabras no permitidas."
                        });
                    }
                }
            }

            reporteActual.Nombre = reporteDTO.Nombre;
            reporteActual.Descripcion = reporteDTO.Descripcion;
            reporteActual.FechaGeneracion = reporteDTO.FechaGeneracion;
            reporteActual.Path = reporteDTO.Path;
            reporteActual.TablasSeleccionadas = reporteDTO.TablasSeleccionadas;
            reporteActual.EmpleadoAccionId = (int)reporteDTO.EmpleadoAccionId;
            if (reporteDTO.ClienteId != null)
            {
                reporteActual.ClienteId = reporteDTO.ClienteId;
            }
            if (reporteDTO.EmpleadoId != null)
            {
                reporteActual.EmpleadoId = reporteDTO.EmpleadoId;
            }
            if (reporteDTO.FechaInicio != null)
            {
                reporteActual.FechaInicio = reporteDTO.FechaInicio;
                reporteActual.FechaFin = reporteDTO.FechaFin;
            }

            _context.Entry(reporteActual).CurrentValues.SetValues(reporteDTO);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReporteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        [HttpDelete("{id}/{empleadoId}")]
        public async Task<IActionResult> DeleteReporte(int id, int empleadoId)
        {
            var reporte = await _context.Reporte.FindAsync(id);
            if (reporte == null)
            {
                return NotFound();
            }

            _context.Reporte.Remove(reporte);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReporteExists(int id)
        {
            return _context.Reporte.Any(e => e.Id == id);
        }
    }
}
