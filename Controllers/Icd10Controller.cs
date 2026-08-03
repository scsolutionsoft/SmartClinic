using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize]
public class Icd10Controller(ApplicationDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q = null, string? chapter = null, string? type = "terminal", int page = 1)
    {
        const int pageSize = 50;
        var term = (q ?? string.Empty).Trim();
        var normalized = term.Replace(".", string.Empty).ToUpperInvariant();
        var chapterCode = (chapter ?? string.Empty).Trim();
        var recordType = type is "all" or "heading" ? type : "terminal";

        var active = dbContext.Icd10Codes.AsNoTracking().Where(x => x.IsActive);
        var query = active;
        if (recordType == "terminal") query = query.Where(x => x.IsTerminal);
        if (recordType == "heading") query = query.Where(x => !x.IsTerminal);
        if (chapterCode.Length > 0) query = query.Where(x => x.ChapterCode == chapterCode);
        if (term.Length > 0)
        {
            query = query.Where(x => x.Code.Contains(normalized) || x.DisplayCode.Contains(term) || x.ThaiName.Contains(term)
                || x.EnglishName.Contains(term) || x.SearchTerms.Contains(term) || x.BlockTitle.Contains(term) || x.ChapterTitle.Contains(term));
        }

        var filteredCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var chapterRows = await active.Where(x => x.ChapterCode != "")
            .GroupBy(x => new { x.ChapterCode, x.ChapterTitle })
            .Select(x => new { Code = x.Key.ChapterCode, Title = x.Key.ChapterTitle, Count = x.Count() })
            .OrderBy(x => x.Code)
            .ToListAsync();
        var chapters = chapterRows.Select(x => new Icd10ChapterOption(x.Code, x.Title, x.Count)).ToList();

        return View(new Icd10CatalogViewModel
        {
            Items = await query.OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Chapters = chapters,
            SearchTerm = term,
            ChapterCode = chapterCode,
            RecordType = recordType,
            Page = page,
            PageSize = pageSize,
            FilteredCount = filteredCount,
            TotalCount = await active.CountAsync(),
            TerminalCount = await active.CountAsync(x => x.IsTerminal),
            ThaiNameCount = await active.CountAsync(x => x.ThaiName != string.Empty)
        });
    }
}
