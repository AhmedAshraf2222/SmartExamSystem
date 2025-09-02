using Microsoft.EntityFrameworkCore;

namespace Graduation_proj.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamUnit> ExamUnits { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<ProblemChoice> ProblemChoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Doctor -> Materials
            modelBuilder.Entity<Material>()
                .HasOne(m => m.Doctor)
                .WithMany(d => d.Materials)
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Material -> Topics
            modelBuilder.Entity<Topic>()
                .HasOne(t => t.Material)
                .WithMany(m => m.Topics)
                .HasForeignKey(t => t.MaterialId)
                .OnDelete(DeleteBehavior.NoAction);

            // Material -> Exams
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Material)
                .WithMany(m => m.Exams)
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.NoAction);

            // Topic -> Groups
            modelBuilder.Entity<Group>()
                .HasOne(g => g.Topic)
                .WithMany(t => t.Groups)
                .HasForeignKey(g => g.TopicId)
                .OnDelete(DeleteBehavior.NoAction);

            // Group -> Problems
            modelBuilder.Entity<Problem>()
                .HasOne(p => p.Group)
                .WithMany(g => g.Problems)
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.NoAction);

            // Problem -> ProblemChoices
            modelBuilder.Entity<ProblemChoice>()
                .HasOne(pc => pc.Problem)
                .WithMany(p => p.ProblemChoices)
                .HasForeignKey(pc => pc.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamUnit -> Exam
            modelBuilder.Entity<ExamUnit>()
                .HasOne(eu => eu.Exam)
                .WithMany(e => e.ExamUnits)
                .HasForeignKey(eu => eu.ExamId)
                .OnDelete(DeleteBehavior.NoAction);

            // ExamUnit -> Group
            modelBuilder.Entity<ExamUnit>()
                .HasOne(eu => eu.Group)
                .WithMany(g => g.ExamUnits)
                .HasForeignKey(eu => eu.GroupId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure composite key for ExamUnit if needed
            modelBuilder.Entity<ExamUnit>()
                .HasKey(eu => new { eu.ExamId, eu.GroupId, eu.UnitOrder });
        }
    }
}