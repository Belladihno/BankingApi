using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AccountNumber)
                .HasMaxLength(10)
                .IsRequired();

            builder.HasIndex(x => x.AccountNumber)
                .IsUnique();

            builder.Property(x => x.AccountType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Balance)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.DailyWithdrawalLimit)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(x => x.TodayWithdrawnAmount)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.HasIndex(x => x.OwnerId);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();

            builder.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
