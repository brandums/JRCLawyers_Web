using JRC_Abogados.Server.DataBaseContext;
using JRC_Abogados.Server.Models;
using JRC_Abogados.Server.Models.EmailHelper;
using JRC_Abogados.Server.ModelsDTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JRC_Abogados.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DBaseContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Empleado> _passwordHasher;
        private readonly IEmailSender _emailSender;

        public AuthController(DBaseContext context, IConfiguration configuration, IEmailSender emailSender)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Empleado>();
            _emailSender = emailSender;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            var isCaptchaValid = await VerifyCaptcha(loginDto.CaptchaResponse);

            if (!isCaptchaValid)
            {
                return Unauthorized(new { message = "Captcha inválido." });
            }

            var usuario = await _context.Empleado.FirstOrDefaultAsync(u => u.CorreoElectronico == loginDto.CorreoElectronico);

            if (usuario == null)
            {
                return Unauthorized(new { message = "Usuario no encontrado." });
            }

            if (usuario.Contraseña != loginDto.Contraseña)
            {
                if (usuario.Contraseña.Length < 15)
                {
                    return Unauthorized(new { message = "Contraseña incorrecta." });
                }
                else
                {
                    var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(usuario, usuario.Contraseña, loginDto.Contraseña);
                    if (passwordVerificationResult != PasswordVerificationResult.Success)
                    {
                        return Unauthorized(new { message = "Contraseña incorrecta." });
                    }
                }
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, usuario.Id.ToString()),
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var controller = new RecordatorioController(_context, _emailSender);
            await controller.CheckAndSendReminders();

            return Ok(new { Token = tokenString, Usuario = usuario });
        }

        private async Task<bool> VerifyCaptcha(string captchaResponse)
        {
            var secretKey = _configuration["ReCaptcha:SecretKey"];
            var apiUrl = $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaResponse}";

            using (var client = new HttpClient())
            {
                var result = await client.GetStringAsync(apiUrl);
                var captchaVerification = JsonConvert.DeserializeObject<ReCaptchaVerification>(result);
                return captchaVerification.Success;
            }
        }
    }
}
