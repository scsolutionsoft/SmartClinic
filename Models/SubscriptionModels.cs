namespace SmartClinic.Web.Models;

public class PublicAnnouncement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string BadgeText { get; set; } = "ข่าวสาร";
    public string LinkUrl { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CreditRequest
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int? RequestedCredits { get; set; }
    public bool RequestUnlimited { get; set; }
    public string Note { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string AdminNote { get; set; } = string.Empty;
    public string? CompletedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class QuotaTransaction
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public int PreviousLimit { get; set; }
    public int Adjustment { get; set; }
    public int NewLimit { get; set; }
    public bool PreviousUnlimited { get; set; }
    public bool NewUnlimited { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PromotionalMedia
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Video";
    public string Placement { get; set; } = "Board";
    public string MediaUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public bool AutoPlay { get; set; }
    public bool Loop { get; set; }
    public bool IsPublished { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PaymentSlip
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int RequestedCredits { get; set; }
    public bool RequestUnlimited { get; set; }
    public DateTime TransferDateTime { get; set; }
    public string TransferBank { get; set; } = string.Empty;
    public string TransferReference { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] SlipData { get; set; } = [];
    public string Status { get; set; } = "Pending";
    public string ClinicNote { get; set; } = string.Empty;
    public string AdminNote { get; set; } = string.Empty;
    public string? ProcessedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
}

public class AdminMessage
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string AdminReply { get; set; } = string.Empty;
    public string? RepliedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RepliedAtUtc { get; set; }
}

public class AdminAuditLog
{
    public long Id { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ClinicCode { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
