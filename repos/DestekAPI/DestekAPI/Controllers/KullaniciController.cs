using DestekAPI.Data;
using DestekAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DestekAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KullaniciController : ControllerBase
    {
        private readonly DestekDbContext _context;
        private readonly IConfiguration _configuration;

        public KullaniciController(DestekDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Kullanici
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Kullanici>>> GetKullanicilar()
        {
            return await _context.Kullanicilar.ToListAsync();
        }

        // GET: api/Kullanici/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Kullanici>> GetKullanici(int id)
        {
            var kullanici = await _context.Kullanicilar.FindAsync(id);

            if (kullanici == null)
            {
                return NotFound();
            }

            return kullanici;
        }

        // POST: api/Kullanici
        [HttpPost]
        public async Task<ActionResult<Kullanici>> PostKullanici(Kullanici kullanici)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Aynı kullanıcı adı kontrolü
            var existingKullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == kullanici.KullaniciAdi);

            if (existingKullanici != null)
            {
                return BadRequest(new { message = "Bu kullanıcı adı zaten kullanılıyor!" });
            }

            // Şifreyi hash'le
            kullanici.Sifre = HashPassword(kullanici.Sifre);

            _context.Kullanicilar.Add(kullanici);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetKullanici), new { id = kullanici.Id }, kullanici);
        }

        // POST: api/Kullanici/login
        [HttpPost("login")]
        public async Task<ActionResult<object>> Login([FromBody] LoginModel loginModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciAdi == loginModel.KullaniciAdi);

            if (kullanici == null || !VerifyPassword(loginModel.Sifre, kullanici.Sifre))
            {
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı" });
            }

            // JWT Token üret
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
                new Claim(ClaimTypes.Role, kullanici.Rol)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"])),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                kullaniciId = kullanici.Id,
                kullaniciAdi = kullanici.KullaniciAdi,
                adSoyad = kullanici.AdSoyad,
                rol = kullanici.Rol,
                sirketAdi = kullanici.SirketAdi
            });
        }

        // PUT: api/Kullanici/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutKullanici(int id, Kullanici kullanici)
        {
            if (id != kullanici.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(kullanici).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KullaniciExists(id))
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

        // DELETE: api/Kullanici/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKullanici(int id)
        {
            var kullanici = await _context.Kullanicilar.FindAsync(id);
            if (kullanici == null)
            {
                return NotFound();
            }

            _context.Kullanicilar.Remove(kullanici);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool KullaniciExists(int id)
        {
            return _context.Kullanicilar.Any(e => e.Id == id);
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return string.Empty;
            }

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Sifre { get; set; } = string.Empty;
    }
}