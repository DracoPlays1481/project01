using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using EWDProject.Models;

namespace EWDProject.Models;

public partial class KardContext : DbContext
{
    public KardContext()
    {
    }

    public KardContext(DbContextOptions<KardContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Orderitem> Orderitems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("BookPK");

            entity.ToTable("BOOK");

            entity.Property(e => e.BookId)
                .ValueGeneratedNever()
                .HasColumnName("BookID");
            entity.Property(e => e.Author)
                .HasMaxLength(100)
                .IsFixedLength();
            entity.Property(e => e.Genre)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .IsFixedLength();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("CustomerPK");

            entity.ToTable("CUSTOMER");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .IsFixedLength();
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsFixedLength();
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsFixedLength();
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsFixedLength();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("OrderPK");

            entity.ToTable("ORDER");

            entity.Property(e => e.OrderId)
                .ValueGeneratedNever()
                .HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .IsFixedLength();

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerOrder");
        });

        modelBuilder.Entity<Orderitem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("OrderItemPK");

            entity.ToTable("ORDERITEM");

            entity.Property(e => e.OrderItemId)
                .ValueGeneratedNever()
                .HasColumnName("OrderItemID");
            entity.Property(e => e.BookId).HasColumnName("BookID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");

            entity.HasOne(d => d.Book).WithMany(p => p.Orderitems)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemBook");

            entity.HasOne(d => d.Order).WithMany(p => p.Orderitems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemOrder");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

public DbSet<EWDProject.Models.Admin> Admin { get; set; } = default!;
}
