using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EventHub.Domain.Entities;

namespace EventHub.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<EventSession> EventSessions => Set<EventSession>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Soft delete: EF Core automatically adds "WHERE IsDeleted = 0" to every query of these entities
        builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);
        builder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<EventSession>().HasQueryFilter(s => !s.IsDeleted);
        builder.Entity<Organization>().HasQueryFilter(o => !o.IsDeleted);
        builder.Entity<Payment>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Registration>().HasQueryFilter(r => !r.IsDeleted);
        builder.Entity<Review>().HasQueryFilter(r => !r.IsDeleted);
        builder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);
        builder.Entity<Favorite>().HasQueryFilter(f => !f.IsDeleted);
        builder.Entity<RefreshToken>().HasQueryFilter(r => !r.IsDeleted);

        builder.Entity<Organization>(entity =>
        {
            entity.HasOne(o => o.Owner)
                .WithOne(u => u.Organization)
                .HasForeignKey<Organization>(o => o.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);  
        });

        builder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(18, 2);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EventSession>(entity =>
        {
            entity.HasOne(s => s.Event)
                .WithMany(e => e.EventSessions)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);  
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Event)
                .WithMany(e => e.Reviews)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);// if an event is deleted, its reviews will also be deleted
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasKey(f => new { f.UserId, f.EventId });

            entity.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Event)
                .WithMany(e => e.Favorites)
                .HasForeignKey(f => f.EventId)
                .OnDelete(DeleteBehavior.Cascade); // if an event is deleted, its favorites will also be deleted
        });

        builder.Entity<Registration>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.EventSession)
                .WithMany(s => s.Registrations)
                .HasForeignKey(r => r.EventSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.HasOne(t => t.Registration)
                .WithOne(r => r.Ticket)
                .HasForeignKey<Ticket>(t => t.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);

            entity.HasOne(p => p.Registration)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
