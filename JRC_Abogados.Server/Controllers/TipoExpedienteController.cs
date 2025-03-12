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
    public class TipoExpedienteController : ControllerBase
    {
        private readonly DBaseContext _context;

        public TipoExpedienteController(DBaseContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TipoExpediente>>> GetTiposDeExpediente()
        {
            return await _context.TipoExpediente.ToListAsync();
        }
    }
}
