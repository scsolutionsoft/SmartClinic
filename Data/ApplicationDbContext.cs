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
    public DbSet<Icd10Code> Icd10Codes => Set<Icd10Code>();

    public DbSet<SignImg> SignImgs => Set<SignImg>();

    public DbSet<NhssoClinicMaster> NhssoClinicMasters => Set<NhssoClinicMaster>();
    public DbSet<WoundCareRecord> WoundCareRecords => Set<WoundCareRecord>();
    public DbSet<WoundCarePhoto> WoundCarePhotos => Set<WoundCarePhoto>();
    public DbSet<PublicAnnouncement> PublicAnnouncements => Set<PublicAnnouncement>();
    public DbSet<CreditRequest> CreditRequests => Set<CreditRequest>();
    public DbSet<QuotaTransaction> QuotaTransactions => Set<QuotaTransaction>();
    public DbSet<PromotionalMedia> PromotionalMedia => Set<PromotionalMedia>();
    public DbSet<PaymentSlip> PaymentSlips => Set<PaymentSlip>();
    public DbSet<AdminMessage> AdminMessages => Set<AdminMessage>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<MedicationLabelTemplate> MedicationLabelTemplates => Set<MedicationLabelTemplate>();
    public DbSet<ClinicDrug> ClinicDrugs => Set<ClinicDrug>();
    public DbSet<IcdDrugProtocol> IcdDrugProtocols => Set<IcdDrugProtocol>();
    public DbSet<DrugAdviceTemplate> DrugAdviceTemplates => Set<DrugAdviceTemplate>();
    public DbSet<DrugKnowledgeAuditLog> DrugKnowledgeAuditLogs => Set<DrugKnowledgeAuditLog>();

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
            entity.Property(x => x.OpeningHours).HasMaxLength(500).IsRequired();
            entity.Property(x => x.LogoPath).HasMaxLength(300);
            entity.Property(x => x.RegisteredBy).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(50).IsRequired();
            // Keep the existing column names so current installations upgrade without losing quota data.
            entity.Property(x => x.OpdRecordLimit).HasColumnName("PatientLimit").HasDefaultValue(30);
            entity.Property(x => x.HasUnlimitedOpdRecords).HasColumnName("HasUnlimitedPatients");
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Active").IsRequired();
            entity.Property(x => x.LastReviewedByUserId).HasMaxLength(450);
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
            entity.HasIndex(x => new { x.ClinicCode, x.FollowUpAppointmentDateTime });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ServiceRecipientId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AuthenticationCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CitizenId).HasMaxLength(13).IsRequired();
            entity.Property(x => x.Diagnosis).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.PrimaryIcd10Code).HasMaxLength(10).IsRequired();
            entity.Property(x => x.DifferentialIcd10Codes).HasMaxLength(500).IsRequired();
            entity.Property(x => x.InitialDifferentialDiagnosis).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ChiefComplaint).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.PresentIllness).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.PhysicalExam).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.TemperatureCelsius).HasPrecision(5, 2);
            entity.Property(x => x.WeightKilograms).HasPrecision(6, 2);
            entity.Property(x => x.HeightCentimeters).HasPrecision(6, 2);
            entity.Property(x => x.BodyMassIndex).HasPrecision(6, 2);
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

        builder.Entity<Icd10Code>(entity =>
        {
            entity.HasKey(x => x.Code);
            entity.Property(x => x.Code).HasMaxLength(10);
            entity.Property(x => x.DisplayCode).HasMaxLength(12).IsRequired();
            entity.Property(x => x.ThaiName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.EnglishName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SearchTerms).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Version).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ThaiNameVersion).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ChapterCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ChapterTitle).HasMaxLength(300).IsRequired();
            entity.Property(x => x.BlockCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.BlockTitle).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ParentCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.ThaiName);
            entity.HasIndex(x => new { x.IsActive, x.IsTerminal, x.Code });
        });

        builder.Entity<ClinicDrug>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.GenericName, x.Strength, x.DosageForm });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.GenericName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ManufacturerName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.TradeName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Strength).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DosageForm).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(50).IsRequired();
            entity.Property(x => x.TmtCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.RegistrationNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ApprovalStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        });

        builder.Entity<IcdDrugProtocol>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.Icd10Code, x.ClinicDrugId });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Icd10Code).HasMaxLength(10).IsRequired();
            entity.Property(x => x.DiagnosisType).HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.Drug).WithMany(x => x.Protocols).HasForeignKey(x => x.ClinicDrugId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DrugAdviceTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.Icd10Code, x.DisplayOrder });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Icd10Code).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Text).HasMaxLength(1000).IsRequired();
        });

        builder.Entity<DrugKnowledgeAuditLog>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.CreatedAtUtc });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<WoundCareRecord>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.PatientId, x.VisitDate });
            entity.HasIndex(x => new { x.ClinicCode, x.AuthenticationCode });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CitizenId).HasMaxLength(13).IsRequired();
            entity.Property(x => x.AuthenticationCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.WoundCause).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.WoundLocation).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.TemperatureCelsius).HasPrecision(5, 2);
            entity.Property(x => x.OriginalDocumentData).HasColumnType("varbinary(max)");
            entity.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WoundCarePhoto>(entity =>
        {
            entity.HasIndex(x => new { x.WoundCareRecordId, x.SequenceNo }).IsUnique();
            entity.Property(x => x.ImageData).HasColumnType("varbinary(max)").IsRequired();
            entity.HasOne(x => x.WoundCareRecord).WithMany(x => x.Photos).HasForeignKey(x => x.WoundCareRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PublicAnnouncement>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.BadgeText).HasMaxLength(40).IsRequired();
            entity.Property(x => x.LinkUrl).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.IsPublished, x.DisplayOrder, x.PublishedAtUtc });
        });

        builder.Entity<CreditRequest>(entity =>
        {
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ContactName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AdminNote).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CompletedByUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.ClinicCode, x.Status, x.CreatedAtUtc });
        });

        builder.Entity<QuotaTransaction>(entity =>
        {
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.ClinicCode, x.CreatedAtUtc });
        });

        builder.Entity<PromotionalMedia>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.MediaType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Placement).HasMaxLength(20).IsRequired();
            entity.Property(x => x.MediaUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.PosterUrl).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.IsPublished, x.Placement, x.DisplayOrder });
        });

        builder.Entity<PaymentSlip>(entity =>
        {
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ContactName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.TransferBank).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TransferReference).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SlipData).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ClinicNote).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AdminNote).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ProcessedByUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.ClinicCode, x.Status, x.CreatedAtUtc });
        });

        builder.Entity<AdminMessage>(entity =>
        {
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ContactName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AdminReply).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.RepliedByUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.ClinicCode, x.Status, x.CreatedAtUtc });
        });

        builder.Entity<AdminAuditLog>(entity =>
        {
            entity.Property(x => x.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.ClinicCode, x.CreatedAtUtc });
        });

        builder.Entity<MedicationLabelTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.ClinicCode, x.DiseaseCategory, x.TemplateName });
            entity.Property(x => x.ClinicCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.DiseaseCategory).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TemplateName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.MedicineName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.DoseAmount).HasMaxLength(50).IsRequired();
            entity.Property(x => x.FrequencyPerDay).HasMaxLength(50).IsRequired();
            entity.Property(x => x.MealTiming).HasMaxLength(30).IsRequired();
            entity.Property(x => x.IntervalHours).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AdditionalAdvice).HasMaxLength(500).IsRequired();
        });
    }
}
