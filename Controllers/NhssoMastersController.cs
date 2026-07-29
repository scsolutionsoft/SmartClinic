using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class NhssoMastersController : Controller
{
    private static readonly Regex ClinicCodeRegex = new("^[A-Za-z0-9]{9,10}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext dbContext;

    public NhssoMastersController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await BuildDashboard("พร้อมจัดการข้อมูลคลินิกจาก สปสช.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NhssoMasterDashboardViewModel model)
    {
        var input = model.CreateInput;
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboard("ข้อมูลที่กรอกไม่ครบถ้วน", model));
        }

        if (!ClinicCodeRegex.IsMatch(input.ClinicCode))
        {
            ModelState.AddModelError(nameof(model.CreateInput.ClinicCode), "รหัสคลินิกต้องเป็นตัวอักษร/ตัวเลข 9-10 ตัว");
            return View("Index", await BuildDashboard("รูปแบบรหัสคลินิกไม่ถูกต้อง", model));
        }

        var exists = await dbContext.NhssoClinicMasters.AnyAsync(x => x.ClinicCode == input.ClinicCode);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.CreateInput.ClinicCode), "รหัสคลินิกนี้มีอยู่แล้วใน master");
            return View("Index", await BuildDashboard("รหัสคลินิกซ้ำ", model));
        }

        dbContext.NhssoClinicMasters.Add(new NhssoClinicMaster
        {
            ClinicCode = input.ClinicCode.Trim(),
            ClinicName = input.ClinicName.Trim(),
            Address = input.Address.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(input.ContactPhone) ? null : input.ContactPhone.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(input.ContactEmail) ? null : input.ContactEmail.Trim(),
            IsActive = input.IsActive
        });

        await dbContext.SaveChangesAsync();
        TempData["StatusModal"] = "nhsso-create";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsv(NhssoMasterDashboardViewModel model)
    {
        if (model.ImportInput.CsvFile is null || model.ImportInput.CsvFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.ImportInput.CsvFile), "กรุณาเลือกไฟล์ CSV");
            return View("Index", await BuildDashboard("ไม่พบไฟล์ CSV", model));
        }

        if (!Path.GetExtension(model.ImportInput.CsvFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.ImportInput.CsvFile), "รองรับเฉพาะไฟล์ .csv เท่านั้น");
            return View("Index", await BuildDashboard("ประเภทไฟล์ไม่ถูกต้อง", model));
        }

        using var reader = new StreamReader(model.ImportInput.CsvFile.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var lines = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (lines.Count <= 1)
        {
            ModelState.AddModelError(nameof(model.ImportInput.CsvFile), "ไฟล์ CSV ไม่มีข้อมูล");
            return View("Index", await BuildDashboard("ไฟล์ไม่มีข้อมูล", model));
        }

        var importCount = 0;
        var updateCount = 0;
        var invalidCount = 0;

        foreach (var rawLine in lines.Skip(1))
        {
            var values = rawLine.Split(',');
            if (values.Length < 3)
            {
                invalidCount++;
                continue;
            }

            var clinicCode = values[0].Trim();
            var clinicName = values[1].Trim();
            var address = values[2].Trim();
            var contactPhone = values.Length > 3 ? values[3].Trim() : null;
            var contactEmail = values.Length > 4 ? values[4].Trim() : null;
            var isActiveRaw = values.Length > 5 ? values[5].Trim() : "true";

            if (!ClinicCodeRegex.IsMatch(clinicCode) || string.IsNullOrWhiteSpace(clinicName) || string.IsNullOrWhiteSpace(address))
            {
                invalidCount++;
                continue;
            }

            var isActive = !isActiveRaw.Equals("false", StringComparison.OrdinalIgnoreCase) && !isActiveRaw.Equals("0", StringComparison.OrdinalIgnoreCase);
            var existing = await dbContext.NhssoClinicMasters.FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);
            if (existing is null)
            {
                dbContext.NhssoClinicMasters.Add(new NhssoClinicMaster
                {
                    ClinicCode = clinicCode,
                    ClinicName = clinicName,
                    Address = address,
                    ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone,
                    ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail,
                    IsActive = isActive
                });
                importCount++;
            }
            else
            {
                existing.ClinicName = clinicName;
                existing.Address = address;
                existing.ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone;
                existing.ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail;
                existing.IsActive = isActive;
                updateCount++;
            }
        }

        await dbContext.SaveChangesAsync();
        TempData["StatusModal"] = "nhsso-import";
        TempData["ImportCount"] = importCount;
        TempData["UpdateCount"] = updateCount;
        TempData["InvalidCount"] = invalidCount;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await dbContext.NhssoClinicMasters.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.IsActive = !item.IsActive;
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.NhssoClinicMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        var vm = new NhssoMasterEditViewModel
        {
            Id = item.Id,
            ClinicCode = item.ClinicCode,
            ClinicName = item.ClinicName,
            Address = item.Address,
            ContactPhone = item.ContactPhone,
            ContactEmail = item.ContactEmail,
            IsActive = item.IsActive
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NhssoMasterEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!ClinicCodeRegex.IsMatch(model.ClinicCode))
        {
            ModelState.AddModelError(nameof(model.ClinicCode), "รหัสคลินิกต้องเป็นตัวอักษร/ตัวเลข 9-10 ตัว");
            return View(model);
        }

        var item = await dbContext.NhssoClinicMasters.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (item is null)
        {
            return NotFound();
        }

        var duplicateCode = await dbContext.NhssoClinicMasters.AnyAsync(x => x.ClinicCode == model.ClinicCode && x.Id != model.Id);
        if (duplicateCode)
        {
            ModelState.AddModelError(nameof(model.ClinicCode), "รหัสคลินิกนี้มีอยู่แล้วใน master");
            return View(model);
        }

        item.ClinicCode = model.ClinicCode.Trim();
        item.ClinicName = model.ClinicName.Trim();
        item.Address = model.Address.Trim();
        item.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : model.ContactPhone.Trim();
        item.ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim();
        item.IsActive = model.IsActive;

        await dbContext.SaveChangesAsync();
        TempData["StatusModal"] = "nhsso-edit";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await dbContext.NhssoClinicMasters.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        var isReferenced = await dbContext.Clinics.AnyAsync(x => x.ClinicCode == item.ClinicCode);
        if (isReferenced)
        {
            TempData["StatusModal"] = "nhsso-delete-blocked";
            return RedirectToAction(nameof(Index));
        }

        dbContext.NhssoClinicMasters.Remove(item);
        await dbContext.SaveChangesAsync();
        TempData["StatusModal"] = "nhsso-delete";
        return RedirectToAction(nameof(Index));
    }

    private async Task<NhssoMasterDashboardViewModel> BuildDashboard(string statusMessage, NhssoMasterDashboardViewModel? model = null)
    {
        var items = await dbContext.NhssoClinicMasters
            .AsNoTracking()
            .OrderBy(x => x.ClinicCode)
            .Select(x => new NhssoMasterItemViewModel
            {
                Id = x.Id,
                ClinicCode = x.ClinicCode,
                ClinicName = x.ClinicName,
                Address = x.Address,
                ContactPhone = x.ContactPhone,
                ContactEmail = x.ContactEmail,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return new NhssoMasterDashboardViewModel
        {
            ImportInput = model?.ImportInput ?? new NhssoMasterImportViewModel(),
            CreateInput = model?.CreateInput ?? new NhssoMasterEditViewModel { IsActive = true },
            Items = items,
            StatusMessage = statusMessage
        };
    }
}