using System.ComponentModel.DataAnnotations;

namespace DestekAPI.Models
{
    public class Kullanici
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        public string KullaniciAdi { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Sifre { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        public string AdSoyad { get; set; }

        [Required(ErrorMessage = "Şirket adı zorunludur.")]
        [StringLength(100)]
        public string SirketAdi { get; set; }

        [Required(ErrorMessage = "Email zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [StringLength(20)]
        public string Telefon { get; set; }

        [Required]
        [StringLength(20)]
        public string Rol { get; set; } = "musteri";
    }
}
