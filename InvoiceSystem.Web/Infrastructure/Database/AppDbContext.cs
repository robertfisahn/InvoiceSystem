using InvoiceSystem.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Infrastructure.Database;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<KsefSetting> KsefSettings => Set<KsefSetting>();
    public DbSet<KsefIncomingInvoice> KsefIncomingInvoices => Set<KsefIncomingInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Konfiguracja encji (można przenieść do osobnych klas w IEntityTypeConfiguration)
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();
            
        modelBuilder.Entity<Contractor>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<KsefIncomingInvoice>()
            .HasIndex(k => k.KsefNumber)
            .IsUnique();
    }
}
