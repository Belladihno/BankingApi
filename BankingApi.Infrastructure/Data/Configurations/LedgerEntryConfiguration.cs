using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Data.Configurations
{
    public class LedgerEntryConfiguration : IEntityTypeConfiguration<TransactionLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<TransactionLedgerEntry> builder)
        {
            builder.ToTable("TransactionLedgerEntries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntryType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(x => x.BalanceBefore)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(x => x.BalanceAfter)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();

            builder.HasOne(x => x.Transaction)
                .WithMany(t => t.LedgerEntries)
                .HasForeignKey(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
