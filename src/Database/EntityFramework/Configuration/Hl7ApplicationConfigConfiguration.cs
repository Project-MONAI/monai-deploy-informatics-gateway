using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration
{
    internal class Hl7ApplicationConfigConfiguration : IEntityTypeConfiguration<Hl7ApplicationConfigEntity>
    {
        public void Configure(EntityTypeBuilder<Hl7ApplicationConfigEntity> builder)
        {
            builder.HasKey(j => j.Id);
            builder.Property(j => j.DataLink.Key).IsRequired();
            builder.Property(j => j.DataLink.Value).IsRequired();
            builder.Property(j => j.DataMapping.Key).IsRequired();
            builder.Property(j => j.DataMapping.Value).IsRequired();
            builder.Property(j => j.SendingId.Key).IsRequired();
            builder.Property(j => j.SendingId.Value).IsRequired();

            builder.Ignore(p => p.Id);
        }
    }
}
