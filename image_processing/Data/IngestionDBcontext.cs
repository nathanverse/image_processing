using image_processing.Data.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace image_processing.Data;

public class IngestionDBcontext : DbContext
{
    public DbSet<TaskModel> Tasks { get; set; } = null!; 

    public IngestionDBcontext(DbContextOptions<IngestionDBcontext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskModel>().HasKey(t => t.Id);
    }
}