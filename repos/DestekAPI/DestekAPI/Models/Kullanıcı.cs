using System.ComponentModel.DataAnnotations;

namespace DestekAPI.Models
{
    public class Kullanici
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50)]
        public string KullaniciAdi { get; set; } = string.Empty; // Varsayılan değer eklendi

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100)]
        public string Sifre { get; set; } = string.Empty; // Varsayılan değer eklendi

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        public string AdSoyad { get; set; } = string.Empty; // Varsayılan değer eklendi

        [Required(ErrorMessage = "Şirket adı zorunludur.")]
        [StringLength(100)]
        public string SirketAdi { get; set; } = string.Empty; // Varsayılan değer eklendi

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; // Varsayılan değer eklendi

        [Required]
        [StringLength(20)]
        public string Telefon { get; set; } = string.Empty; // Varsayılan değer eklendi

        public string Rol { get; set; } = "musteri";
    }
}