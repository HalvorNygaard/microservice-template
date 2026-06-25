using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroserviceTemplate.Features.Tasks.Models;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(task => task.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(task => task.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(task => task.CreatedAt)
            .IsRequired();

        builder.Property(task => task.UpdatedAt)
            .IsRequired();

        builder.HasIndex(task => task.Status);
        builder.HasIndex(task => task.DueDate);
        builder.HasIndex(task => task.CreatedAt);

        SeedData(builder);
    }

    private static void SeedData(EntityTypeBuilder<TaskItem> builder)
    {
        var now = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Setup Development Environment",
                Description = "Install all necessary tools and dependencies for the project",
                Status = TaskStatus.Done,
                DueDate = now.AddDays(-5),
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-1)
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Review Architecture Documentation",
                Description = "Go through the technical architecture document and provide feedback",
                Status = TaskStatus.InProgress,
                DueDate = now.AddDays(2),
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-1)
            },
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Title = "Implement Authentication",
                Description = "Add JWT-based authentication to the API endpoints",
                Status = TaskStatus.Todo,
                DueDate = now.AddDays(7),
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-1)
            },
            new
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Title = "Write Unit Tests",
                Description = "Create comprehensive unit tests for the service layer",
                Status = TaskStatus.Todo,
                DueDate = now.AddDays(10),
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-1)
            },
            new
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Title = "Deploy to Staging",
                Description = "Deploy the application to staging environment for QA testing",
                Status = TaskStatus.Todo,
                DueDate = now.AddDays(14),
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-1)
            });
    }
}
