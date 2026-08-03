using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class DrugCatalogController(ApplicationDbContext db, UserManager<ApplicationUser> users, IHttpClientFactory httpClients, IConfiguration configuration) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q = null, string? icd = null, int page = 1)
    {
        var clinicCode = await GetClinicCode();
        var term = Clean(q);
        var code = NormalizeCode(icd);
        var query = db.ClinicDrugs.AsNoTracking().Include(x => x.Protocols.Where(p => p.IsActive)).Where(x => x.ClinicCode == clinicCode);
        if (term.Length > 0) query = query.Where(x => x.GenericName.Contains(term) || x.TradeName.Contains(term) || x.ManufacturerName.Contains(term) || x.TmtCode.Contains(term) || x.RegistrationNumber.Contains(term));
        if (code.Length > 0) query = query.Where(x => x.Protocols.Any(p => p.IsActive && p.Icd10Code == code));
        var all = await db.ClinicDrugs.AsNoTracking().Where(x => x.ClinicCode == clinicCode).ToListAsync();
        var duplicateCount = all.GroupBy(x => new { Name = x.GenericName.ToUpper(), Strength = x.Strength.ToUpper(), Form = x.DosageForm.ToUpper(), Manufacturer = x.ManufacturerName.ToUpper() }).Count(x => x.Count() > 1);
        var adviceTemplates = await db.DrugAdviceTemplates.AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && (code == "" || x.Icd10Code == code))
            .OrderBy(x => x.Icd10Code).ThenBy(x => x.DisplayOrder).ToListAsync();
        foreach (var template in adviceTemplates) template.Text = AdviceOnly(template.Text);
        adviceTemplates = adviceTemplates.Where(x => x.Text.Length >= 8).ToList();
        const int pageSize = 10;
        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var drugs = await query.OrderByDescending(x => x.IsActive).ThenBy(x => x.GenericName)
            .ThenBy(x => x.ManufacturerName).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var diseaseLinks = await db.IcdDrugProtocols.AsNoTracking().Include(x => x.Drug)
            .Where(x => x.ClinicCode == clinicCode && x.IsActive && x.Drug.IsActive)
            .OrderBy(x => x.Icd10Code).ThenBy(x => x.DisplayOrder).ThenBy(x => x.Drug.GenericName).ToListAsync();
        var reportCodes = diseaseLinks.Select(x => x.Icd10Code).Distinct().ToList();
        var reportDiseases = await db.Icd10Codes.AsNoTracking().Where(x => x.IsActive && reportCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code);
        var diseaseDrugReports = diseaseLinks.GroupBy(x => x.Icd10Code).Select(group =>
        {
            reportDiseases.TryGetValue(group.Key, out var disease);
            return new IcdDiseaseDrugReportViewModel
            {
                Icd10Code = group.Key,
                ThaiName = !string.IsNullOrWhiteSpace(disease?.ThaiName) ? disease.ThaiName : disease?.EnglishName ?? "Disease name unavailable",
                EnglishName = disease?.EnglishName ?? "English description unavailable",
                Drugs = group.GroupBy(x => x.ClinicDrugId).Select(drugGroup => drugGroup.First().Drug).Select(drug => new IcdDiseaseDrugItemViewModel
                {
                    DrugId = drug.Id, GenericName = drug.GenericName, TradeName = drug.TradeName,
                    ManufacturerName = drug.ManufacturerName, Strength = drug.Strength, DosageForm = drug.DosageForm,
                    UsageText = drug.UsageText
                }).OrderBy(x => x.GenericName).ThenBy(x => x.ManufacturerName).ToList()
            };
        }).OrderBy(x => x.Icd10Code).ToList();
        var displayedIcdCodes = adviceTemplates.Select(x => x.Icd10Code)
            .Concat(drugs.SelectMany(x => x.Protocols).Select(x => x.Icd10Code))
            .Where(x => x.Length > 0).Distinct().ToList();
        var displayedIcdNames = await db.Icd10Codes.AsNoTracking().Where(x => x.IsActive && displayedIcdCodes.Contains(x.Code)).ToDictionaryAsync(x => x.Code, x => x.ThaiName != "" ? x.ThaiName : x.EnglishName);
        return View(new DrugCatalogDashboardViewModel
        {
            Drugs = drugs,
            DiseaseDrugReports = diseaseDrugReports,
            AdviceTemplates = adviceTemplates,
            Icd10ThaiNames = displayedIcdNames,
            ManufacturerNames = all.Select(x => x.ManufacturerName).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
            RecentAudit = await db.DrugKnowledgeAuditLogs.AsNoTracking().Where(x => x.ClinicCode == clinicCode && (x.EntityType == "Drug" || x.EntityType == "Protocol")).OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync(),
            SearchTerm = term, Icd10Code = code, ActiveDrugCount = all.Count(x => x.IsActive), PendingCount = all.Count(x => x.ApprovalStatus == "Pending"), DuplicateCandidateCount = duplicateCount,
            Page = page, PageSize = pageSize, TotalCount = totalCount
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> SaveDrug(ClinicDrugInputViewModel input)
    {
        var clinicCode = await GetClinicCode();
        var name = Clean(input.GenericName);
        var strength = Clean(input.Strength);
        var form = Clean(input.DosageForm);
        if (name.Length == 0) return RedirectToAction(nameof(Index));
        var manufacturer = Clean(input.ManufacturerName);
        var duplicate = await db.ClinicDrugs.AsNoTracking().AnyAsync(x => x.ClinicCode == clinicCode && x.Id != input.Id && x.GenericName == name && x.Strength == strength && x.DosageForm == form && x.ManufacturerName == manufacturer);
        if (duplicate) { TempData["DrugCatalogError"] = "พบชื่อสามัญ ความแรง รูปแบบยา และบริษัทผู้ผลิตซ้ำในคลัง"; return RedirectToAction(nameof(Index), new { q = name }); }
        var entity = input.Id.HasValue ? await db.ClinicDrugs.FirstOrDefaultAsync(x => x.Id == input.Id && x.ClinicCode == clinicCode) : null;
        var action = entity is null ? "Create" : "Update";
        if (entity is null) { entity = new ClinicDrug { ClinicCode = clinicCode, CreatedByUserId = users.GetUserId(User) ?? string.Empty }; db.ClinicDrugs.Add(entity); }
        entity.GenericName=name; entity.TradeName=Clean(input.TradeName); entity.ManufacturerName=manufacturer; entity.Strength=strength; entity.DosageForm=form; entity.Unit=Clean(input.Unit); entity.TmtCode=Clean(input.TmtCode); entity.RegistrationNumber=Clean(input.RegistrationNumber); entity.Source=Clean(input.Source) is { Length: > 0 } source ? source : "Clinic"; entity.DoseAmount=Clean(input.DoseAmount); entity.FrequencyPerDay=Clean(input.FrequencyPerDay); entity.MealTiming=Clean(input.MealTiming); entity.Morning=input.Morning; entity.Noon=input.Noon; entity.Evening=input.Evening; entity.Bedtime=input.Bedtime; entity.IntervalHours=Clean(input.IntervalHours); entity.UsageText=Clean(input.UsageText); entity.AdviceText=Clean(input.AdviceText); entity.ApprovalStatus="Approved"; entity.UpdatedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(); await Audit(clinicCode,"Drug",entity.Id,action,$"{entity.GenericName} {entity.Strength} {entity.DosageForm}");
        return RedirectToAction(nameof(Index), new { q = entity.GenericName });
    }

    [HttpGet, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> EditDrugDetails(int id)
    {
        var clinicCode = await GetClinicCode();
        var entity = await db.ClinicDrugs.AsNoTracking().Include(x => x.Protocols)
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (entity is null) return NotFound();
        var codes = entity.Protocols.Select(x => x.Icd10Code).Distinct().ToList();
        var diseaseNames = await db.Icd10Codes.AsNoTracking().Where(x => x.IsActive && codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, x => x.ThaiName != "" ? x.ThaiName : x.EnglishName);
        return Json(new
        {
            entity.Id,
            entity.GenericName,
            entity.TradeName,
            entity.ManufacturerName,
            entity.Strength,
            entity.DosageForm,
            entity.Unit,
            entity.TmtCode,
            entity.RegistrationNumber,
            entity.Source,
            entity.DoseAmount,
            entity.FrequencyPerDay,
            entity.MealTiming,
            entity.Morning,
            entity.Noon,
            entity.Evening,
            entity.Bedtime,
            entity.IntervalHours,
            entity.UsageText,
            entity.AdviceText,
            entity.ApprovalStatus,
            Protocols = entity.Protocols.Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => new
            {
                x.Id,
                x.Icd10Code,
                DiseaseName = diseaseNames.GetValueOrDefault(x.Icd10Code, "ไม่พบชื่อโรคภาษาไทย"),
                x.DiagnosisType,
                x.DisplayOrder
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> SaveDrugDetails(ClinicDrugDetailsInputViewModel input)
    {
        var clinicCode = await GetClinicCode();
        if (!input.Id.HasValue) return BadRequest();
        var entity = await db.ClinicDrugs.Include(x => x.Protocols)
            .FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.ClinicCode == clinicCode);
        if (entity is null) return NotFound();

        var name = Clean(input.GenericName);
        var strength = Clean(input.Strength);
        var form = Clean(input.DosageForm);
        var manufacturer = Clean(input.ManufacturerName);
        if (name.Length == 0) return RedirectToAction(nameof(Index));
        var duplicate = await db.ClinicDrugs.AsNoTracking().AnyAsync(x => x.ClinicCode == clinicCode && x.Id != entity.Id
            && x.GenericName == name && x.Strength == strength && x.DosageForm == form && x.ManufacturerName == manufacturer);
        if (duplicate)
        {
            TempData["DrugCatalogError"] = "พบชื่อสามัญ ความแรง รูปแบบยา และบริษัทผู้ผลิตซ้ำในคลัง";
            return RedirectToAction(nameof(Index), new { q = name });
        }

        entity.GenericName = name;
        entity.TradeName = Clean(input.TradeName);
        entity.ManufacturerName = manufacturer;
        entity.Strength = strength;
        entity.DosageForm = form;
        entity.Unit = Clean(input.Unit);
        entity.TmtCode = Clean(input.TmtCode);
        entity.RegistrationNumber = Clean(input.RegistrationNumber);
        entity.Source = Clean(input.Source) is { Length: > 0 } source ? source : "Clinic";
        entity.DoseAmount = Clean(input.DoseAmount);
        entity.FrequencyPerDay = Clean(input.FrequencyPerDay);
        entity.MealTiming = Clean(input.MealTiming);
        entity.Morning = input.Morning;
        entity.Noon = input.Noon;
        entity.Evening = input.Evening;
        entity.Bedtime = input.Bedtime;
        entity.IntervalHours = Clean(input.IntervalHours);
        entity.UsageText = Clean(input.UsageText);
        entity.AdviceText = Clean(input.AdviceText);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        var postedProtocols = input.Protocols.Where(x => x.Id.HasValue)
            .GroupBy(x => x.Id!.Value).ToDictionary(x => x.Key, x => x.First());
        foreach (var protocol in entity.Protocols)
        {
            if (!postedProtocols.TryGetValue(protocol.Id, out var posted)) continue;
            var postedCode = NormalizeCode(posted.Icd10Code);
            if (postedCode.Length >= 3) protocol.Icd10Code = postedCode;
            protocol.DiagnosisType = Clean(posted.DiagnosisType);
            protocol.DisplayOrder = posted.DisplayOrder;
            protocol.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        await Audit(clinicCode, "Drug", entity.Id, "Update", $"{entity.GenericName} {entity.Strength} · แก้ไขข้อมูลยา วิธีใช้ และความสัมพันธ์ ICD-10 {postedProtocols.Count} รายการ");
        TempData["DrugCatalogSuccess"] = $"บันทึกการแก้ไข {entity.GenericName} เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index), new { q = entity.GenericName });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> ToggleDrug(int id)
    {
        var clinicCode=await GetClinicCode(); var entity=await db.ClinicDrugs.FirstOrDefaultAsync(x=>x.Id==id&&x.ClinicCode==clinicCode); if(entity is null)return NotFound(); entity.IsActive=!entity.IsActive;entity.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();await Audit(clinicCode,"Drug",id,entity.IsActive?"Activate":"Deactivate",entity.GenericName);return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> DeleteDrug(int id)
    {
        var clinicCode = await GetClinicCode();
        var entity = await db.ClinicDrugs.Include(x => x.Protocols).FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (entity is null) return NotFound();
        var detail = $"{entity.GenericName} {entity.Strength} {entity.DosageForm} · บริษัท {entity.ManufacturerName} · ลบความสัมพันธ์ ICD-10 {entity.Protocols.Count} รายการ";
        db.IcdDrugProtocols.RemoveRange(entity.Protocols);
        db.ClinicDrugs.Remove(entity);
        await db.SaveChangesAsync();
        await Audit(clinicCode, "Drug", id, "Delete", detail);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> SaveProtocol(IcdDrugProtocolInputViewModel input)
    {
        var clinicCode = await GetClinicCode();
        if (!await db.ClinicDrugs.AnyAsync(x => x.Id == input.ClinicDrugId && x.ClinicCode == clinicCode)) return NotFound();
        var code = NormalizeCode(input.Icd10Code);
        if (!await db.Icd10Codes.AsNoTracking().AnyAsync(x => x.IsActive && x.IsTerminal && x.Code == code))
        {
            TempData["DrugCatalogError"] = $"ไม่พบรหัส ICD-10 {code} กรุณาเลือกจากรายการค้นหา";
            return RedirectToAction(nameof(Index));
        }
        var entity = input.Id.HasValue
            ? await db.IcdDrugProtocols.FirstOrDefaultAsync(x => x.Id == input.Id && x.ClinicCode == clinicCode)
            : await db.IcdDrugProtocols.FirstOrDefaultAsync(x => x.ClinicCode == clinicCode && x.ClinicDrugId == input.ClinicDrugId && x.Icd10Code == code);
        var action = entity is null ? "Create" : entity.IsActive ? "Update" : "Reactivate";
        if (entity is null) { entity = new IcdDrugProtocol { ClinicCode = clinicCode }; db.Add(entity); }
        entity.ClinicDrugId = input.ClinicDrugId; entity.Icd10Code = code; entity.DiagnosisType = Clean(input.DiagnosisType); entity.DisplayOrder = input.DisplayOrder; entity.IsActive = true; entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); await Audit(clinicCode, "Protocol", entity.Id, action, $"{entity.Icd10Code} / Drug {entity.ClinicDrugId}");
        TempData["DrugCatalogSuccess"] = $"เชื่อมยาเข้ากับโรค {code} เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index), new { q = (string?)null });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> DeleteProtocol(int id)
    { var clinicCode=await GetClinicCode();var entity=await db.IcdDrugProtocols.FirstOrDefaultAsync(x=>x.Id==id&&x.ClinicCode==clinicCode);if(entity is not null){db.Remove(entity);await db.SaveChangesAsync();await Audit(clinicCode,"Protocol",id,"Delete",entity.Icd10Code);}return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> SaveAdvice(DrugAdviceInputViewModel input)
    { var clinicCode=await GetClinicCode();var entity=input.Id.HasValue?await db.DrugAdviceTemplates.FirstOrDefaultAsync(x=>x.Id==input.Id&&x.ClinicCode==clinicCode):null;var action=entity is null?"Create":"Update";if(entity is null){entity=new DrugAdviceTemplate{ClinicCode=clinicCode};db.Add(entity);}entity.Icd10Code=NormalizeCode(input.Icd10Code);entity.Category=Clean(input.Category);entity.Text=Clean(input.Text);entity.DisplayOrder=input.DisplayOrder;entity.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();await Audit(clinicCode,"Advice",entity.Id,action,$"{entity.Icd10Code}: {entity.Text}");return RedirectToAction(nameof(Index),new{icd=entity.Icd10Code}); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> ToggleAdvice(int id)
    {var clinicCode=await GetClinicCode();var entity=await db.DrugAdviceTemplates.FirstOrDefaultAsync(x=>x.Id==id&&x.ClinicCode==clinicCode);if(entity is null)return NotFound();entity.IsActive=!entity.IsActive;entity.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();await Audit(clinicCode,"Advice",id,entity.IsActive?"Activate":"Deactivate",entity.Text);return RedirectToAction(nameof(Index));}

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> DeleteAdvice(int id)
    {
        var clinicCode = await GetClinicCode();
        var entity = await db.DrugAdviceTemplates.FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (entity is null) return NotFound();
        var detail = $"{entity.Icd10Code}: {entity.Text}";
        db.DrugAdviceTemplates.Remove(entity);
        await db.SaveChangesAsync();
        await Audit(clinicCode, "Advice", id, "Delete", detail);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string? icdCodes)
    {
        var clinicCode=await GetClinicCode();var codes=(icdCodes??"").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(NormalizeCode).Where(x=>x.Length>=3).Distinct().Take(20).ToList();if(codes.Count==0)return Json(new{drugs=Array.Empty<object>(),advice=Array.Empty<object>()});
        var protocols=await db.IcdDrugProtocols.AsNoTracking().Include(x=>x.Drug).Where(x=>x.ClinicCode==clinicCode&&x.IsActive&&codes.Contains(x.Icd10Code)&&x.Drug.IsActive&&x.Drug.ApprovalStatus=="Approved").OrderBy(x=>x.DisplayOrder).ThenByDescending(x=>x.UsageCount).ToListAsync();
        var advice=await db.DrugAdviceTemplates.AsNoTracking().Where(x=>x.ClinicCode==clinicCode&&x.IsActive&&codes.Contains(x.Icd10Code)).OrderBy(x=>x.DisplayOrder).ToListAsync();
        var groupedDrugs = protocols.GroupBy(x => DrugKey(x.Drug.GenericName, x.Drug.Strength, x.Drug.DosageForm, x.Drug.ManufacturerName)).Select(group =>
        {
            var item = group.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.UsageCount).First();
            return new
            {
                item.Id,
                icd10Code = string.Join(", ", group.Select(x => x.Icd10Code).Distinct().OrderBy(x => x)),
                name = item.Drug.GenericName,
                strength = item.Drug.Strength,
                dosageForm = item.Drug.DosageForm,
                tradeName = item.Drug.TradeName,
                manufacturerName = item.Drug.ManufacturerName,
                UsageText = item.Drug.UsageText,
                AdviceText = item.Drug.AdviceText,
                usageCount = group.Sum(x => x.UsageCount)
            };
        }).OrderBy(x => x.name).ToList();
        var groupedAdvice = advice.GroupBy(x => NormalizeDrugPart(x.Text)).Select(group =>
        {
            var item = group.OrderBy(x => x.DisplayOrder).First();
            return new { item.Id, icd10Code = string.Join(", ", group.Select(x => x.Icd10Code).Distinct().OrderBy(x => x)), item.Category, item.Text };
        }).ToList();
        return Json(new { drugs = groupedDrugs, advice = groupedAdvice });
    }

    [HttpGet, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> PreviewOpdImport(int take = 500)
    {
        var clinicCode = await GetClinicCode();
        var records = await db.TreatmentRecords.AsNoTracking().Where(x => x.ClinicCode == clinicCode && x.TreatmentAndAdvice != "")
            .OrderByDescending(x => x.VisitDate).Take(Math.Clamp(take, 1, 2000))
            .Select(x => new { x.Id, x.PrimaryIcd10Code, x.DifferentialIcd10Codes, x.TreatmentAndAdvice, x.VisitDate }).ToListAsync();
        var existing = await db.ClinicDrugs.AsNoTracking().Include(x => x.Protocols).Where(x => x.ClinicCode == clinicCode).ToListAsync();
        var candidates = BuildImportCandidates(records.Select(x => new LegacyRecord(x.Id, x.PrimaryIcd10Code, x.DifferentialIcd10Codes, x.TreatmentAndAdvice, x.VisitDate)));
        var adviceCandidates = BuildAdviceCandidates(records.Select(x => new LegacyRecord(x.Id, x.PrimaryIcd10Code, x.DifferentialIcd10Codes, x.TreatmentAndAdvice, x.VisitDate)));
        var evaluatedCandidates = candidates.Select(candidate =>
        {
            var drug = existing.FirstOrDefault(item => SameDrug(item.GenericName, item.Strength, item.DosageForm, item.ManufacturerName, candidate.GenericName, candidate.Strength, candidate.DosageForm, candidate.ManufacturerName));
            var linkedCodes = drug?.Protocols.Where(x => x.IsActive).Select(x => NormalizeCode(x.Icd10Code)).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            var missingCodes = candidate.IcdCodes.Where(x => !linkedCodes.Contains(x)).OrderBy(x => x).ToList();
            return new { Candidate = candidate, Drug = drug, LinkedCodes = linkedCodes.OrderBy(x => x).ToList(), MissingCodes = missingCodes };
        }).ToList();
        var parseWarnings = BuildParseWarnings(records.Select(x => new LegacyRecord(x.Id, x.PrimaryIcd10Code, x.DifferentialIcd10Codes, x.TreatmentAndAdvice, x.VisitDate)));
        var allCodes = candidates.SelectMany(x => x.IcdCodes).Concat(adviceCandidates.SelectMany(x => x.IcdCodes)).Distinct().ToList();
        var icdNames = await db.Icd10Codes.AsNoTracking().Where(x => x.IsActive && allCodes.Contains(x.Code)).ToDictionaryAsync(x => x.Code, x => x.ThaiName != "" ? x.ThaiName : x.EnglishName);
        return Json(new
        {
            scannedRecords = records.Count,
            detectedDrugCount = candidates.Count,
            newDrugCount = evaluatedCandidates.Count(x => x.Drug is null),
            existingDrugCount = evaluatedCandidates.Count(x => x.Drug is not null),
            parseWarnings,
            candidates = evaluatedCandidates.Where(x => x.Drug is null).Select(result => new
            {
                result.Candidate.Key, result.Candidate.GenericName, result.Candidate.TradeName, result.Candidate.ManufacturerName, result.Candidate.Strength, result.Candidate.DosageForm,
                result.Candidate.DoseAmount, result.Candidate.FrequencyPerDay, result.Candidate.MealTiming, result.Candidate.Morning, result.Candidate.Noon,
                result.Candidate.Evening, result.Candidate.Bedtime, result.Candidate.IntervalHours, result.Candidate.UsageText, result.Candidate.AdviceText, result.Candidate.RecordCount,
                icdCodes = result.Candidate.IcdCodes.OrderBy(v => v),
                detectedIcdCodes = result.Candidate.IcdCodes.OrderBy(v => v),
                exists = false
            }),
            advice = adviceCandidates.Select(x => new { x.Key, x.Text, x.Category, x.RecordCount, icdCodes = x.IcdCodes.OrderBy(v => v) }),
            icdNames
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> CommitOpdImport([FromBody] OpdImportCommitRequest request)
    {
        var clinicCode = await GetClinicCode();
        var selected = (request.Items ?? []).Where(x => x.Selected && !string.IsNullOrWhiteSpace(x.Key)).ToList();
        var selectedAdvice = (request.Advice ?? []).Where(x => x.Selected && !string.IsNullOrWhiteSpace(x.Text)).ToList();
        if (selected.Count == 0 && selectedAdvice.Count == 0) return BadRequest(new { error = "กรุณาเลือกยา หรือคำแนะนำที่ต้องการนำเข้า" });
        var records = await db.TreatmentRecords.AsNoTracking().Where(x => x.ClinicCode == clinicCode && x.TreatmentAndAdvice != "")
            .OrderByDescending(x => x.VisitDate).Take(2000)
            .Select(x => new { x.Id, x.PrimaryIcd10Code, x.DifferentialIcd10Codes, x.TreatmentAndAdvice, x.VisitDate }).ToListAsync();
        var parsedCandidates = BuildImportCandidates(records.Select(x => new LegacyRecord(x.Id, x.PrimaryIcd10Code, x.DifferentialIcd10Codes, x.TreatmentAndAdvice, x.VisitDate))).ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var allowedAdvice = BuildAdviceCandidates(records.Select(x => new LegacyRecord(x.Id, x.PrimaryIcd10Code, x.DifferentialIcd10Codes, x.TreatmentAndAdvice, x.VisitDate))).Select(x=>x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0; var skipped = 0; var linked = 0; var adviceCreated = 0; var user = await users.GetUserAsync(User);
        var catalogDrugs = await db.ClinicDrugs.Where(x => x.ClinicCode == clinicCode).ToListAsync();
        var requestedOpdCodes = selected.SelectMany(x => parsedCandidates.TryGetValue(x.Key, out var parsed) ? parsed.IcdCodes : [])
            .Select(NormalizeCode).Where(x => x.Length >= 3).Distinct().ToList();
        var validOpdCodes = (await db.Icd10Codes.AsNoTracking().Where(x => x.IsActive && x.IsTerminal && requestedOpdCodes.Contains(x.Code)).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingLinks = await db.IcdDrugProtocols.Where(x => x.ClinicCode == clinicCode).OrderBy(x => x.Id).ToListAsync();
        var existingLinkMap = existingLinks.GroupBy(x => $"{x.ClinicDrugId}|{NormalizeCode(x.Icd10Code)}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var input in selected)
        {
            if (!parsedCandidates.TryGetValue(input.Key, out var original)) continue;
            var name=Clean(input.GenericName);var strength=Clean(input.Strength);var form=Clean(input.DosageForm);var manufacturer=Clean(input.ManufacturerName);if(name.Length==0)continue;
            var drug = catalogDrugs.FirstOrDefault(x => SameDrug(x.GenericName, x.Strength, x.DosageForm, x.ManufacturerName, name, strength, form, manufacturer));
            if (drug is null)
            {
                drug = new ClinicDrug { ClinicCode = clinicCode, GenericName = name, TradeName=Clean(input.TradeName), ManufacturerName=Clean(input.ManufacturerName), Strength = strength, DosageForm = form, Source = "OPD Import", ApprovalStatus = "Approved", CreatedByUserId = user?.Id ?? string.Empty };
                db.ClinicDrugs.Add(drug); await db.SaveChangesAsync(); catalogDrugs.Add(drug); created++;
                db.DrugKnowledgeAuditLogs.Add(new DrugKnowledgeAuditLog { ClinicCode=clinicCode,EntityType="Drug",EntityId=drug.Id,Action="Import",Detail=$"{drug.GenericName} {drug.Strength} จาก OPD {original.RecordCount} รายการ",UserId=user?.Id??"",UserName=user?.FullName??user?.UserName??"" });
            }
            else { skipped++; if (drug.ManufacturerName.Length == 0 && Clean(input.ManufacturerName).Length > 0) drug.ManufacturerName = Clean(input.ManufacturerName); }
            drug.DoseAmount = Clean(input.DoseAmount);
            drug.FrequencyPerDay = Clean(input.FrequencyPerDay);
            drug.MealTiming = Clean(input.MealTiming);
            drug.Morning = input.Morning; drug.Noon = input.Noon; drug.Evening = input.Evening; drug.Bedtime = input.Bedtime;
            drug.IntervalHours = Clean(input.IntervalHours);
            drug.UsageText = Clean(input.UsageText);
            drug.AdviceText = Clean(input.AdviceText);
            drug.UpdatedAtUtc = DateTime.UtcNow;
            // รายการยาที่ผู้ใช้ยืนยันนำเข้าจาก OPD จะผูกรหัสโรคที่ตรวจพบและผ่านการตรวจสอบโดยอัตโนมัติ
            if (original.IcdCodes.Count > 0)
            {
                var originalCodes = original.IcdCodes.Select(NormalizeCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var code in originalCodes.Where(validOpdCodes.Contains))
                {
                    var linkKey = $"{drug.Id}|{code}";
                    if (existingLinkMap.TryGetValue(linkKey, out var existingLink))
                    {
                        if (!existingLink.IsActive) { existingLink.IsActive = true; existingLink.UpdatedAtUtc = DateTime.UtcNow; linked++; }
                        continue;
                    }
                    var newLink = new IcdDrugProtocol { ClinicCode = clinicCode, ClinicDrugId = drug.Id, Icd10Code = code, DiagnosisType = "Primary", IsActive = true };
                    db.IcdDrugProtocols.Add(newLink);
                    existingLinkMap[linkKey] = newLink;
                    linked++;
                }
            }
        }
        foreach(var input in selectedAdvice)
        {
            if(!allowedAdvice.Contains(input.Key))continue;var text=Clean(input.Text);foreach(var code in (input.Icd10Codes??[]).Select(NormalizeCode).Where(x=>x.Length>=3).Distinct())
            {if(await db.DrugAdviceTemplates.AnyAsync(x=>x.ClinicCode==clinicCode&&x.Icd10Code==code&&x.Text==text))continue;db.DrugAdviceTemplates.Add(new DrugAdviceTemplate{ClinicCode=clinicCode,Icd10Code=code,Category=Clean(input.Category) is {Length:>0} category?category:"General",Text=text});adviceCreated++;}
        }
        await db.SaveChangesAsync();
        db.DrugKnowledgeAuditLogs.Add(new DrugKnowledgeAuditLog{ClinicCode=clinicCode,EntityType="ImportBatch",Action="Import",Detail=$"ยาใหม่ {created}, พบยาเดิม {skipped}, ผูก ICD-10 {linked}, คำแนะนำ {adviceCreated}",UserId=user?.Id??"",UserName=user?.FullName??user?.UserName??""});await db.SaveChangesAsync();
        return Json(new { created, linked, skipped, adviceCreated, imported = selected.Count });
    }

    [HttpGet, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> IcdDrugSelection(string? icdCode)
    {
        var clinicCode = await GetClinicCode();
        var code = NormalizeCode(icdCode);
        if (code.Length < 3) return BadRequest(new { error = "กรุณาระบุรหัส ICD-10" });
        var disease = await db.Icd10Codes.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive && x.IsTerminal && x.Code == code);
        var selectedIds = await db.IcdDrugProtocols.AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && x.Icd10Code == code && x.IsActive)
            .Select(x => x.ClinicDrugId).ToListAsync();
        var drugs = await db.ClinicDrugs.AsNoTracking().Where(x => x.ClinicCode == clinicCode && x.IsActive && x.ApprovalStatus == "Approved")
            .OrderBy(x => x.GenericName).ThenBy(x => x.ManufacturerName).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.GenericName, x.TradeName, x.ManufacturerName, x.Strength, x.DosageForm, x.Unit, x.DoseAmount, x.FrequencyPerDay, x.MealTiming, x.UsageText })
            .ToListAsync();
        return Json(new { icd10Code = code, diseaseName = !string.IsNullOrWhiteSpace(disease?.ThaiName) ? disease.ThaiName : disease?.EnglishName ?? "Disease name unavailable", englishName = disease?.EnglishName ?? "English description unavailable", selectedDrugIds = selectedIds, drugs });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> SaveIcdDrugSelection([FromBody] IcdDrugSelectionRequest request)
    {
        var clinicCode = await GetClinicCode();
        var code = NormalizeCode(request.Icd10Code);
        if (code.Length < 3) return BadRequest(new { error = "กรุณาระบุรหัส ICD-10" });
        var disease = await db.Icd10Codes.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive && x.IsTerminal && x.Code == code);
        if (disease is null) return BadRequest(new { error = $"ไม่พบรหัส ICD-10 {code} ในฐานข้อมูล กรุณาเลือกจากรายการค้นหา" });
        var requestedIds = (request.DrugIds ?? []).Distinct().ToHashSet();
        var validIds = (await db.ClinicDrugs.AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && x.IsActive && x.ApprovalStatus == "Approved" && requestedIds.Contains(x.Id))
            .Select(x => x.Id).ToListAsync()).ToHashSet();
        var existing = await db.IcdDrugProtocols.Where(x => x.ClinicCode == clinicCode && x.Icd10Code == code)
            .OrderBy(x => x.Id).ToListAsync();
        var added = 0; var removed = 0;
        foreach (var group in existing.GroupBy(x => x.ClinicDrugId))
        {
            var links = group.ToList();
            var primaryLink = links[0];
            var shouldBeActive = validIds.Contains(group.Key);
            if (primaryLink.IsActive != shouldBeActive)
            {
                primaryLink.IsActive = shouldBeActive;
                primaryLink.UpdatedAtUtc = DateTime.UtcNow;
                if (shouldBeActive) added++; else removed++;
            }
            // ข้อมูลรุ่นเก่าอาจมีความสัมพันธ์ซ้ำ เก็บไว้เพียงแถวแรกเพื่อให้หนึ่งยาเชื่อมหนึ่งโรคครั้งเดียว
            foreach (var duplicateLink in links.Skip(1)) db.IcdDrugProtocols.Remove(duplicateLink);
        }
        var existingIds = existing.Select(x => x.ClinicDrugId).ToHashSet();
        foreach (var drugId in validIds.Where(x => !existingIds.Contains(x)))
        {
            db.IcdDrugProtocols.Add(new IcdDrugProtocol { ClinicCode = clinicCode, ClinicDrugId = drugId, Icd10Code = code, DiagnosisType = "Primary", IsActive = true });
            added++;
        }
        await db.SaveChangesAsync();
        await Audit(clinicCode, "Protocol", 0, "AssignDrugs", $"{code} {disease.ThaiName}: เลือกยา {validIds.Count} รายการ · เพิ่ม {added} · นำออก {removed}");
        return Json(new { icd10Code = code, diseaseName = disease.ThaiName, englishName = disease.EnglishName, selected = validIds.Count, added, removed });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,AdminClinic")]
    public async Task<IActionResult> DeleteIcdDrugAssignments(string icd10Code)
    {
        var clinicCode = await GetClinicCode();
        var code = NormalizeCode(icd10Code);
        var links = await db.IcdDrugProtocols.Where(x => x.ClinicCode == clinicCode && x.Icd10Code == code && x.IsActive).ToListAsync();
        foreach (var link in links) { link.IsActive = false; link.UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        await Audit(clinicCode, "Protocol", 0, "RemoveDiseaseDrugs", $"{code}: นำรายการยาออก {links.Count} ความสัมพันธ์");
        TempData["DrugCatalogSuccess"] = $"ลบความสัมพันธ์รายการยาของโรค {code} แล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExternalSearch(string? q)
    {
        var query = Clean(q); if (query.Length < 2) return Json(new { configured = true, items = Array.Empty<object>() });
        var baseUrl = configuration["DrugDataApi:BaseUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl)) return Json(new { configured = false, message = "ยังไม่ได้กำหนด API URL/Key จากหน่วยงานเจ้าของข้อมูล", items = Array.Empty<object>() });
        try
        {
            var path = (configuration["DrugDataApi:SearchPath"] ?? "/drugs/search?q={query}").Replace("{query}", Uri.EscapeDataString(query));
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), path.TrimStart('/')));
            var key = configuration["DrugDataApi:ApiKey"]; var header = configuration["DrugDataApi:ApiKeyHeader"] ?? "X-Api-Key";
            if (!string.IsNullOrWhiteSpace(key)) request.Headers.TryAddWithoutValidation(header, key);
            using var response = await httpClients.CreateClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return StatusCode(502, new { configured = true, message = $"API ตอบกลับ {(int)response.StatusCode}", items = Array.Empty<object>() });
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
            var array = document.RootElement.ValueKind == JsonValueKind.Array ? document.RootElement : document.RootElement.TryGetProperty("items", out var items) ? items : default;
            if (array.ValueKind != JsonValueKind.Array) return Json(new { configured = true, message = "รูปแบบข้อมูล API ไม่ถูกต้อง", items = Array.Empty<object>() });
            var result = array.EnumerateArray().Take(50).Select(x => new
            {
                genericName = JsonText(x,"genericName","generic_name","activeIngredient","name"), tradeName = JsonText(x,"tradeName","trade_name","brand"),
                manufacturerName = JsonText(x,"manufacturerName","manufacturer_name","manufacturer","company","companyName"),
                strength = JsonText(x,"strength"), dosageForm = JsonText(x,"dosageForm","dosage_form","form"), tmtCode = JsonText(x,"tmtCode","tmt_code","tmtId"),
                registrationNumber = JsonText(x,"registrationNumber","registration_number","register")
            }).ToList();
            return Json(new { configured = true, items = result });
        }
        catch (Exception ex) { return StatusCode(502, new { configured = true, message = $"เชื่อมต่อ API ไม่สำเร็จ: {ex.Message}", items = Array.Empty<object>() }); }
    }

    [HttpGet]
    public async Task<IActionResult> DrugSearchSuggestions(string? q, int take = 12)
    {
        var clinicCode = await GetClinicCode();
        var term = Clean(q);
        take = Math.Clamp(take, 1, 20);
        if (term.Length == 0) return Json(Array.Empty<object>());

        var items = await db.ClinicDrugs.AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode &&
                (x.GenericName.Contains(term) || x.TradeName.Contains(term) || x.ManufacturerName.Contains(term) ||
                 x.Strength.Contains(term) || x.TmtCode.Contains(term) || x.RegistrationNumber.Contains(term)))
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.GenericName)
            .ThenBy(x => x.ManufacturerName)
            .ThenBy(x => x.Id)
            .Take(take)
            .Select(x => new
            {
                x.Id, x.GenericName, x.TradeName, x.ManufacturerName, x.Strength, x.DosageForm,
                x.TmtCode, x.RegistrationNumber, x.IsActive
            }).ToListAsync();
        return Json(items);
    }

    private const string MedicineItemMarker = @"(?<!\d)\d{1,3}\s*[.)]\s*(?=[A-Za-zก-๙])";

    private static List<ImportCandidate> BuildImportCandidates(IEnumerable<LegacyRecord> records)
    {
        var map = new Dictionary<string, ImportCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        foreach (var parsed in ParseLegacyMedicines(record.Treatment))
        {
            var key = DrugKey(parsed.GenericName, parsed.Strength, parsed.DosageForm, parsed.ManufacturerName);
            if (!map.TryGetValue(key, out var item)) map[key] = item = new ImportCandidate(key, parsed.GenericName, parsed.TradeName, parsed.ManufacturerName, parsed.Strength, parsed.DosageForm, parsed.DoseAmount, parsed.FrequencyPerDay, parsed.MealTiming, parsed.Morning, parsed.Noon, parsed.Evening, parsed.Bedtime, parsed.IntervalHours, parsed.UsageText, parsed.AdviceText);
            item.RecordCount++; foreach (var code in new[] { record.PrimaryCode }.Concat((record.DifferentialCodes??"").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)).Select(NormalizeCode)) if(code.Length>=3)item.IcdCodes.Add(code);
        }
        return map.Values.OrderByDescending(x=>x.RecordCount).ThenBy(x=>x.GenericName).ToList();
    }

    private static IEnumerable<ParsedLegacyDrug> ParseLegacyMedicines(string? source)
    {
        var text=NormalizeThaiDigits(Regex.Replace(source??"",@"\s+"," "));text=Regex.Split(text,@"กรณีเกินศักยภาพ|การติดตามผู้รับบริการ|เวลาสิ้นสุดการรับบริการ",RegexOptions.IgnoreCase)[0];
        const string marker=MedicineItemMarker;
        foreach(Match match in Regex.Matches(text,$@"{marker}(?<body>.*?)(?={marker}|$)"))
        {
            var body=match.Groups["body"].Value.Trim();
            var usage=Regex.Match(body,@"(?:การใช้ยา|วิธีใช้)\s*:\s*(?<v>.*?)(?=คำแนะนำ\s*:|$)",RegexOptions.IgnoreCase);
            var advice=Regex.Match(body,@"คำแนะนำ\s*:\s*(?<v>.*)$",RegexOptions.IgnoreCase);
            var name=Regex.Replace(body,@"(?:การใช้ยา|วิธีใช้)\s*:.*$","",RegexOptions.IgnoreCase).Trim(' ','-','.');
            if(name.Length==0)continue;
            var strengthMatch=Regex.Match(name,@"(?<strength>\d+(?:\.\d+)?\s*(?:mg|g|mcg|ml|%)(?:\s*/\s*\d+(?:\.\d+)?\s*(?:ml|g))?)",RegexOptions.IgnoreCase);
            var formMatch=Regex.Match(name,@"\b(tablet|capsule(?:,\s*soft|,\s*hard)?|syrup|suspension|solution|lotion|cream|ointment|gel|drops?|injection)\b",RegexOptions.IgnoreCase);
            var strength=strengthMatch.Success?strengthMatch.Groups["strength"].Value:"";
            var form=formMatch.Success?formMatch.Value:"";
            var parentheses=Regex.Matches(name,@"\((?<value>[^()]*)\)");
            var generic=parentheses.Count>0?name[..parentheses[0].Index].Trim():name;
            generic=Regex.Replace(generic,@"\b(tablet|capsule(?:,\s*soft|,\s*hard)?|syrup|suspension|solution|lotion|cream|ointment|gel|drops?|injection)\b","",RegexOptions.IgnoreCase);
            generic=Regex.Replace(generic,@"\d+(?:\.\d+)?\s*(?:mg|g|mcg|ml|%)(?:\s*/\s*\d+(?:\.\d+)?\s*(?:ml|g))?","",RegexOptions.IgnoreCase).Trim(' ','-','.',',');
            if(generic.Length==0)generic=Regex.Replace(name,@"\([^()]*\)","").Trim(' ','-','.',',');
            var manufacturer=parentheses.Count>0?parentheses[0].Groups["value"].Value.Trim():"";
            var trade=parentheses.Count>1?parentheses[1].Groups["value"].Value.Trim():"";
            trade=Regex.Replace(trade,@"\d+(?:\.\d+)?\s*(?:mg|g|mcg|ml|%)(?:\s*/\s*\d+(?:\.\d+)?\s*(?:ml|g))?","",RegexOptions.IgnoreCase);
            trade=Regex.Replace(trade,@"\b(tablet|capsule|syrup|suspension|solution|lotion|cream|ointment|gel|drops?|injection)\b","",RegexOptions.IgnoreCase).Trim(' ','-','.',',');
            var use=usage.Success?usage.Groups["v"].Value.Trim():"";var dose=Regex.Match(use,@"ครั้งละ\s*(?<v>.*?)(?=\s*วันละ|\s*(?:ก่อน|หลัง)อาหาร|$)");var freq=Regex.Match(use,@"วันละ\s*(?<v>\d+)");var interval=Regex.Match(use,@"ทุก\s*(?<v>\d+(?:\.\d+)?)\s*ชั่วโมง");
            yield return new ParsedLegacyDrug(generic,trade,manufacturer,strength,form,dose.Success?dose.Groups["v"].Value.Trim():"",freq.Success?freq.Groups["v"].Value:"",use.Contains("ก่อนอาหาร")?"ก่อนอาหาร":use.Contains("หลังอาหาร")?"หลังอาหาร":"",use.Contains("เช้า"),use.Contains("เที่ยง"),use.Contains("เย็น"),use.Contains("ก่อนนอน"),interval.Success?interval.Groups["v"].Value:"",use,advice.Success?advice.Groups["v"].Value.Trim():"");
        }
    }

    private static List<AdviceCandidate> BuildAdviceCandidates(IEnumerable<LegacyRecord> records)
    {var map=new Dictionary<string,AdviceCandidate>(StringComparer.OrdinalIgnoreCase);foreach(var record in records){var prefix=AdviceOnly(record.Treatment);foreach(var line in Regex.Split(prefix,@"\s+-\s*|(?<=\.)\s+(?=แนะนำ|หาก|ควร|หลีกเลี่ยง)").Select(x=>x.Trim(' ','-','.')).Where(x=>x.Length>=8&&!ContainsNumberedItem(x))){var key=NormalizeDrugPart(line);if(!map.TryGetValue(key,out var item))map[key]=item=new AdviceCandidate(key,line,ClassifyAdvice(line));item.RecordCount++;foreach(var code in new[]{record.PrimaryCode}.Concat((record.DifferentialCodes??"").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)).Select(NormalizeCode))if(code.Length>=3)item.IcdCodes.Add(code);}}return map.Values.OrderByDescending(x=>x.RecordCount).ToList();}
    private static string AdviceOnly(string? source)
    {
        var text = NormalizeThaiDigits(Regex.Replace(source ?? "", @"\s+", " ")).Trim();
        var firstMedicine = Regex.Match(text, MedicineItemMarker, RegexOptions.CultureInvariant);
        return (firstMedicine.Success ? text[..firstMedicine.Index] : text).Trim(' ', '-', '.', ';', ':');
    }
    private static bool ContainsNumberedItem(string? text) => Regex.IsMatch(NormalizeThaiDigits(text ?? ""), MedicineItemMarker);
    private static List<ParseWarning> BuildParseWarnings(IEnumerable<LegacyRecord> records)
    {
        var warnings = new List<ParseWarning>();
        foreach (var record in records)
        {
            var normalized = NormalizeThaiDigits(Regex.Replace(record.Treatment ?? "", @"\s+", " ")).Trim();
            var detectedMarkers = Regex.Matches(normalized, MedicineItemMarker).Count;
            var parsedItems = ParseLegacyMedicines(normalized).Count();
            var appearsToContainMedicine = normalized.Contains("การใช้ยา", StringComparison.OrdinalIgnoreCase) || normalized.Contains("วิธีใช้", StringComparison.OrdinalIgnoreCase);
            if (detectedMarkers <= parsedItems && !(appearsToContainMedicine && parsedItems == 0)) continue;
            var excerpt = normalized.Length <= 260 ? normalized : normalized[..260] + "…";
            warnings.Add(new ParseWarning(record.Id, NormalizeCode(record.PrimaryCode), detectedMarkers, parsedItems, excerpt));
        }
        return warnings;
    }
    private static string ClassifyAdvice(string text)=>text.Contains("กลับมา")||text.Contains("พบแพทย์")?"FollowUp":text.Contains("ห้าม")||text.Contains("หลีกเลี่ยง")||text.Contains("ระวัง")?"Warning":"General";
    private static string NormalizeThaiDigits(string value){const string thai="๐๑๒๓๔๕๖๗๘๙";for(var i=0;i<thai.Length;i++)value=value.Replace(thai[i],(char)('0'+i));return value;}

    private static bool SameDrug(string n1,string s1,string f1,string m1,string n2,string s2,string f2,string m2)=>DrugKey(n1,s1,f1,m1)==DrugKey(n2,s2,f2,m2);
    private static string DrugKey(string name,string strength,string form,string manufacturer)=>$"{NormalizeDrugPart(name)}|{NormalizeDrugPart(strength)}|{NormalizeDrugPart(form)}|{NormalizeDrugPart(manufacturer)}";
    private static string NormalizeDrugPart(string? value)=>Regex.Replace(Clean(value).ToUpperInvariant(),@"\s+","");
    private static string JsonText(JsonElement item,params string[] names){foreach(var name in names)if(item.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String)return value.GetString()??"";return "";}
    public sealed class OpdImportCommitRequest { public List<OpdImportDrugInput> Items { get; set; } = []; public List<OpdImportAdviceInput> Advice { get; set; } = []; }
    public sealed class IcdDrugSelectionRequest { public string Icd10Code { get; set; } = string.Empty; public List<int> DrugIds { get; set; } = []; }
    public sealed class OpdImportDrugInput{public bool Selected{get;set;}public bool LinkDetectedIcd{get;set;}public string Key{get;set;}="";public string GenericName{get;set;}="";public string TradeName{get;set;}="";public string ManufacturerName{get;set;}="";public string Strength{get;set;}="";public string DosageForm{get;set;}="";public string DoseAmount{get;set;}="";public string FrequencyPerDay{get;set;}="";public string MealTiming{get;set;}="";public bool Morning{get;set;}public bool Noon{get;set;}public bool Evening{get;set;}public bool Bedtime{get;set;}public string IntervalHours{get;set;}="";public string UsageText{get;set;}="";public string AdviceText{get;set;}="";public List<string>Icd10Codes{get;set;}=[];}
    public sealed class OpdImportAdviceInput{public bool Selected{get;set;}public string Key{get;set;}="";public string Text{get;set;}="";public string Category{get;set;}="General";public List<string>Icd10Codes{get;set;}=[];}
    private sealed record LegacyRecord(int Id,string PrimaryCode,string DifferentialCodes,string Treatment,DateTime VisitDate);
    private sealed record ParseWarning(int RecordId,string Icd10Code,int DetectedMarkers,int ParsedItems,string Excerpt);
    private sealed record ParsedLegacyDrug(string GenericName,string TradeName,string ManufacturerName,string Strength,string DosageForm,string DoseAmount,string FrequencyPerDay,string MealTiming,bool Morning,bool Noon,bool Evening,bool Bedtime,string IntervalHours,string UsageText,string AdviceText);
    private sealed class ImportCandidate(string key,string name,string trade,string manufacturer,string strength,string form,string dose,string frequency,string meal,bool morning,bool noon,bool evening,bool bedtime,string interval,string usage,string advice){public string Key{get;}=key;public string GenericName{get;}=name;public string TradeName{get;}=trade;public string ManufacturerName{get;}=manufacturer;public string Strength{get;}=strength;public string DosageForm{get;}=form;public string DoseAmount{get;}=dose;public string FrequencyPerDay{get;}=frequency;public string MealTiming{get;}=meal;public bool Morning{get;}=morning;public bool Noon{get;}=noon;public bool Evening{get;}=evening;public bool Bedtime{get;}=bedtime;public string IntervalHours{get;}=interval;public string UsageText{get;}=usage;public string AdviceText{get;}=advice;public int RecordCount{get;set;}public HashSet<string>IcdCodes{get;}=[];}
    private sealed class AdviceCandidate(string key,string text,string category){public string Key{get;}=key;public string Text{get;}=text;public string Category{get;}=category;public int RecordCount{get;set;}public HashSet<string>IcdCodes{get;}=[];}

    private async Task Audit(string clinicCode,string type,int id,string action,string detail){var user=await users.GetUserAsync(User);db.DrugKnowledgeAuditLogs.Add(new DrugKnowledgeAuditLog{ClinicCode=clinicCode,EntityType=type,EntityId=id,Action=action,Detail=detail,UserId=user?.Id??"",UserName=user?.FullName??user?.UserName??""});await db.SaveChangesAsync();}
    private async Task<string> GetClinicCode(){var user=await users.GetUserAsync(User);return user?.ClinicCode??"SMARTCLINIC";}
    private static string Clean(string? value)=>value?.Trim()??string.Empty;
    private static string NormalizeCode(string? value)=>Clean(value).ToUpperInvariant();
}
