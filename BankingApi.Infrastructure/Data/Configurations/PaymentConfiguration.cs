using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(x => x.Reference)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => x.Reference)
                .IsUnique();

            builder.Property(x => x.PaystackReference)
                .HasMaxLength(100);

            builder.Property(x => x.PaystackAccessCode)
                .HasMaxLength(100);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();

            builder.Property(x => x.CompletedAt)
                .HasColumnType("datetimeoffset(7)");

            builder.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
