using ModernMicroservice.Features.Tasks;
using ModernMicroservice.Features.Tasks.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ModernMicroservice.Infrastructure.Data;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    internal DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
    }
}
