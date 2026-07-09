using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("ApplicationUsers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.NationalIdentityNumber)
                .HasMaxLength(11)
                .IsRequired();

            builder.HasIndex(x => x.NationalIdentityNumber)
                .IsUnique();

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(x => x.PasswordHash)
                .IsRequired();

            builder.Property(x => x.TransactionPinHash)
                .HasMaxLength(200);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetimeoffset(7)");
        }
    }
}
