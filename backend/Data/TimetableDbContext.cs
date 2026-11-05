using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using api.Models;

namespace api.Data;

public class TimetableDbContext : DbContext
{
    public TimetableDbContext(DbContextOptions<TimetableDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        var universityHeader = httpContextAccessor?.HttpContext?.Request.Headers["X-University-Id"].FirstOrDefault();
        CurrentUniversityId = int.TryParse(universityHeader, out var universityId) && universityId > 0
            ? universityId
            : 1;
    }

    public int CurrentUniversityId { get; }
    public DbSet<University> Universities => Set<University>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Lecture> Lectures => Set<Lecture>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<University>(entity =>
        {
            entity.ToTable("universities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Country).HasColumnName("country").IsRequired();
            entity.Property(e => e.State).HasColumnName("state").IsRequired();
            entity.Property(e => e.District).HasColumnName("district").IsRequired();
            entity.Property(e => e.City).HasColumnName("city").IsRequired();
            entity.Property(e => e.ContactEmail).HasColumnName("contact_email").IsRequired();
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.ToTable("teachers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired();
            entity.HasQueryFilter(e => e.UniversityId == CurrentUniversityId);
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("classes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.HasQueryFilter(e => e.UniversityId == CurrentUniversityId);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("subjects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.HasQueryFilter(e => e.UniversityId == CurrentUniversityId);
        });

        modelBuilder.Entity<Period>(entity =>
        {
            entity.ToTable("periods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.Label).HasColumnName("label").IsRequired();
            entity.Property(e => e.StartsAt).HasColumnName("starts_at").IsRequired();
            entity.Property(e => e.EndsAt).HasColumnName("ends_at").IsRequired();
            entity.HasQueryFilter(e => e.UniversityId == CurrentUniversityId);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.FloorNumber).HasColumnName("floor_number").IsRequired();
            entity.Property(e => e.RoomNumber).HasColumnName("room_number").IsRequired();
            entity.Ignore(e => e.Name);
            entity.HasIndex(e => new { e.UniversityId, e.FloorNumber, e.RoomNumber }).IsUnique();
            entity.HasQueryFilter(e => e.UniversityId == CurrentUniversityId);
        });

        modelBuilder.Entity<Lecture>(entity =>
        {
            entity.ToTable("lectures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UniversityId).HasColumnName("university_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id").IsRequired();
            entity.Property(e => e.ClassId).HasColumnName("class_id").IsRequired();
            entity.Property(e => e.SubjectId).HasColumnName("subject_id").IsRequired();
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week").IsRequired();
            entity.Property(e => e.PeriodId).HasColumnName("period_id").IsRequired();
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasQueryFilter(e => e.UniversityId == CurrentUniversityId);

            entity.HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Class)
                .WithMany()
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Subject)
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        AssignUniversity();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AssignUniversity();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AssignUniversity()
    {
        foreach (var entry in ChangeTracker.Entries<IUniversityOwned>().Where(entry => entry.State == EntityState.Added))
            entry.Entity.UniversityId = CurrentUniversityId;
    }
}
