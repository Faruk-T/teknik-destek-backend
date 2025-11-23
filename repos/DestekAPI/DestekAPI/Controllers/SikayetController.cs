using DestekAPI.Data;
using DestekAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using DestekAPI.Hubs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;

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

        // YÖNETİCİ: Tüm şikayetleri listele
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

        // YÖNETİCİ: Çözülen şikayetleri listele
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

        // MÜŞTERİ: Kendi şikayetlerini listele
        // GET: api/Sikayet/kullanici/5
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
        // YÖNETİCİ: Şikayet atama
        // PUT: api/Sikayet/{id}/ata
        [Authorize(Roles = "yonetici")]
        [HttpPut("{id}/ata")]
        public async Task<IActionResult> AtaSikayet(int id, [FromBody] AtamaModel model)
        {
            var sikayet = await _context.Sikayetler.FindAsync(id);
            if (sikayet == null)
                return NotFound();

            sikayet.YoneticiId = model.YoneticiId;
            sikayet.AtamaTarihi = DateTime.Now;
            sikayet.Durum = "İşleniyor";
            await _context.SaveChangesAsync();

            // Log ekle
            var yonetici = await _context.Kullanicilar.FindAsync(model.YoneticiId);
            var yapanKullaniciId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            var yapanKullanici = await _context.Kullanicilar.FindAsync(yapanKullaniciId);

            var log = new YapilanIs
            {
                KullaniciId = yapanKullaniciId,
                KullaniciAdSoyad = yapanKullanici.AdSoyad,
                IslemTuru = "Atama",
                Aciklama = $"Şikayet {sikayet.Id} {yonetici.AdSoyad} kişisine atandı."
            };
            _context.YapilanIsler.Add(log);
            await _context.SaveChangesAsync();

            // SignalR ile atanan yöneticiye bildirim gönder
            try
            {
                var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
                await hubContext.Clients.Group($"user_{model.YoneticiId}")
                    .SendAsync("SikayetAtandi", sikayet.Id, sikayet.Konu, model.YoneticiId, yapanKullanici.AdSoyad);

                // Müşteriye de bildirim gönder (şikayet sahibi)
                await hubContext.Clients.Group($"user_{sikayet.KullaniciId}")
                    .SendAsync("SikayetAtandi", sikayet.Id, sikayet.Konu, model.YoneticiId, yapanKullanici.AdSoyad);
            }
            catch (Exception ex)
            {
                // SignalR hatası loglama ama işlemi durdurma
                Console.WriteLine($"SignalR atama bildirimi hatası: {ex.Message}");
            }

            return NoContent();
        }

        // YÖNETİCİ: Şikayet durumunu güncelle
        // PUT: api/Sikayet/{id}/durum
        [Authorize(Roles = "yonetici")]
        [HttpPut("{id}/durum")]
        public async Task<IActionResult> UpdateDurum(int id, [FromBody] DurumUpdateModel durumModel)
        {
            var sikayet = await _context.Sikayetler.FindAsync(id);
            if (sikayet == null)
                return NotFound();

            sikayet.Durum = durumModel.Durum;

            // Çözüldü durumunda çözüm açıklamasını ve tarihini kaydet
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

            // SignalR ile müşteriye bildirim gönder
            try
            {
                var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
                await hubContext.Clients.All.SendAsync("SikayetDurumGuncellendi", id, durumModel.Durum, durumModel.CozumAciklamasi);
            }
            catch (Exception ex)
            {
                // SignalR hatası loglama ama işlemi durdurma
                Console.WriteLine($"SignalR bildirim hatası: {ex.Message}");
            }

            // Log ekle
            var yapanKullaniciId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            var yapanKullanici = await _context.Kullanicilar.FindAsync(yapanKullaniciId);

            var log = new YapilanIs
            {
                KullaniciId = yapanKullaniciId,
                KullaniciAdSoyad = yapanKullanici.AdSoyad,
                IslemTuru = "Durum Güncelle",
                Aciklama = $"Şikayet {sikayet.Id} durumu {sikayet.Durum} olarak güncellendi."
            };
            _context.YapilanIsler.Add(log);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // TÜM KULLANICILAR: Belirli bir şikayeti getir
        // GET: api/Sikayet/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Sikayet>> GetSikayet(int id)
        {
            var sikayet = await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sikayet == null)
                return NotFound();

            return sikayet;
        }

        // MÜŞTERİ: Şikayet ekle
        // POST: api/Sikayet
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Sikayet>> PostSikayet([FromBody] SikayetEkleDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            // SignalR ile yöneticilere yeni şikayet bildirimi gönder
            try
            {
                var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
                await hubContext.Clients.All.SendAsync("YeniSikayetEklendi", sikayet.Id, sikayet.Konu, sikayet.Aciklama, sikayet.KullaniciId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR yeni şikayet bildirimi hatası: {ex.Message}");
            }

            return CreatedAtAction(nameof(GetSikayet), new { id = sikayet.Id }, sikayet);
        }

        // YÖNETİCİ: Şikayet sil
        // DELETE: api/Sikayet/5
        [Authorize(Roles = "yonetici")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSikayet(int id)
        {
            var sikayet = await _context.Sikayetler.FindAsync(id);
            if (sikayet == null)
                return NotFound();

            _context.Sikayetler.Remove(sikayet);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // KULLANICI: Bana atanan açık/işlemdeki şikayetler
        // GET: api/Sikayet/bana-atananlar/{kullaniciId}
        [Authorize]
        [HttpGet("bana-atananlar/{kullaniciId}")]
        public async Task<ActionResult<IEnumerable<Sikayet>>> GetBanaAtananlar(int kullaniciId)
        {
            return await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .Where(s => s.YoneticiId == kullaniciId && (s.Durum == "Açık" || s.Durum == "İşleniyor" || s.Durum == "İşlemde"))
                .OrderByDescending(s => s.Tarih)
                .ToListAsync();
        }

        // KULLANICI: Bana atanan çözülen şikayetler
        // GET: api/Sikayet/cozulenlerim/{kullaniciId}
        [Authorize]
        [HttpGet("cozulenlerim/{kullaniciId}")]
        public async Task<ActionResult<IEnumerable<Sikayet>>> GetCozulenlerim(int kullaniciId)
        {
            return await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .Where(s => s.YoneticiId == kullaniciId && s.Durum == "Çözüldü")
                .OrderByDescending(s => s.CozulmeTarihi)
                .ToListAsync();
        }

        // YÖNETİCİ: Günlük çözülen şikayetler (bugün çözülenler)
        // GET: api/Sikayet/gunluk-cozulenler
        [Authorize(Roles = "yonetici")]
        [HttpGet("gunluk-cozulenler")]
        public async Task<ActionResult<IEnumerable<object>>> GetGunlukCozulenler()
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            var gunlukCozulenler = await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .Where(s => s.Durum == "Çözüldü" &&
                           s.CozulmeTarihi >= bugun &&
                           s.CozulmeTarihi < yarin)
                .OrderByDescending(s => s.CozulmeTarihi)
                .Select(s => new
                {
                    s.Id,
                    s.Konu,
                    s.Aciklama,
                    s.CozulmeTarihi,
                    s.CozumAciklamasi,
                    s.Oncelik,
                    MusteriAdi = s.Kullanici.AdSoyad,
                    MusteriSirketi = s.Kullanici.SirketAdi,
                    YoneticiAdi = s.Yonetici.AdSoyad,
                    YoneticiId = s.Yonetici.Id
                })
                .ToListAsync();

            // C# tarafında formatla
            var formattedGunlukCozulenler = gunlukCozulenler.Select(s => new
            {
                s.Id,
                s.Konu,
                s.Aciklama,
                s.CozulmeTarihi,
                s.CozumAciklamasi,
                s.Oncelik,
                s.MusteriAdi,
                s.MusteriSirketi,
                s.YoneticiAdi,
                s.YoneticiId,
                CozulmeSaat = s.CozulmeTarihi.Value.ToString("HH:mm"),
                CozulmeGunu = s.CozulmeTarihi.Value.ToString("dd.MM.yyyy")
            }).ToList();

            return formattedGunlukCozulenler;
        }

        // YÖNETİCİ: Belirli bir yöneticinin günlük çözülen şikayetleri
        // GET: api/Sikayet/gunluk-cozulenler/{yoneticiId}
        [Authorize(Roles = "yonetici")]
        [HttpGet("gunluk-cozulenler/{yoneticiId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetGunlukCozulenlerByYonetici(int yoneticiId)
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            var gunlukCozulenler = await _context.Sikayetler
                .Include(s => s.Kullanici)
                .Include(s => s.Yonetici)
                .Where(s => s.Durum == "Çözüldü" &&
                           s.YoneticiId == yoneticiId &&
                           s.CozulmeTarihi >= bugun &&
                           s.CozulmeTarihi < yarin)
                .OrderByDescending(s => s.CozulmeTarihi)
                .Select(s => new
                {
                    s.Id,
                    s.Konu,
                    s.Aciklama,
                    s.CozulmeTarihi,
                    s.CozumAciklamasi,
                    s.Oncelik,
                    MusteriAdi = s.Kullanici.AdSoyad,
                    MusteriSirketi = s.Kullanici.SirketAdi,
                    YoneticiAdi = s.Yonetici.AdSoyad,
                    YoneticiId = s.Yonetici.Id
                })
                .ToListAsync();

            // C# tarafında formatla
            var formattedGunlukCozulenler = gunlukCozulenler.Select(s => new
            {
                s.Id,
                s.Konu,
                s.Aciklama,
                s.CozulmeTarihi,
                s.CozumAciklamasi,
                s.Oncelik,
                s.MusteriAdi,
                s.MusteriSirketi,
                s.YoneticiAdi,
                s.YoneticiId,
                CozulmeSaat = s.CozulmeTarihi.Value.ToString("HH:mm"),
                CozulmeGunu = s.CozulmeTarihi.Value.ToString("dd.MM.yyyy")
            }).ToList();

            return formattedGunlukCozulenler;
        }

        // YÖNETİCİ: Günlük iş özeti (tüm yöneticiler için)
        // GET: api/Sikayet/gunluk-ozet
        [Authorize(Roles = "yonetici")]
        [HttpGet("gunluk-ozet")]
        public async Task<ActionResult<object>> GetGunlukOzet()
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            // Önce veriyi çek, sonra C# tarafında formatla
            var gunlukCozulenler = await _context.Sikayetler
                .Include(s => s.Yonetici)
                .Where(s => s.Durum == "Çözüldü" &&
                           s.CozulmeTarihi >= bugun &&
                           s.CozulmeTarihi < yarin)
                .Select(s => new { s.Id, s.Konu, s.Oncelik, s.CozulmeTarihi, s.YoneticiId, YoneticiAdi = s.Yonetici.AdSoyad })
                .ToListAsync();

            // C# tarafında grupla ve formatla
            var gunlukOzet = gunlukCozulenler
                .GroupBy(s => new { s.YoneticiId, s.YoneticiAdi })
                .Select(g => new
                {
                    YoneticiId = g.Key.YoneticiId,
                    YoneticiAdi = g.Key.YoneticiAdi,
                    CozulenSikayetSayisi = g.Count(),
                    ToplamOncelik = g.Sum(s => s.Oncelik == "Yüksek" ? 3 : s.Oncelik == "Orta" ? 2 : 1),
                    SonCozulenSaat = g.Max(s => s.CozulmeTarihi).Value.ToString("HH:mm"),
                    Sikayetler = g.Select(s => new
                    {
                        s.Id,
                        s.Konu,
                        s.Oncelik,
                        CozulmeSaat = s.CozulmeTarihi.Value.ToString("HH:mm"),
                        CozulmeTarihi = s.CozulmeTarihi
                    }).OrderByDescending(s => s.CozulmeTarihi).ToList()
                })
                .OrderByDescending(x => x.CozulenSikayetSayisi)
                .ThenByDescending(x => x.ToplamOncelik)
                .ToList();

            return new
            {
                Tarih = bugun.ToString("dd.MM.yyyy"),
                ToplamCozulenSikayet = gunlukOzet.Sum(x => x.CozulenSikayetSayisi),
                ToplamYonetici = gunlukOzet.Count,
                YoneticiDetaylari = gunlukOzet
            };
        }

        // Yardımcı: Şikayet var mı?
        private bool SikayetExists(int id)
        {
            return _context.Sikayetler.Any(e => e.Id == id);
        }
    }
}
