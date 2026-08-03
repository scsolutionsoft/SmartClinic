using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class Icd10CatalogViewModel
{
    public IReadOnlyList<Icd10Code> Items { get; set; } = [];
    public IReadOnlyList<Icd10ChapterOption> Chapters { get; set; } = [];
    public string SearchTerm { get; set; } = string.Empty;
    public string ChapterCode { get; set; } = string.Empty;
    public string RecordType { get; set; } = "terminal";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
    public int TerminalCount { get; set; }
    public int ThaiNameCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public record Icd10ChapterOption(string Code, string Title, int Count);
