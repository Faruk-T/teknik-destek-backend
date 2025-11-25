using DestekAPI.Data;
using DestekAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using DestekAPI.Hubs;
using System.Security.Claims;

namespace DestekAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SikayetController : ControllerBase
    {
        private readonly DestekDbContext _context;

        public SikayetController(DestekDbContext context)
        {
            _context = context;
        }

        // GET: api/Sikayet/tum
        [Authorize(Roles = "yonetici")]
        [HttpGet("tum")]
        public async Task<ActionResult<IEnumerable<Sikayet>>> GetTumSikayetler()
        {
            return await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .OrderByDescending(s => s.Tarih)
                .ToListAsync();
        }

        // GET: api/Sikayet/cozulenler
        [Authorize(Roles = "yonetici")]
        [HttpGet("cozulenler")]
        public async Task<ActionResult<IEnumerable<Sikayet>>> GetCozulenSikayetler()
        {
            return await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .Where(s => s.Durum == "Çözüldü")
                .OrderByDescending(s => s.Tarih)
                .ToListAsync();
        }

        // MÜŞTERİ: Kendi şikayetleri
        [Authorize]
        [HttpGet("kullanici/{kullaniciId}")]
        public async Task<ActionResult<IEnumerable<Sikayet>>> GetSikayetlerByKullanici(int kullaniciId)
        {
            return await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .Where(s => s.KullaniciId == kullaniciId)
                .OrderByDescending(s => s.Tarih)
                .ToListAsync();
        }

        // YÖNETİCİ: Şikayet atama (GÜVENLİ LOGLAMA EKLENDİ)
        [Authorize(Roles = "yonetici")]
        [HttpPut("{id}/ata")]
        public async Task<IActionResult> AtaSikayet(int id, [FromBody] AtamaModel model)
        {
            var sikayet = await _context.Sikayetler.FindAsync(id);
            if (sikayet == null) return NotFound();

            sikayet.YoneticiId = model.YoneticiId;
            sikayet.AtamaTarihi = DateTime.Now;
            sikayet.Durum = "İşleniyor";
            await _context.SaveChangesAsync();

            // --- LOGLAMA KISMI (Güvenli Hale Getirildi) ---
            try
            {
                var yonetici = await _context.Kullanicilar.FindAsync(model.YoneticiId);
                string yapanAdSoyad = "Sistem Yöneticisi";
                int yapanId = 0;

                // Token'dan ID'yi almayı dene
                var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out int parsedId))
                {
                    yapanId = parsedId;
                    var yapanUser = await _context.Kullanicilar.FindAsync(yapanId);
                    if (yapanUser != null) yapanAdSoyad = yapanUser.AdSoyad;
                }

                var log = new YapilanIs
                {
                    KullaniciId = yapanId,
                    KullaniciAdSoyad = yapanAdSoyad,
                    IslemTuru = "Atama",
                    Aciklama = $"Şikayet #{sikayet.Id}, {yonetici?.AdSoyad ?? "Yönetici"} kişisine atandı.",
                    Tarih = DateTime.Now
                };
                _context.YapilanIsler.Add(log);
                await _context.SaveChangesAsync();

                // SignalR Bildirimi
                var hubContext = HttpContext.RequestServices.GetService<IHubContext<ChatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.Group($"user_{model.YoneticiId}").SendAsync("SikayetAtandi", sikayet.Id, sikayet.Konu, model.YoneticiId, yapanAdSoyad);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loglama hatası: " + ex.Message);
                // Loglama hatası yüzünden işlem iptal olmasın, devam et.
            }

            return NoContent();
        }

        // YÖNETİCİ: Durum Güncelleme (GÜVENLİ LOGLAMA EKLENDİ)
        [Authorize(Roles = "yonetici")]
        [HttpPut("{id}/durum")]
        public async Task<IActionResult> UpdateDurum(int id, [FromBody] DurumUpdateModel durumModel)
        {
            var sikayet = await _context.Sikayetler.FindAsync(id);
            if (sikayet == null) return NotFound();

            sikayet.Durum = durumModel.Durum;

            if (durumModel.Durum == "Çözüldü")
            {
                sikayet.CozulmeTarihi = DateTime.Now;
                sikayet.CozumAciklamasi = durumModel.CozumAciklamasi;
            }
            else if (durumModel.Durum == "İşleniyor")
            {
                sikayet.CozulmeTarihi = null;
                sikayet.CozumAciklamasi = null;
            }

            await _context.SaveChangesAsync();

            // --- LOGLAMA KISMI (Güvenli) ---
            try
            {
                string yapanAdSoyad = "Sistem Yöneticisi";
                int yapanId = 0;

                var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out int parsedId))
                {
                    yapanId = parsedId;
                    var yapanUser = await _context.Kullanicilar.FindAsync(yapanId);
                    if (yapanUser != null) yapanAdSoyad = yapanUser.AdSoyad;
                }

                var log = new YapilanIs
                {
                    KullaniciId = yapanId,
                    KullaniciAdSoyad = yapanAdSoyad,
                    IslemTuru = "Durum Güncelle",
                    Aciklama = $"Şikayet #{sikayet.Id} durumu '{sikayet.Durum}' yapıldı.",
                    Tarih = DateTime.Now
                };
                _context.YapilanIsler.Add(log);
                await _context.SaveChangesAsync();

                var hubContext = HttpContext.RequestServices.GetService<IHubContext<ChatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("SikayetDurumGuncellendi", id, durumModel.Durum, durumModel.CozumAciklamasi);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loglama hatası: " + ex.Message);
            }

            return NoContent();
        }

        // MÜŞTERİ: Şikayet ekle
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Sikayet>> PostSikayet([FromBody] SikayetEkleDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var sikayet = new Sikayet
            {
                KullaniciId = dto.KullaniciId,
                Konu = dto.Konu,
                Aciklama = dto.Aciklama,
                AlpemixIsteniyor = dto.AlpemixIsteniyor,
                AlpemixKodu = dto.AlpemixKodu,
                Tarih = DateTime.Now,
                Durum = "Bekliyor",
                Oncelik = "Orta"
            };

            _context.Sikayetler.Add(sikayet);
            await _context.SaveChangesAsync();

            // SignalR Bildirim
            try
            {
                var hubContext = HttpContext.RequestServices.GetService<IHubContext<ChatHub>>();
                if (hubContext != null)
                    await hubContext.Clients.All.SendAsync("YeniSikayetEklendi", sikayet.Id, sikayet.Konu, sikayet.Aciklama, sikayet.KullaniciId);
            }
            catch { }

            return CreatedAtAction(nameof(GetSikayet), new { id = sikayet.Id }, sikayet);
        }

        // GET: api/Sikayet/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Sikayet>> GetSikayet(int id)
        {
            var sikayet = await _context.Sikayetler.Include(s => s.Kullanici).Include(s => s.Yonetici).FirstOrDefaultAsync(s => s.Id == id);
            if (sikayet == null) return NotFound();
            return sikayet;
        }

        // YÖNETİCİ: Günlük çözülen şikayetler
        [Authorize(Roles = "yonetici")]
        [HttpGet("gunluk-cozulenler")]
        public async Task<ActionResult<IEnumerable<object>>> GetGunlukCozulenler()
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);
            var data = await _context.Sikayetler
                .Include(s => s.Kullanici).Include(s => s.Yonetici)
                .Where(s => s.Durum == "Çözüldü" && s.CozulmeTarihi >= bugun && s.CozulmeTarihi < yarin)
                .OrderByDescending(s => s.CozulmeTarihi)
                .Select(s => new {
                    s.Id,
                    s.Konu,
                    s.Aciklama,
                    s.CozulmeTarihi,
                    s.CozumAciklamasi,
                    s.Oncelik,
                    MusteriAdi = s.Kullanici.AdSoyad,
                    YoneticiAdi = s.Yonetici.AdSoyad
                }).ToListAsync();
            return Ok(data);
        }

        // YÖNETİCİ: Günlük Özet
        [Authorize(Roles = "yonetici")]
        [HttpGet("gunluk-ozet")]
        public async Task<ActionResult<object>> GetGunlukOzet()
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);
            var data = await _context.Sikayetler.Include(s => s.Yonetici)
                .Where(s => s.Durum == "Çözüldü" && s.CozulmeTarihi >= bugun && s.CozulmeTarihi < yarin)
                .ToListAsync(); // Önce çek

            // Bellekte grupla
            var ozet = data.GroupBy(s => new { s.YoneticiId, YoneticiAdi = s.Yonetici?.AdSoyad ?? "Bilinmiyor" })
                .Select(g => new {
                    YoneticiAdi = g.Key.YoneticiAdi,
                    CozulenSayisi = g.Count()
                }).ToList();

            return Ok(new { ToplamCozulen = data.Count, Detaylar = ozet });
        }

        // KULLANICI: Bana Atananlar
        [Authorize]
        [HttpGet("bana-atananlar/{kullaniciId}")]
        public async Task<ActionResult<IEnumerable<Sikayet>>> GetBanaAtananlar(int kullaniciId)
        {
            return await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .Where(s => s.YoneticiId == kullaniciId && s.Durum != "Çözüldü")
                .OrderByDescending(s => s.Tarih)
                .ToListAsync();
        }
    }
}