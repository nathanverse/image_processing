using Google.Api;
using image_processing.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace image_processing.Data;

public class AppDbContext : DbContext
{
    public DbSet<TaskModel> Tasks { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskModel>().HasKey(t => t.Id);
    }
}