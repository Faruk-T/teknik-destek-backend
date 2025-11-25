using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DestekAPI.Data;
using DestekAPI.Models;

namespace DestekAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DestekDbContext _context;

        public AuthController(IConfiguration configuration, DestekDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // Giriş için kullanıcıdan beklenen veri modeli
        public class LoginDto
        {
            public string KullaniciAdi { get; set; }
            public string Sifre { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            // 1. Kullanıcıyı Veritabanında Ara (Basit şifre kontrolü)
            var user = _context.Kullanicilar.FirstOrDefault(u => u.KullaniciAdi == loginDto.KullaniciAdi && u.Sifre == loginDto.Sifre);

            if (user == null)
            {
                return Unauthorized(new { message = "Hatalı giriş! Kullanıcı adı veya şifre yanlış." });
            }

            // 2. Token Üret (Kullanıcı ID, Adı ve Rolü içine gömülür)
            var tokenString = TokenUret(user.Id.ToString(), user.KullaniciAdi, user.Rol);

            // 3. Frontend'in Beklediği Tüm Bilgileri Döndür (Kritik Kısım Burasıydı)
            return Ok(new
            {
                token = tokenString,
                kullaniciId = user.Id,
                kullaniciAdi = user.KullaniciAdi,
                rol = user.Rol,
                adSoyad = user.AdSoyad,
                sirketAdi = user.SirketAdi
            });
        }

        // Token Üretme Yardımcı Metodu
        private string TokenUret(string id, string userName, string role)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            // Eğer appsettings.json'da Key yoksa yedek anahtarı kullan (Hata almamak için)
            var keyString = jwtSettings["Key"] ?? "Bu_Varsayilan_Guvenli_Olmayan_Bir_Yedek_Anahtardir_Lutfen_Degistiriniz_123";
            var key = Encoding.UTF8.GetBytes(keyString);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role)
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7), // Token 7 gün geçerli
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}