using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Main Tables
        public DbSet<User> Users => Set<User>();
        public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<UserDocument> UserDocuments => Set<UserDocument>();

        // Communication Module
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Interview> Interviews => Set<Interview>();

        // Enterprise & Assessment Modules
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<SkillAssessment> SkillAssessments => Set<SkillAssessment>();
        public DbSet<RecruitmentAnalytic> RecruitmentAnalytics => Set<RecruitmentAnalytic>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------------------------
            // User
            // ---------------------------
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ---------------------------
            // JobPosting -> Recruiter
            // ---------------------------
            modelBuilder.Entity<JobPosting>()
                .HasOne(j => j.Recruiter)
                .WithMany(u => u.JobPostings)
                .HasForeignKey(j => j.RecruiterId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------------
            // Application -> JobPosting
            // ---------------------------
            modelBuilder.Entity<Application>()
                .HasOne(a => a.JobPosting)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------------
            // Application -> Candidate
            // ---------------------------
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Candidate)
                .WithMany(u => u.Applications)
                .HasForeignKey(a => a.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate applications
            modelBuilder.Entity<Application>()
                .HasIndex(a => new { a.JobPostingId, a.CandidateId })
                .IsUnique();

            // ---------------------------
            // Notification -> Candidate
            // ---------------------------
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Candidate)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------------
            // Interview -> Candidate
            // ---------------------------
            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Candidate)
                .WithMany()
                .HasForeignKey(i => i.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------------
            // Interview -> JobPosting
            // ---------------------------
            modelBuilder.Entity<Interview>()
                .HasOne(i => i.JobPosting)
                .WithMany()
                .HasForeignKey(i => i.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------------
            // SkillAssessment -> Candidate
            // ---------------------------
            modelBuilder.Entity<SkillAssessment>()
                .HasOne(sa => sa.Candidate)
                .WithMany()
                .HasForeignKey(sa => sa.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------------
            // UserDocument -> User
            // ---------------------------
            modelBuilder.Entity<UserDocument>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(d => d.UserId)
                    .UseCollation("utf8mb4_unicode_ci");
            });
        }
    }
}