using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SmartClinic.Web.ViewModels;
using Tesseract;
using UglyToad.PdfPig;

namespace SmartClinic.Web.Services;

public interface IWoundCareOcrService
{
    Task<WoundCareOcrResultViewModel> AnalyzeAsync(IFormFile file);
}

public class WoundCareOcrService : IWoundCareOcrService
{
    private readonly IWebHostEnvironment environment;
    public WoundCareOcrService(IWebHostEnvironment environment) => this.environment = environment;

    public async Task<WoundCareOcrResultViewModel> AnalyzeAsync(IFormFile file)
    {
        string text;
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var bytes = stream.ToArray();

        if (Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            using var document = PdfDocument.Open(bytes);
            var builder = new StringBuilder();
            foreach (var page in document.GetPages()) builder.AppendLine(page.Text);
            text = builder.ToString();
        }
        else
        {
            var dataPath = Path.Combine(environment.ContentRootPath, "tessdata");
            using var engine = new TesseractEngine(dataPath, "tha+eng", EngineMode.LstmOnly);
            using var pix = Pix.LoadFromMemory(bytes);
            using var page = engine.Process(pix, PageSegMode.Auto);
            text = page.GetText();
        }

        return Parse(text);
    }

    private static WoundCareOcrResultViewModel Parse(string raw)
    {
        var text = Regex.Replace(raw ?? string.Empty, @"[ \t]+", " ").Trim();
        string Value(string start, string end) =>
            Regex.Match(text, $"{start}\\s*:?[.\\s]*(?<v>.*?)(?={end}|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)
                is { Success: true } match ? Regex.Replace(match.Groups["v"].Value, @"\s+", " ").Trim(' ', '.', '_', ':') : string.Empty;
        int? Number(string pattern) => int.TryParse(Regex.Match(text, pattern).Groups["v"].Value, out var value) ? value : null;

        decimal? temperature = null;
        var temperatureText = Regex.Match(text, @"อุณหภูมิ\s*(?<v>\d{2}(?:\.\d+)?)").Groups["v"].Value;
        if (decimal.TryParse(temperatureText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedTemp)) temperature = parsedTemp;
        var pressure = Regex.Match(text, @"ความดันโลหิต\s*(?<s>\d{2,3})\s*/\s*(?<d>\d{2,3})");

        return new WoundCareOcrResultViewModel
        {
            RawText = text,
            ServiceRecipientId = Value(@"เลขประจำตัวผู้รับบริการ", @"วันที่รับบริการ|ชื่อ-สกุล"),
            AuthenticationCode = Regex.Match(text, @"Authentication\s*Code\s*:?\s*(?<v>[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase).Groups["v"].Value,
            WoundCause = Value(@"สาเหตุของแผล", @"ตำแหน่งของแผล"),
            WoundLocation = Value(@"ตำแหน่งของแผล", @"ลักษณะแผล|การทำแผลครั้งที่"),
            WoundSize = Value(@"ขนาดของแผล", @"การทำแผลครั้งที่|ความก้าวหน้า"),
            TemperatureCelsius = temperature,
            PulseRate = Number(@"ชีพจร\s*(?<v>\d{2,3})"),
            RespiratoryRate = Number(@"หายใจ\s*(?<v>\d{1,3})"),
            SystolicPressure = pressure.Success ? int.Parse(pressure.Groups["s"].Value) : null,
            DiastolicPressure = pressure.Success ? int.Parse(pressure.Groups["d"].Value) : null
        };
    }
}
