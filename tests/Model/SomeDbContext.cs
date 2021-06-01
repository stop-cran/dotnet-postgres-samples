using Microsoft.EntityFrameworkCore;

namespace PostgresSamples.Model
{
    public class SomeDbContext : DbContext
    {
        public SomeDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<SomeEntity> SomeEntities { get; set; }
        public DbSet<ParentEntity> ParentEntities { get; set; }
        public DbSet<Child1Entity> Child1Entities { get; set; }
        public DbSet<Child2Entity> Child2Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SomeEntity>()
                .ToTable("SomeEntity", "public")
                .HasKey(a => a.Id);
            modelBuilder.Entity<ParentEntity>()
                .ToTable("ParentEntity", "public")
                .HasKey(a => a.Id);
            modelBuilder.Entity<Child1Entity>()
                .ToTable("Child1Entity", "public")
                .HasKey(a => a.Id);
            modelBuilder.Entity<Child2Entity>()
                .ToTable("Child2Entity", "public")
                .HasKey(a => a.Id);
            
            modelBuilder.Entity<ParentEntity>()
                .HasMany(p => p.Children1)
                .WithOne(p => p.Parent)
                .HasPrincipalKey(p => p.Id)
                .HasForeignKey(p => p.ParentId);

            modelBuilder.Entity<ParentEntity>()
                .HasMany(p => p.Children2)
                .WithOne(p => p.Parent)
                .HasPrincipalKey(p => p.Id)
                .HasForeignKey(p => p.ParentId);
        }
    }
}