using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

public partial class SmartCampusDbContext : DbContext
{
    public SmartCampusDbContext()
    {
    }

    public SmartCampusDbContext(DbContextOptions<SmartCampusDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activity> Activities { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<PaymentFee> PaymentFees { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<RegistrationPayment> RegistrationPayments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RulesRegulation> RulesRegulations { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentRegistration> StudentRegistrations { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Tutor> Tutors { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public object Student_Registrations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=SmartCampusDb;User Id=sa;Password=sasa@123;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.ActivityId).HasName("PK__Activity__393F5A452A0468C9");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PK__Book__C223F3B4A9366735");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);

            entity.HasOne(d => d.Category).WithMany(p => p.Books)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Book__Category_I__5DCAEF64");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB38D6EDD2875F1");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__151675F14D7DCED4");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);

            entity.HasOne(d => d.Faculty).WithMany(p => p.Departments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Departmen__Facul__59063A47");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.FacultyId).HasName("PK__Faculty__4EFCEAAADECD7578");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
        });

        modelBuilder.Entity<PaymentFee>(entity =>
        {
            entity.HasKey(e => e.FeesId).HasName("PK__Payment___24E61F7BD6034E00");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValue("Active");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__3C3EAE062191C9DE");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
        });

        modelBuilder.Entity<RegistrationPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Registra__DA6C7FC1C239E7F3");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Registration).WithMany(p => p.RegistrationPayments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Registrat__Regis__02084FDA");

            entity.HasOne(d => d.VerifyByNavigation).WithMany(p => p.RegistrationPayments).HasConstraintName("FK__Registrat__Verif__02FC7413");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__D80AB4BB2E4B3432");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
        });

        modelBuilder.Entity<RulesRegulation>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("PK__Rules_Re__70B7089E77C8C2C5");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__12459A74A033FE51");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__A2F4E98C1185513A");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValue("Active");

            entity.HasOne(d => d.User).WithMany(p => p.Students)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student__User_Id__7C4F7684");
        });

        modelBuilder.Entity<StudentRegistration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PK__Student___22A298F6A8464C8B");

            entity.Property(e => e.ApplicationDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreatedDatetime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.StipendRequested).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.StudentRegistrations).HasConstraintName("FK__Student_R__user___76969D2E");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subject__D98F54B64787BB85");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);

            entity.HasOne(d => d.Semester).WithMany(p => p.Subjects)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Subject__Semeste__628FA481");
        });

        modelBuilder.Entity<Tutor>(entity =>
        {
            entity.HasKey(e => e.TutorId).HasName("PK__Tutor__95664E0D409B1130");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);

            entity.HasOne(d => d.Department).WithMany(p => p.Tutors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor__Departmen__6EF57B66");

            entity.HasOne(d => d.Position).WithMany(p => p.Tutors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor__Position___6E01572D");

            entity.HasOne(d => d.User).WithMany(p => p.Tutors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor__User_Id__6D0D32F4");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__206D91702B22E388");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__Role_id__68487DD7");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
