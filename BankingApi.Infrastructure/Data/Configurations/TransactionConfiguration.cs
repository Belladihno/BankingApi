using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Data.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reference)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => x.Reference)
                .IsUnique();

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.HasIndex(x => x.SourceAccountId);

            builder.HasIndex(x => x.DestinationAccountId);

            builder.HasIndex(x => x.CreatedAt);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();

            builder.Property(x => x.CompletedAt)
                .HasColumnType("datetimeoffset(7)");

            builder.HasOne(x => x.SourceAccount)
                .WithMany()
                .HasForeignKey(x => x.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DestinationAccount)
                .WithMany()
                .HasForeignKey(x => x.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Initiator)
                .WithMany()
                .HasForeignKey(x => x.InitiatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
