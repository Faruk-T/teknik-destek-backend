using System.ComponentModel.DataAnnotations;

namespace DestekAPI.Models
{
    // Atama için model
    public class AtamaModel
    {
        public int YoneticiId { get; set; }
    }

    // Durum güncelleme için model
    public class DurumUpdateModel
    {
        [Required(ErrorMessage = "Durum alanı zorunludur.")]
        public string Durum { get; set; }

        // Çözüm açıklaması (opsiyonel)
        public string? CozumAciklamasi { get; set; }
    }
}