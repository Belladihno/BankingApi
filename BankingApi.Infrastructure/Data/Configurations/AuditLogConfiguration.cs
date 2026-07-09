using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.EntityId)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Action)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.ActorId)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => new { x.EntityId, x.EntityName });

            builder.HasIndex(x => x.ActorId);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();
        }
    }
}
