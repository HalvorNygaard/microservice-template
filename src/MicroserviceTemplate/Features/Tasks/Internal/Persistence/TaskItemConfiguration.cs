using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ModernMicroservice.Features.Tasks.Internal.Persistence;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Title).IsRequired().HasMaxLength(200);
        builder.Property(task => task.Description).IsRequired().HasMaxLength(2000);
        builder.Property(task => task.Status).IsRequired().HasConversion<string>();
        builder.Property(task => task.CreatedAt).IsRequired();
        builder.Property(task => task.UpdatedAt).IsRequired();
    }
}
