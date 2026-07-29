using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartClinic.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class SmartCardController : ControllerBase
{
    private static readonly Regex CitizenIdRegex = new("^\\d{13}$", RegexOptions.Compiled);
    private const int BridgePort = 9999;
    private const string BridgeInstallPath = @"C:\Program Files\SmartClinic\CardReader";
    private const string BridgeStatusUrl = "http://localhost:9999/status";
    private readonly ApplicationDbContext _context;

    public SmartCardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("reader-status")]
    public async Task<IActionResult> ReaderStatus()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await http.GetAsync(BridgeStatusUrl);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    success = false,
                    hasCardInserted = false,
                    statusText = "เชื่อมต่อ Bridge ไม่สำเร็จ",
                    detail = json
                });
            }

            var payload = JsonSerializer.Deserialize<JsonElement>(json);
            return Ok(payload);
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                hasCardInserted = false,
                statusText = "Bridge ไม่พร้อมใช้งาน",
                error = ex.Message
            });
        }
    }

    [HttpGet("read")]
    public async Task<IActionResult> ReadCard([FromQuery] string citizenId)
    {
        if (string.IsNullOrWhiteSpace(citizenId) || !CitizenIdRegex.IsMatch(citizenId))
        {
            return BadRequest(new
            {
                message = "CitizenId must be 13 digits"
            });
        }

        try
        {
            // ค้นหาผู้ป่วยจากฐานข้อมูล
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.CitizenId == citizenId);

            if (patient != null)
            {
                // ส่งข้อมูลจากฐานข้อมูล
                return Ok(new
                {
                    success = true,
                    citizenId = patient.CitizenId,
                    fullName = patient.FullName,
                    address = patient.Address ?? "",
                    phoneNumber = patient.PhoneNumber ?? "",
                    birthDate = patient.BirthDate?.ToString("yyyy-MM-dd") ?? "",
                    gender = patient.Gender ?? "",
                    source = "database"
                });
            }
            else
            {
                // หากไม่พบในฐานข้อมูล
                return NotFound(new
                {
                    success = false,
                    message = $"No patient found with citizen ID: {citizenId}"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = $"Database error: {ex.Message}"
            });
        }
    }

    [HttpPost("restart-bridge")]
    public async Task<IActionResult> RestartBridge()
    {
        try
        {
            var startScriptPath = Path.Combine(BridgeInstallPath, "start-bridge.bat");
            if (!System.IO.File.Exists(startScriptPath))
            {
                return NotFound(new
                {
                    success = false,
                    error = $"Bridge start script not found at {startScriptPath}"
                });
            }

            try
            {
                using var killProc = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c taskkill /F /IM SmartClinic.CardReader.Bridge.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (killProc is not null)
                {
                    await killProc.WaitForExitAsync();
                }
            }
            catch
            {
                // Ignore errors here; start script contains its own recovery steps.
            }

            using var startProc = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{startScriptPath}\"\"",
                WorkingDirectory = BridgeInstallPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (startProc is not null)
            {
                await startProc.WaitForExitAsync();
            }

            await Task.Delay(1200);

            var isPortListening = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(endpoint => endpoint.Port == BridgePort);

            return Ok(new
            {
                success = isPortListening,
                port = BridgePort,
                message = isPortListening
                    ? "Bridge restarted and port 9999 is listening"
                    : "Restart command sent, but port 9999 is not listening yet"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = $"Restart bridge failed: {ex.Message}"
            });
        }
    }
}
