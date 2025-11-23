using System;

namespace DestekAPI.Models
{
    public class YapilanIs
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public string KullaniciAdSoyad { get; set; }
        public string IslemTuru { get; set; }
        public string Aciklama { get; set; }
        public DateTime Tarih { get; set; }
    }
}