using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Models;

public partial class DemoAgentContext : DbContext
{

    public static DemoAgentContext INSTANCE = new DemoAgentContext();
    public DemoAgentContext()
    {
    }

    public DemoAgentContext(DbContextOptions<DemoAgentContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Meeting> Meetings { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(config.GetConnectionString("DBContext"));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Account");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Mail).HasColumnName("mail");
            entity.Property(e => e.Name)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("name");
            entity.Property(e => e.PasswordKey).HasColumnName("passwordKey");
            entity.Property(e => e.PrivateKey).HasColumnName("privateKey");
            entity.Property(e => e.PublicKey).HasColumnName("publicKey");
            entity.Property(e => e.RoleId).HasColumnName("roleId");
            entity.Property(e => e.StatusId).HasColumnName("statusId");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Account_Role");

            entity.HasOne(d => d.Status).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_Account_Status");
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.ToTable("Meeting");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Creator)
                .HasMaxLength(50)
                .HasColumnName("creator");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.TimeEnd)
                .HasColumnType("datetime")
                .HasColumnName("timeEnd");
            entity.Property(e => e.TimeStart)
                .HasColumnType("datetime")
                .HasColumnName("timeStart");

            entity.HasOne(d => d.Status).WithMany(p => p.Meetings)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meeting_Status");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
