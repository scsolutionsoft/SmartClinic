using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;
using System.Text.RegularExpressions;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class DrugAdviceController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q = null, string? icd = null, string? category = null)
    {
        var clinicCode = await GetClinicCode();
        var term = Clean(q);
        var code = NormalizeCode(icd);
        var selectedCategory = Clean(category);
        var all = await db.DrugAdviceTemplates.AsNoTracking().Where(x => x.ClinicCode == clinicCode).ToListAsync();
        var query = all.AsEnumerable();
        if (term.Length > 0) query = query.Where(x => x.Text.Contains(term, StringComparison.OrdinalIgnoreCase));
        if (code.Length > 0) query = query.Where(x => x.Icd10Code == code);
        if (selectedCategory.Length > 0) query = query.Where(x => x.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase));
        var items = query.OrderBy(x => x.Icd10Code).ThenBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToList();
        foreach (var item in items) item.Text = AdviceOnly(item.Text);
        items = items.Where(x => x.Text.Length >= 8).ToList();
        var codes = items.Select(x => x.Icd10Code).Where(x => x.Length > 0).Distinct().ToList();
        var names = await db.Icd10Codes.AsNoTracking().Where(x => x.IsActive && codes.Contains(x.Code)).ToDictionaryAsync(x => x.Code, x => x.ThaiName != "" ? x.ThaiName : x.EnglishName);
        return View(new DrugAdviceDashboardViewModel
        {
            Items = items,
            Icd10ThaiNames = names,
            RecentAudit = await db.DrugKnowledgeAuditLogs.AsNoTracking().Where(x => x.ClinicCode == clinicCode && x.EntityType == "Advice").OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(),
            SearchTerm = term,
            Icd10Code = code,
            Category = selectedCategory,
            ActiveCount = all.Count(x => x.IsActive),
            WarningCount = all.Count(x => x.IsActive && x.Category == "Warning"),
            FollowUpCount = all.Count(x => x.IsActive && x.Category == "FollowUp")
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> Save(DrugAdviceInputViewModel input)
    {
        var clinicCode = await GetClinicCode();
        var code = NormalizeCode(input.Icd10Code);
        if (!await db.Icd10Codes.AsNoTracking().AnyAsync(x => x.IsActive && x.IsTerminal && x.Code == code))
        {
            TempData["DrugCatalogError"] = $"ไม่พบรหัส ICD-10 {code} หรือรหัสดังกล่าวเป็นเพียงหัวข้อหมวด";
            return RedirectToAction(nameof(Index), new { icd = code });
        }
        var entity = input.Id.HasValue ? await db.DrugAdviceTemplates.FirstOrDefaultAsync(x => x.Id == input.Id && x.ClinicCode == clinicCode) : null;
        var action = entity is null ? "Create" : "Update";
        if (entity is null) { entity = new DrugAdviceTemplate { ClinicCode = clinicCode }; db.Add(entity); }
        entity.Icd10Code = code;
        entity.Category = Clean(input.Category) is { Length: > 0 } value ? value : "General";
        entity.Text = Clean(input.Text);
        entity.DisplayOrder = input.DisplayOrder;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await Audit(clinicCode, entity.Id, action, $"{entity.Icd10Code}: {entity.Text}");
        return RedirectToAction(nameof(Index), new { icd = entity.Icd10Code });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> Toggle(int id)
    {
        var clinicCode = await GetClinicCode();
        var entity = await db.DrugAdviceTemplates.FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (entity is null) return NotFound();
        entity.IsActive = !entity.IsActive; entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); await Audit(clinicCode, id, entity.IsActive ? "Activate" : "Deactivate", entity.Text);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> Delete(int id)
    {
        var clinicCode = await GetClinicCode();
        var entity = await db.DrugAdviceTemplates.FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (entity is null) return NotFound();
        var detail = $"{entity.Icd10Code}: {entity.Text}";
        db.Remove(entity); await db.SaveChangesAsync(); await Audit(clinicCode, id, "Delete", detail);
        return RedirectToAction(nameof(Index));
    }

    private async Task Audit(string clinicCode, int id, string action, string detail)
    {
        var user = await users.GetUserAsync(User);
        db.DrugKnowledgeAuditLogs.Add(new DrugKnowledgeAuditLog { ClinicCode = clinicCode, EntityType = "Advice", EntityId = id, Action = action, Detail = detail, UserId = user?.Id ?? "", UserName = user?.FullName ?? user?.UserName ?? "" });
        await db.SaveChangesAsync();
    }

    private async Task<string> GetClinicCode() { var user = await users.GetUserAsync(User); return user?.ClinicCode ?? "SMARTCLINIC"; }
    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
    private static string NormalizeCode(string? value) => Clean(value).Replace(".", "").ToUpperInvariant();
    private static string AdviceOnly(string? source)
    {
        var text = Regex.Replace(source ?? "", @"\s+", " ").Trim();
        var firstMedicine = Regex.Match(text, @"(?<!\d)\d{1,3}\s*[.)]\s*(?=[A-Za-zก-๙])");
        return (firstMedicine.Success ? text[..firstMedicine.Index] : text).Trim(' ', '-', '.', ';', ':');
    }
}
