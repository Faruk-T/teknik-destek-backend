using DestekAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DestekAPI.Data
{
    public class DestekDbContext : DbContext
    {
        public DestekDbContext(DbContextOptions<DestekDbContext> options) : base(options)
        {
        }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Sikayet> Sikayetler { get; set; }
        public DbSet<YapilanIs> YapilanIsler { get; set; }
        public DbSet<Message> Messages { get; set; } // <-- Mesajlar tablosu eklendi
    }
}