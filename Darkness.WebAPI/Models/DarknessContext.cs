using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darkness.WebAPI.Models
{
    public partial class DarknessContext : DbContext
    {
        public DarknessContext()
        {
        }

        public DarknessContext(DbContextOptions<DarknessContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Characters> Characters { get; set; }
        public virtual DbSet<Users> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=COMPUTER\\DARKNESS;Database=Darkness;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Characters>(entity =>
            {
                entity.HasKey(e => e.CharacterId);

                entity.HasIndex(e => e.CharacterName)
                    .HasName("CharName")
                    .IsUnique();

                entity.Property(e => e.CharacterName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CharacterXp).HasColumnName("CharacterXP");

                entity.Property(e => e.Class)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Creation).HasColumnType("datetime");

                entity.Property(e => e.PlayerGuid)
                    .IsRequired()
                    .HasColumnName("PlayerGUID")
                    .HasMaxLength(50);

                entity.Property(e => e.PremierClass)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.Creation).HasColumnType("datetime");

                entity.Property(e => e.EmailAddress)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Guid)
                    .HasColumnName("GUID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TimeZone)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Xp).HasColumnName("XP");
            });
        }
    }
}
