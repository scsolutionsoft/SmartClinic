using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public static class Icd10Seed
{
    private const string WhoVersion = "WHO ICD-10 2019";
    private const string WhoSource = "World Health Organization";
    private const string WhoSourceUrl = "https://icdcdn.who.int/icd10/index.html";
    private const int ExpectedWhoCodeCount = 12_221;
    private const int ExpectedWhoHeadingCount = 1_588;

    private const string TmVersion = "ICD-10-TM 2019 update 2024-05";
    private const string TmSource = "Thai Health Coding Center";
    private const string TmSourceUrl = "http://thcc.or.th/download/icd/Data%20TM2019-update_052024.xlsx";
    private const int ExpectedTmCodeCount = 39_236;

    public static async Task SeedAsync(ApplicationDbContext dbContext, string contentRootPath)
    {
        var whoHeadingCount = await dbContext.Icd10Codes.CountAsync(x => !x.IsTerminal && x.ChapterCode != "");
        if (whoHeadingCount < ExpectedWhoHeadingCount)
        {
            await SeedWhoHierarchyAsync(dbContext, contentRootPath);
        }

        var tmImportedCount = await dbContext.Icd10Codes.CountAsync(x => x.Version == TmVersion && x.IsTerminal);
        if (tmImportedCount < ExpectedTmCodeCount)
        {
            await SeedThaiModificationAsync(dbContext, contentRootPath);
        }

        await EnrichLegacyRowsAsync(dbContext);
        await ApplyThaiNameOverlaysAsync(dbContext);
    }

    private static async Task SeedWhoHierarchyAsync(ApplicationDbContext dbContext, string contentRootPath)
    {
        var archivePath = Path.Combine(contentRootPath, "Data", "SeedData", "icd102019enMeta.zip");
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException("The official WHO ICD-10 seed archive was not found.", archivePath);
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1252);
        using var archive = ZipFile.OpenRead(archivePath);

        var chapters = ReadDelimitedRows(archive, "icd102019syst_chapters.txt", encoding)
            .ToDictionary(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);
        var blocks = ReadDelimitedRows(archive, "icd102019syst_groups.txt", encoding)
            .ToDictionary(
                x => x[0],
                x => new IcdBlock($"{x[0]}-{x[1]}", x[3]),
                StringComparer.OrdinalIgnoreCase);
        var officialRows = ReadDelimitedRows(archive, "icd102019syst_codes.txt", encoding).ToList();

        if (officialRows.Count != ExpectedWhoCodeCount)
        {
            throw new InvalidDataException($"Expected {ExpectedWhoCodeCount:N0} WHO ICD-10 rows but found {officialRows.Count:N0}.");
        }

        var existing = await dbContext.Icd10Codes.ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var fields in officialRows)
        {
            if (fields.Length != 17)
            {
                throw new InvalidDataException("A WHO ICD-10 source row does not contain the expected 17 fields.");
            }

            var code = fields[7].Trim().ToUpperInvariant();
            var title = fields[8].Trim();
            var chapterCode = fields[3].Trim();
            var blockStart = fields[4].Trim();
            var block = blocks.GetValueOrDefault(blockStart) ?? new IcdBlock(blockStart, string.Empty);

            if (!existing.TryGetValue(code, out var entity))
            {
                entity = new Icd10Code { Code = code };
                dbContext.Icd10Codes.Add(entity);
                existing.Add(code, entity);
            }
            else if (!string.IsNullOrWhiteSpace(entity.ThaiName) && string.IsNullOrWhiteSpace(entity.ThaiNameVersion))
            {
                entity.ThaiNameVersion = entity.Version;
            }

            entity.DisplayCode = fields[6].Trim().TrimEnd('-', '.');
            entity.EnglishName = title;
            entity.SearchTerms = MergeSearchTerms(entity.SearchTerms, title, fields[9], fields[10], block.Title);
            entity.Version = WhoVersion;
            entity.ChapterCode = chapterCode;
            entity.ChapterTitle = chapters.GetValueOrDefault(chapterCode) ?? string.Empty;
            entity.BlockCode = block.Code;
            entity.BlockTitle = block.Title;
            entity.ParentCode = code.Length > 3 ? code[..3] : string.Empty;
            entity.Source = WhoSource;
            entity.SourceUrl = WhoSourceUrl;
            entity.Level = int.TryParse(fields[0], out var level) ? level : 0;
            entity.IsTerminal = fields[1].Equals("T", StringComparison.OrdinalIgnoreCase);
            entity.IsDerived = fields[2].Equals("S", StringComparison.OrdinalIgnoreCase);
            entity.IsActive = true;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedThaiModificationAsync(ApplicationDbContext dbContext, string contentRootPath)
    {
        var workbookPath = Path.Combine(contentRootPath, "Data", "SeedData", "icd10tm2019-update-202405.xlsx");
        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException("The official ICD-10-TM workbook was not found.", workbookPath);
        }

        var rows = ReadThaiModificationWorkbook(workbookPath);
        ValidateThaiModificationRows(rows);

        var existing = await dbContext.Icd10Codes.ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase);
        EnsureMissingTmParentCategories(rows, existing, dbContext);
        foreach (var row in rows)
        {
            var parentCode = row.Code[..3];
            var parent = existing.GetValueOrDefault(parentCode) ?? FindHierarchyTemplate(existing.Values, parentCode);

            var existed = existing.TryGetValue(row.Code, out var entity);
            var wasWhoCode = existed && entity!.Source == WhoSource;
            if (!existed)
            {
                entity = new Icd10Code { Code = row.Code };
                dbContext.Icd10Codes.Add(entity);
                existing.Add(row.Code, entity);
            }
            else if (!string.IsNullOrWhiteSpace(entity!.ThaiName) && string.IsNullOrWhiteSpace(entity.ThaiNameVersion))
            {
                entity.ThaiNameVersion = entity.Version;
            }

            entity!.DisplayCode = FormatDisplayCode(row.Code);
            entity.EnglishName = row.Description;
            entity.SearchTerms = MergeSearchTerms(entity.SearchTerms, row.Description, parent.EnglishName, parent.BlockTitle);
            entity.Version = TmVersion;
            entity.ChapterCode = parent.ChapterCode;
            entity.ChapterTitle = parent.ChapterTitle;
            entity.BlockCode = parent.BlockCode;
            entity.BlockTitle = parent.BlockTitle;
            entity.ParentCode = row.Code == parentCode ? string.Empty : parentCode;
            entity.Source = TmSource;
            entity.SourceUrl = TmSourceUrl;
            entity.Level = row.Code.Length;
            entity.IsTerminal = true;
            entity.IsDerived = !wasWhoCode || row.Code.Length > 4;
            entity.IsActive = true;
        }

        await dbContext.SaveChangesAsync();
    }

    private static void EnsureMissingTmParentCategories(
        IReadOnlyList<IcdSourceRow> rows,
        IDictionary<string, Icd10Code> existing,
        ApplicationDbContext dbContext)
    {
        var missingParents = rows
            .Where(x => x.Code.Length > 3)
            .GroupBy(x => x.Code[..3], StringComparer.OrdinalIgnoreCase)
            .Where(x => !existing.ContainsKey(x.Key))
            .OrderBy(x => x.Key)
            .ToList();

        foreach (var group in missingParents)
        {
            var template = FindHierarchyTemplate(existing.Values, group.Key);
            var title = Regex.Replace(group.First().Description, ", unspecified$", string.Empty, RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\.\d$", string.Empty);
            var entity = new Icd10Code
            {
                Code = group.Key,
                DisplayCode = group.Key,
                EnglishName = title,
                SearchTerms = MergeSearchTerms(title, template.BlockTitle),
                Version = TmVersion,
                ChapterCode = template.ChapterCode,
                ChapterTitle = template.ChapterTitle,
                BlockCode = template.BlockCode,
                BlockTitle = template.BlockTitle,
                Source = TmSource,
                SourceUrl = TmSourceUrl,
                Level = 3,
                IsTerminal = false,
                IsDerived = true,
                IsActive = true
            };
            dbContext.Icd10Codes.Add(entity);
            existing.Add(group.Key, entity);
        }
    }

    private static Icd10Code FindHierarchyTemplate(IEnumerable<Icd10Code> existing, string categoryCode)
    {
        var template = existing.FirstOrDefault(x => BlockContains(x.BlockCode, categoryCode));
        return template ?? throw new InvalidDataException($"ICD-10-TM category {categoryCode} does not belong to a known WHO block.");
    }

    private static bool BlockContains(string blockCode, string categoryCode)
    {
        var boundaries = blockCode.Split('-', 2, StringSplitOptions.TrimEntries);
        return boundaries.Length == 2
            && string.CompareOrdinal(categoryCode, boundaries[0]) >= 0
            && string.CompareOrdinal(categoryCode, boundaries[1]) <= 0;
    }

    private static async Task EnrichLegacyRowsAsync(ApplicationDbContext dbContext)
    {
        var legacyRows = await dbContext.Icd10Codes
            .Where(x => x.Version != WhoVersion && x.Version != TmVersion)
            .ToListAsync();
        if (legacyRows.Count == 0)
        {
            return;
        }

        var parentCodes = legacyRows.Select(x => x.Code[..Math.Min(3, x.Code.Length)]).Distinct().ToList();
        var parents = await dbContext.Icd10Codes.AsNoTracking()
            .Where(x => parentCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var entity in legacyRows)
        {
            var parentCode = entity.Code[..Math.Min(3, entity.Code.Length)];
            parents.TryGetValue(parentCode, out var parent);
            entity.DisplayCode = FormatDisplayCode(entity.Code);
            entity.ThaiNameVersion = string.IsNullOrWhiteSpace(entity.ThaiNameVersion) ? entity.Version : entity.ThaiNameVersion;
            entity.ChapterCode = parent?.ChapterCode ?? string.Empty;
            entity.ChapterTitle = parent?.ChapterTitle ?? string.Empty;
            entity.BlockCode = parent?.BlockCode ?? string.Empty;
            entity.BlockTitle = parent?.BlockTitle ?? string.Empty;
            entity.ParentCode = parentCode == entity.Code ? string.Empty : parentCode;
            entity.Source = "SmartClinic ICD-10-TM legacy overlay";
            entity.SourceUrl = string.Empty;
            entity.Level = entity.Code.Length;
            entity.IsTerminal = true;
            entity.IsDerived = true;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task ApplyThaiNameOverlaysAsync(ApplicationDbContext dbContext)
    {
        var entity = await dbContext.Icd10Codes.FirstAsync(x => x.Code == "M7911");
        entity.ThaiName = "ปวดกล้ามเนื้อบริเวณหัวไหล่และบ่า";
        entity.ThaiNameVersion = "ICD-10-TM 2016 reviewed Thai label";
        entity.SearchTerms = MergeSearchTerms(entity.SearchTerms,
            "ปวดกล้ามเนื้อ หัวไหล่ บ่า ไหล่ ปวดไหล่ ปวดบ่า กล้ามเนื้อไหล่ shoulder myalgia shoulder region shoulder muscle pain");
        await dbContext.SaveChangesAsync();
    }

    private static IReadOnlyList<IcdSourceRow> ReadThaiModificationWorkbook(string workbookPath)
    {
        using var archive = ZipFile.OpenRead(workbookPath);
        var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml")
            ?? throw new InvalidDataException("The ICD-10-TM workbook has no shared string table.");
        var worksheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidDataException("The ICD-10-TM workbook has no first worksheet.");

        XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using var sharedStream = sharedStringsEntry.Open();
        var sharedDocument = XDocument.Load(sharedStream);
        var sharedStrings = sharedDocument.Descendants(spreadsheet + "si")
            .Select(x => string.Concat(x.Descendants(spreadsheet + "t").Select(t => t.Value)))
            .ToList();

        using var worksheetStream = worksheetEntry.Open();
        var worksheet = XDocument.Load(worksheetStream);
        var result = new List<IcdSourceRow>(ExpectedTmCodeCount);
        foreach (var row in worksheet.Descendants(spreadsheet + "row").Skip(1))
        {
            string code = string.Empty;
            string description = string.Empty;
            foreach (var cell in row.Elements(spreadsheet + "c"))
            {
                var reference = cell.Attribute("r")?.Value ?? string.Empty;
                var column = new string(reference.TakeWhile(char.IsLetter).ToArray());
                if (column is not ("A" or "B")) continue;

                var rawValue = cell.Element(spreadsheet + "v")?.Value ?? string.Empty;
                var value = cell.Attribute("t")?.Value == "s" && int.TryParse(rawValue, out var index)
                    ? sharedStrings[index]
                    : rawValue;
                if (column == "A") code = NormalizeCode(value);
                if (column == "B") description = value.Trim();
            }

            if (code.Length > 0 || description.Length > 0)
            {
                result.Add(new IcdSourceRow(code, description));
            }
        }

        return result;
    }

    private static void ValidateThaiModificationRows(IReadOnlyList<IcdSourceRow> rows)
    {
        if (rows.Count != ExpectedTmCodeCount)
        {
            throw new InvalidDataException($"Expected {ExpectedTmCodeCount:N0} ICD-10-TM rows but found {rows.Count:N0}.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!Regex.IsMatch(row.Code, "^[A-Z][0-9]{2}[0-9A-Z]{0,4}$"))
            {
                throw new InvalidDataException($"ICD-10-TM code '{row.Code}' has an invalid format.");
            }
            if (!seen.Add(row.Code))
            {
                throw new InvalidDataException($"Duplicate ICD-10-TM code '{row.Code}' was found in the official workbook.");
            }
            if (string.IsNullOrWhiteSpace(row.Description))
            {
                throw new InvalidDataException($"ICD-10-TM code '{row.Code}' has no description.");
            }
        }
    }

    private static IEnumerable<string[]> ReadDelimitedRows(ZipArchive archive, string entryName, Encoding encoding)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidDataException($"The WHO ICD-10 archive is missing {entryName}.");
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line.Split(';');
            }
        }
    }

    private static string NormalizeCode(string value) => value.Trim().Replace(".", string.Empty).ToUpperInvariant();

    private static string FormatDisplayCode(string code) => code.Length > 3 ? code.Insert(3, ".") : code;

    private static string MergeSearchTerms(params string?[] values)
    {
        var merged = string.Join(' ', values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase));
        return merged.Length <= 1000 ? merged : merged[..1000];
    }

    private sealed record IcdBlock(string Code, string Title);
    private sealed record IcdSourceRow(string Code, string Description);
}
