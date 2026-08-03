namespace SmartClinic.Web.Models;

public class Icd10Code
{
    public string Code { get; set; } = string.Empty;
    public string DisplayCode { get; set; } = string.Empty;
    public string ThaiName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string SearchTerms { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ThaiNameVersion { get; set; } = string.Empty;
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public string BlockCode { get; set; } = string.Empty;
    public string BlockTitle { get; set; } = string.Empty;
    public string ParentCode { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsTerminal { get; set; } = true;
    public bool IsDerived { get; set; }
    public bool IsActive { get; set; } = true;
}
