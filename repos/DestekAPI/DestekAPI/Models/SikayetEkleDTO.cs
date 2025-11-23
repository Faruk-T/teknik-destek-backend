using System.ComponentModel.DataAnnotations;

namespace DestekAPI.Models
{
    public class SikayetEkleDTO
    {
        [Required]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Konu zorunludur.")]
        [StringLength(100, ErrorMessage = "Konu en fazla 100 karakter olabilir.")]
        public string Konu { get; set; }

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        public string Aciklama { get; set; }

        public bool AlpemixIsteniyor { get; set; } = false;
        public string AlpemixKodu { get; set; } // Alpemix bağlantı kodu
    }
}