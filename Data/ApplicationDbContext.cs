using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clinic> Clinics => Set<Clinic>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<PatientMedicalProfile> PatientMedicalProfiles => Set<PatientMedicalProfile>();

    public DbSet<TreatmentRecord> TreatmentRecords => Set<TreatmentRecord>();

    public DbSet<SignImg> SignImgs => Set<SignImg>();

    public DbSet<NhssoClinicMaster> NhssoClinicMasters => Set<NhssoClinicMaster>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.ClinicCode).HasMaxLength(10);
            entity.Property(x => x.ProfessionalTitle).HasMaxLength(200);
            entity.Property(x => x.LicenseNo).HasMaxLength(100);
            entity.Property(x => x.ProviderSignatureFileName).HasMaxLength(260);
            entity.Property(x => x.ProviderSignatureContentType).HasMaxLength(120);
            entity.Property(x => x.ProviderSignatureImageData).HasColumnType("varbinary(max)");
        });

        builder.Entity<Clinic>(entity =>
        {
            entity.HasIndex(x => x.ClinicCode).IsUnique();
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ClinicName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
            entity.Property(x => x.LogoPath).HasMaxLength(300);
            entity.Property(x => x.RegisteredBy).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(50).IsRequired();
        });

        builder.Entity<Patient>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.CitizenId }).IsUnique();
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CitizenId).HasMaxLength(13).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(20).IsRequired();
            entity.Property(x => x.PhotoPath).HasMaxLength(300);
        });

        builder.Entity<PatientMedicalProfile>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.PatientId, x.InformationGivenDate });
            entity.HasIndex(x => new { x.ClinicCode, x.PatientId }).IsUnique();
            entity.HasIndex(x => new { x.ClinicCode, x.CitizenId, x.CreatedAtUtc });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CitizenId).HasMaxLength(13).IsRequired();
            entity.Property(x => x.ServiceRecipientId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ClinicName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ClinicAddress).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.PatientName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Race).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Nationality).HasMaxLength(50).IsRequired();
            entity.Property(x => x.MaritalStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.RegisteredAddress).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ContactAddress).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.PrimaryHospital).HasMaxLength(300).IsRequired();
            entity.Property(x => x.UnderlyingDisease).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.PastHistory).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.FamilyHistory).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.AllergyHistory).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.MedicalBenefit).HasMaxLength(300).IsRequired();
            entity.Property(x => x.EmergencyContactName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmergencyContactPhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SourcePdfFileName).HasMaxLength(260);
            entity.Property(x => x.SourcePdfContentType).HasMaxLength(120);
            entity.Property(x => x.SourcePdfData).HasColumnType("varbinary(max)");
            entity
                .HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TreatmentRecord>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.PatientId, x.VisitDate });
            entity.HasIndex(x => new { x.ClinicCode, x.CitizenId, x.VisitDate });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ServiceRecipientId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AuthenticationCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CitizenId).HasMaxLength(13).IsRequired();
            entity.Property(x => x.Diagnosis).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.InitialDifferentialDiagnosis).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ChiefComplaint).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.PresentIllness).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.PhysicalExam).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ProblemPhysicalExam).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.TreatmentAndAdvice).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ReferralDetail).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.FollowUpClinicNote).HasMaxLength(500);
            entity.Property(x => x.FollowUpOtherNote).HasMaxLength(500);
            entity.Property(x => x.ChildGrowthStatus).HasMaxLength(20);
            entity.Property(x => x.ChildDevelopmentStatus).HasMaxLength(20);
            entity.Property(x => x.ChildVaccineStatus).HasMaxLength(20);
            entity.Property(x => x.ChildVaccineNote).HasMaxLength(500);
            entity.Property(x => x.Note).HasMaxLength(2000);
            entity.Property(x => x.OpdFileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.OpdContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.OpdPdfData).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(x => x.ProviderUserId).HasMaxLength(450);
            entity.Property(x => x.ProviderName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProviderProfessionalTitle).HasMaxLength(200).IsRequired();
            entity
                .HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SignImg>(entity =>
        {
            entity.ToTable("SignImg");
            entity.HasIndex(x => new { x.ClinicCode, x.CitizenId, x.UploadedAtUtc });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CitizenId).HasMaxLength(13).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ImageData).HasColumnType("varbinary(max)").IsRequired();
        });

        builder.Entity<NhssoClinicMaster>(entity =>
        {
            entity.HasIndex(x => x.ClinicCode).IsUnique();
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ClinicName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ContactPhone).HasMaxLength(30);
            entity.Property(x => x.ContactEmail).HasMaxLength(200);
        });
    }
}
