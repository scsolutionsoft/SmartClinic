using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class SystemAdminController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var clinics = await BuildClinicItems(db.Clinics.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(8));
        return View(new SystemAdminDashboardViewModel
        {
            TotalClinics = await db.Clinics.CountAsync(),
            ActiveClinics = await db.Clinics.CountAsync(x => x.Status == "Active"),
            UnlimitedClinics = await db.Clinics.CountAsync(x => x.HasUnlimitedOpdRecords),
            TotalPatients = await db.Patients.CountAsync(),
            PendingSlips = await db.PaymentSlips.CountAsync(x => x.Status == "Pending"),
            PendingAmount = await db.PaymentSlips.Where(x => x.Status == "Pending").SumAsync(x => (decimal?)x.Amount) ?? 0,
            NewMessages = await db.AdminMessages.CountAsync(x => x.Status == "New"),
            Clinics = clinics,
            RecentSlips = await db.PaymentSlips.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(6).ToListAsync(),
            RecentMessages = await db.AdminMessages.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(6).ToListAsync(),
            RecentActivity = await db.AdminAuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(10).ToListAsync()
        });
    }

    public async Task<IActionResult> Clinics(string? search, string? status)
    {
        var query = db.Clinics.AsNoTracking();
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term)) query = query.Where(x => x.ClinicCode.Contains(term) || x.ClinicName.Contains(term) || x.FullName.Contains(term) || x.PhoneNumber.Contains(term) || x.Email.Contains(term));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        return View(new SystemClinicsViewModel { Search = term ?? string.Empty, Status = status ?? string.Empty, Clinics = await BuildClinicItems(query.OrderByDescending(x => x.CreatedAtUtc)) });
    }

    public async Task<IActionResult> Clinic(int id)
    {
        var clinic = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (clinic is null) return NotFound();
        var item = (await BuildClinicItems(db.Clinics.AsNoTracking().Where(x => x.Id == id))).Single();
        ViewBag.Users = await users.Users.AsNoTracking().Where(x => x.ClinicCode == clinic.ClinicCode).OrderBy(x => x.FullName).ToListAsync();
        ViewBag.Slips = await db.PaymentSlips.AsNoTracking().Where(x => x.ClinicCode == clinic.ClinicCode).OrderByDescending(x => x.CreatedAtUtc).Take(20).ToListAsync();
        ViewBag.Messages = await db.AdminMessages.AsNoTracking().Where(x => x.ClinicCode == clinic.ClinicCode).OrderByDescending(x => x.CreatedAtUtc).Take(20).ToListAsync();
        ViewBag.Audit = await db.AdminAuditLogs.AsNoTracking().Where(x => x.ClinicCode == clinic.ClinicCode).OrderByDescending(x => x.CreatedAtUtc).Take(30).ToListAsync();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClinicStatus(int id, string status, string? note)
    {
        var allowed = new[] { "Pending", "Active", "Suspended", "Expired", "Rejected", "Closed" };
        var clinic = await db.Clinics.FindAsync(id); var user = await users.GetUserAsync(User);
        if (clinic is null || user is null) return NotFound();
        var previous = clinic.Status; clinic.Status = allowed.Contains(status) ? status : clinic.Status; clinic.LastReviewedAtUtc = DateTime.UtcNow; clinic.LastReviewedByUserId = user.Id;
        AddAudit(user.Id, "ClinicStatusChanged", "Clinic", clinic.Id.ToString(), clinic.ClinicCode, $"{previous} → {clinic.Status}. {note}" );
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Clinic), new { id });
    }

    public async Task<IActionResult> Slips(string? status, string? search)
    {
        var query = db.PaymentSlips.AsNoTracking(); var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(term)) query = query.Where(x => x.ClinicCode.Contains(term) || x.ContactName.Contains(term) || x.TransferReference.Contains(term));
        var slips = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        var clinicCodes = slips.Select(x => x.ClinicCode).Distinct().ToList();
        var clinics = await db.Clinics.AsNoTracking().Where(x => clinicCodes.Contains(x.ClinicCode)).ToDictionaryAsync(x => x.ClinicCode);
        var counts = await db.TreatmentRecords.AsNoTracking().Where(x => clinicCodes.Contains(x.ClinicCode)).GroupBy(x => x.ClinicCode).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return View(new SlipReportViewModel
        {
            Status = status ?? string.Empty, Search = term ?? string.Empty, TotalAmount = slips.Sum(x => x.Amount),
            Items = slips.Select(x => { clinics.TryGetValue(x.ClinicCode, out var c); return new PaymentSlipReportItemViewModel { Slip = x, ClinicName = c?.ClinicName ?? x.ClinicCode, UsedPatients = counts.GetValueOrDefault(x.ClinicCode), PatientLimit = c?.OpdRecordLimit ?? 0, IsUnlimited = c?.HasUnlimitedOpdRecords ?? false }; }).ToList()
        });
    }

    public async Task<IActionResult> SlipFile(int id)
    {
        var slip = await db.PaymentSlips.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return slip is null ? NotFound() : File(slip.SlipData, slip.ContentType, enableRangeProcessing: true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessSlip(int id, string decision, int credits, bool unlimited, string? adminNote)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var slip = await db.PaymentSlips.FirstOrDefaultAsync(x => x.Id == id); var user = await users.GetUserAsync(User);
        if (slip is null || user is null) return NotFound();
        if (slip.Status != "Pending") { TempData["AdminError"] = "สลิปนี้ได้รับการดำเนินการแล้ว"; return RedirectToAction(nameof(Slips)); }
        var clinic = await db.Clinics.FirstOrDefaultAsync(x => x.ClinicCode == slip.ClinicCode);
        if (clinic is null) return NotFound();
        if (decision == "Approve")
        {
            var previousLimit = clinic.OpdRecordLimit; var previousUnlimited = clinic.HasUnlimitedOpdRecords;
            clinic.OpdRecordLimit = unlimited ? clinic.OpdRecordLimit : checked(clinic.OpdRecordLimit + Math.Max(0, credits)); clinic.HasUnlimitedOpdRecords = unlimited;
            slip.Status = "Approved";
            db.QuotaTransactions.Add(new QuotaTransaction { ClinicCode = clinic.ClinicCode, PreviousLimit = previousLimit, Adjustment = clinic.OpdRecordLimit - previousLimit, NewLimit = clinic.OpdRecordLimit, PreviousUnlimited = previousUnlimited, NewUnlimited = unlimited, Reason = $"อนุมัติสลิป #{slip.Id}: {adminNote}", CreatedByUserId = user.Id });
            AddAudit(user.Id, "PaymentSlipApproved", "PaymentSlip", slip.Id.ToString(), clinic.ClinicCode, $"จำนวน {slip.Amount:N2} บาท, เครดิต +{credits}, Unlimited={unlimited}");
        }
        else { slip.Status = "Rejected"; AddAudit(user.Id, "PaymentSlipRejected", "PaymentSlip", slip.Id.ToString(), clinic.ClinicCode, adminNote ?? string.Empty); }
        slip.AdminNote = adminNote?.Trim() ?? string.Empty; slip.ProcessedByUserId = user.Id; slip.ProcessedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); await transaction.CommitAsync(); return RedirectToAction(nameof(Slips));
    }

    public async Task<IActionResult> Messages(string? status)
    {
        var query = db.AdminMessages.AsNoTracking(); if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        ViewBag.Status = status ?? string.Empty; return View(await query.OrderBy(x => x.Status == "New" ? 0 : 1).ThenByDescending(x => x.CreatedAtUtc).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyMessage(int id, string status, string adminReply)
    {
        var message = await db.AdminMessages.FindAsync(id); var user = await users.GetUserAsync(User); if (message is null || user is null) return NotFound();
        message.Status = new[] { "New", "InProgress", "Replied", "Closed" }.Contains(status) ? status : "InProgress"; message.AdminReply = adminReply?.Trim() ?? string.Empty; message.RepliedByUserId = user.Id; message.RepliedAtUtc = DateTime.UtcNow;
        AddAudit(user.Id, "AdminMessageUpdated", "AdminMessage", message.Id.ToString(), message.ClinicCode, message.Status); await db.SaveChangesAsync(); return RedirectToAction(nameof(Messages));
    }

    private async Task<List<SystemClinicItemViewModel>> BuildClinicItems(IQueryable<Clinic> query)
    {
        var clinics = await query.ToListAsync(); var codes = clinics.Select(x => x.ClinicCode).ToList();
        var counts = await db.TreatmentRecords.AsNoTracking().Where(x => codes.Contains(x.ClinicCode)).GroupBy(x => x.ClinicCode).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return clinics.Select(x => new SystemClinicItemViewModel { Id = x.Id, ClinicCode = x.ClinicCode, ClinicName = x.ClinicName, ContactName = x.FullName, PhoneNumber = x.PhoneNumber, Email = x.Email, Status = x.Status, UsedPatients = counts.GetValueOrDefault(x.ClinicCode), PatientLimit = x.OpdRecordLimit, IsUnlimited = x.HasUnlimitedOpdRecords, CreatedAtUtc = x.CreatedAtUtc }).ToList();
    }

    private void AddAudit(string actor, string action, string type, string id, string clinic, string detail) => db.AdminAuditLogs.Add(new AdminAuditLog { ActorUserId = actor, Action = action, EntityType = type, EntityId = id, ClinicCode = clinic, Detail = detail });
}
