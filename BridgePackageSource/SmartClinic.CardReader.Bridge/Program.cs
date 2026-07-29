using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;
using PCSC;
using PCSC.Iso7816;

// Register code pages so Encoding.GetEncoding(874) (TIS-620 Thai) works on .NET Core
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// SQL Server connection string for SmartClinic database
const string ConnectionString = "Server=localhost;Database=SmartClinic;Trusted_Connection=True;TrustServerCertificate=True;";
const int BridgePort = 9999;

var listener = new HttpListener();
listener.Prefixes.Add($"http://localhost:{BridgePort}/");

try
{
    listener.Start();
}
catch (HttpListenerException ex) when (ex.ErrorCode == 183)
{
    Console.WriteLine($"Port {BridgePort} is already in use. Please run start-bridge.bat to auto-recover the port.");
    return;
}

Console.WriteLine("SmartClinic Card Reader Bridge");
Console.WriteLine($"WebSocket server listening on ws://localhost:{BridgePort}/card");
Console.WriteLine("Using PC/SC API to read Thai Smart Card or Database Fallback...");
Console.WriteLine("Waiting for connections...");

while (true)
{
    var context = await listener.GetContextAsync();

    if (context.Request.Url is not null && context.Request.Url.AbsolutePath == "/status")
    {
        await HandleStatusRequestAsync(context.Response);
        continue;
    }

    if (!context.Request.IsWebSocketRequest || context.Request.Url is null || context.Request.Url.AbsolutePath != "/card")
    {
        context.Response.StatusCode = 400;
        context.Response.Close();
        continue;
    }

    var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
    _ = Task.Run(async () => await HandleClientAsync(wsContext.WebSocket));
}

static async Task HandleClientAsync(WebSocket webSocket)
{
    try
    {
        var buffer = new byte[4096];
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
        var requestText = Encoding.UTF8.GetString(buffer, 0, result.Count);

        string responseText;
        try
        {
            using var document = JsonDocument.Parse(requestText);
            var citizenId = document.RootElement.TryGetProperty("citizenId", out var idElement)
                ? idElement.GetString()
                : null;

            var hasValidCitizenId = !string.IsNullOrWhiteSpace(citizenId) && citizenId.Length == 13;

            // Always try to read from the actual card first.
            var cardData = await ReadFromSmartCardAsync();

            // If card read failed, fallback to database only when citizenId is valid.
            if ((cardData == null || cardData.Count == 0) && hasValidCitizenId)
            {
                Console.WriteLine("Fallback: Reading from SmartClinic database...");
                cardData = await ReadFromDatabaseAsync(citizenId!);
            }

            if (cardData != null && cardData.Count > 0)
            {
                responseText = JsonSerializer.Serialize(new
                {
                    success = true,
                    citizenId = cardData.GetValueOrDefault("CitizenId", citizenId ?? ""),
                    fullName = cardData.GetValueOrDefault("FullName", ""),
                    thaiFullName = cardData.GetValueOrDefault("ThaiFullName", ""),
                    englishFullName = cardData.GetValueOrDefault("EnglishFullName", ""),
                    address = cardData.GetValueOrDefault("Address", ""),
                    phoneNumber = cardData.GetValueOrDefault("PhoneNumber", ""),
                    birthDate = cardData.GetValueOrDefault("BirthDate", ""),
                    gender = cardData.GetValueOrDefault("Gender", ""),
                    issueDate = cardData.GetValueOrDefault("IssueDate", ""),
                    expiryDate = cardData.GetValueOrDefault("ExpiryDate", ""),
                    issuer = cardData.GetValueOrDefault("Issuer", ""),
                    readerName = cardData.GetValueOrDefault("ReaderName", ""),
                    photoBase64 = cardData.GetValueOrDefault("PhotoBase64", ""),
                    source = cardData.GetValueOrDefault("Source", "unknown")
                });
            }
            else if (!hasValidCitizenId)
            {
                responseText = JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "No smart card detected. Enter 13-digit citizen ID to use database fallback."
                });
            }
            else
            {
                responseText = JsonSerializer.Serialize(new { success = false, error = "Unable to read smart card and no data found in database" });
            }
        }
        catch (Exception ex)
        {
            responseText = JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }

        var responseBytes = Encoding.UTF8.GetBytes(responseText);
        await webSocket.SendAsync(responseBytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WebSocket error: {ex.Message}");
    }
    finally
    {
        if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        webSocket.Dispose();
    }
}

static async Task<Dictionary<string, string>?> ReadFromSmartCardAsync()
{
    var cardEncoding = Encoding.GetEncoding(874); // TIS-620 Thai (CodePagesEncodingProvider registered at startup)

    try
    {
        using (var context = new SCardContext())
        {
            context.Establish(SCardScope.System);
            var readerNames = context.GetReaders();

            if (readerNames.Length == 0)
            {
                Console.WriteLine("No smart card readers found");
                return null;
            }

            Console.WriteLine($"Found {readerNames.Length} reader(s): {string.Join(", ", readerNames)}");

            foreach (var readerName in readerNames)
            {
                try
                {
                    using (var isoReader = new IsoReader(context, readerName, SCardShareMode.Shared, SCardProtocol.Any))
                    {
                        var cardData = new Dictionary<string, string>();
                        Console.WriteLine($"Connected to reader: {readerName}");

                        try
                        {
                            var selectApplet = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
                            {
                                CLA = 0x00,
                                INS = 0xA4,
                                P1 = 0x04,
                                P2 = 0x00,
                                Data = new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x54, 0x48, 0x00, 0x01 }
                            };

                            var selectResponse = isoReader.Transmit(selectApplet);
                            if (!IsSuccessStatus(selectResponse))
                            {
                                Console.WriteLine($"Select applet failed on {readerName}: SW={selectResponse.SW1:X2}{selectResponse.SW2:X2}");
                                continue;
                            }

                            var citizenId = ReadThaiCardField(isoReader, cardEncoding, 0x00, 0x04, 0x0D);
                            var thaiFullName = ReadThaiCardField(isoReader, cardEncoding, 0x00, 0x11, 0x64);
                            var englishFullName = ReadThaiCardField(isoReader, cardEncoding, 0x00, 0x75, 0x64);
                            var birthDateRaw = ReadThaiCardField(isoReader, cardEncoding, 0x00, 0xD9, 0x08);
                            var genderRaw = ReadThaiCardField(isoReader, cardEncoding, 0x00, 0xE1, 0x01);
                            var issuer = ReadThaiCardField(isoReader, cardEncoding, 0x00, 0xF6, 0x64);
                            var issueDateRaw = ReadThaiCardField(isoReader, cardEncoding, 0x01, 0x67, 0x08);
                            var expiryDateRaw = ReadThaiCardField(isoReader, cardEncoding, 0x01, 0x6F, 0x08);
                            var address = ReadThaiCardField(isoReader, cardEncoding, 0x15, 0x79, 0x64);

                            // Read photo (JPEG binary, returned as base64)
                            var photoBase64 = ReadThaiCardPhoto(isoReader);

                            if (!string.IsNullOrWhiteSpace(citizenId)) cardData["CitizenId"] = citizenId;
                            if (!string.IsNullOrWhiteSpace(thaiFullName))
                            {
                                cardData["ThaiFullName"] = thaiFullName;
                                cardData["FullName"] = thaiFullName;
                            }
                            if (!string.IsNullOrWhiteSpace(englishFullName)) cardData["EnglishFullName"] = englishFullName;
                            if (!string.IsNullOrWhiteSpace(address)) cardData["Address"] = address;

                            var birthDate = ParseThaiDateString(birthDateRaw);
                            if (!string.IsNullOrWhiteSpace(birthDate)) cardData["BirthDate"] = birthDate;

                            var issueDate = ParseThaiDateString(issueDateRaw);
                            if (!string.IsNullOrWhiteSpace(issueDate)) cardData["IssueDate"] = issueDate;

                            var expiryDate = ParseThaiDateString(expiryDateRaw);
                            if (!string.IsNullOrWhiteSpace(expiryDate)) cardData["ExpiryDate"] = expiryDate;

                            var gender = NormalizeGender(genderRaw);
                            if (!string.IsNullOrWhiteSpace(gender)) cardData["Gender"] = gender;
                            if (!string.IsNullOrWhiteSpace(issuer)) cardData["Issuer"] = issuer;
                            if (!string.IsNullOrWhiteSpace(photoBase64)) cardData["PhotoBase64"] = photoBase64;

                            cardData["ReaderName"] = readerName;
                            cardData["Source"] = "smartcard-reader";

                            if (cardData.Count > 0)
                            {
                                Console.WriteLine("Successfully read card data");
                                return cardData;
                            }
                        }
                        catch (Exception readEx)
                        {
                            Console.WriteLine($"Error reading card data: {readEx.Message}");
                            continue;
                        }
                    }
                }
                catch (Exception readerEx)
                {
                    Console.WriteLine($"Error with reader {readerName}: {readerEx.Message}");
                    continue;
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"PC/SC Error: {ex.Message}");
    }

    return null;
}

static async Task HandleStatusRequestAsync(HttpListenerResponse response)
{
    try
    {
        var payload = GetReaderStatus();
        var json = JsonSerializer.Serialize(payload);
        var buffer = Encoding.UTF8.GetBytes(json);

        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }
    catch (Exception ex)
    {
        var json = JsonSerializer.Serialize(new
        {
            success = false,
            hasCardInserted = false,
            statusText = "ไม่สามารถอ่านสถานะเครื่องอ่านได้",
            error = ex.Message
        });
        var buffer = Encoding.UTF8.GetBytes(json);
        response.StatusCode = 500;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }
    finally
    {
        response.OutputStream.Close();
        response.Close();
    }
}

static object GetReaderStatus()
{
    using var context = new SCardContext();
    context.Establish(SCardScope.System);

    var readerNames = context.GetReaders();
    if (readerNames.Length == 0)
    {
        return new
        {
            success = true,
            hasReader = false,
            hasCardInserted = false,
            statusText = "ไม่พบเครื่องอ่านบัตร",
            readers = Array.Empty<object>()
        };
    }

    var readers = new List<object>();
    var hasCardInserted = false;

    foreach (var readerName in readerNames)
    {
        using var reader = new SCardReader(context);
        var connectResult = reader.Connect(readerName, SCardShareMode.Shared, SCardProtocol.Any);

        var isCardPresent = connectResult == SCardError.Success;
        if (isCardPresent)
        {
            hasCardInserted = true;
            reader.Disconnect(SCardReaderDisposition.Leave);
        }

        var stateText = isCardPresent ? "เสียบบัตรแล้ว" : "ถอดบัตรอยู่";
        readers.Add(new
        {
            readerName,
            isCardPresent,
            stateText,
            detail = connectResult.ToString()
        });
    }

    return new
    {
        success = true,
        hasReader = true,
        hasCardInserted,
        statusText = hasCardInserted ? "เสียบบัตรแล้ว" : "ถอดบัตรอยู่",
        readers
    };
}

static string ReadThaiCardField(IsoReader isoReader, Encoding cardEncoding, byte p1, byte p2, byte length)
{
    var data = ReadThaiCardBinaryField(isoReader, p1, p2, length);
    if (data == null || data.Length == 0) return string.Empty;
    return NormalizeCardText(cardEncoding.GetString(data));
}

static byte[]? ReadThaiCardBinaryField(IsoReader isoReader, byte p1, byte p2, byte length)
{
    var readBinary = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
    {
        CLA = 0x80,
        INS = 0xB0,
        P1 = p1,
        P2 = p2,
        Data = new byte[] { 0x00, length }
    };

    var response = isoReader.Transmit(readBinary);

    // Thai ID cards often return 61xx and require GET RESPONSE to fetch the data payload.
    if (response.SW1 == 0x61)
    {
        var expectedLength = response.SW2 == 0x00 ? length : response.SW2;
        var getResponse = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
        {
            CLA = 0x00,
            INS = 0xC0,
            P1 = 0x00,
            P2 = 0x00,
            Le = expectedLength
        };

        response = isoReader.Transmit(getResponse);
    }

    if (!IsSuccessStatus(response))
    {
        Console.WriteLine($"  APDU[{p1:X2}{p2:X2}] SW={response.SW1:X2}{response.SW2:X2} (fail)");
        return null;
    }
    return response.GetData();
}

// Read photo JPEG from card. Thai EID photo is stored as 20 binary chunks
// starting at 0x017B, with each following chunk offset advanced by 0xFF.
static string? ReadThaiCardPhoto(IsoReader isoReader)
{
    try
    {
        const int firstPhotoOffset = 0x017B;
        const int chunkSize = 0xFF;
        const int chunkCount = 20;
        var allBytes = new List<byte>(chunkSize * chunkCount);

        Console.WriteLine("Photo: accumulating Thai EID photo chunks from offset 0x017B...");

        // Step 1: Accumulate ALL bytes first (do NOT scan per-chunk — SOI may span chunk boundary)
        for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            var offset = firstPhotoOffset + (chunkIndex * chunkSize);
            var p1 = (byte)((offset >> 8) & 0xFF);
            var p2 = (byte)(offset & 0xFF);
            var chunk = ReadThaiCardBinaryField(isoReader, p1, p2, chunkSize);

            if (chunk == null || chunk.Length == 0)
            {
                Console.WriteLine($"Photo: chunk {chunkIndex + 1}/{chunkCount} at 0x{offset:X4} returned no data");
                break;
            }

            allBytes.AddRange(chunk);
        }

        Console.WriteLine($"Photo: total accumulated = {allBytes.Count} bytes");

        if (allBytes.Count == 0) return null;

        // Step 2: Scan ENTIRE accumulated buffer for JPEG SOI (FF D8 FF)
        int soiIndex = -1;
        for (int i = 0; i <= allBytes.Count - 3; i++)
        {
            if (allBytes[i] == 0xFF && allBytes[i + 1] == 0xD8 && allBytes[i + 2] == 0xFF)
            {
                soiIndex = i;
                Console.WriteLine($"Photo: SOI (FF D8 FF) found at byte index {soiIndex}");
                break;
            }
        }

        if (soiIndex < 0)
        {
            // Log first 16 bytes for diagnosis
            var preview = string.Join(" ", allBytes.Take(16).Select(b => $"{b:X2}"));
            Console.WriteLine($"Photo: no JPEG SOI in {allBytes.Count} bytes. First bytes: {preview}");
            return null;
        }

        // Step 3: Find JPEG EOI (FF D9) scanning backward from end
        int eoiIndex = -1;
        for (int i = allBytes.Count - 2; i > soiIndex + 4; i--)
        {
            if (allBytes[i] == 0xFF && allBytes[i + 1] == 0xD9)
            {
                eoiIndex = i;
                break;
            }
        }

        byte[] jpeg;
        if (eoiIndex >= 0)
        {
            jpeg = allBytes.Skip(soiIndex).Take(eoiIndex - soiIndex + 2).ToArray();
            Console.WriteLine($"Photo: JPEG extracted {jpeg.Length} bytes (SOI={soiIndex}, EOI={eoiIndex})");
        }
        else
        {
            jpeg = allBytes.Skip(soiIndex).ToArray();
            Console.WriteLine($"Photo: JPEG (no EOI found) {jpeg.Length} bytes");
        }

        return Convert.ToBase64String(jpeg);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Photo read exception: {ex.Message}");
    }

    return null;
}

static bool IsSuccessStatus(Response response)
{
    return response.SW1 == 0x90 && response.SW2 == 0x00;
}

static string NormalizeCardText(string value)
{
    var normalized = value
        .Replace("#", " ")
        .Replace("\0", " ")
        .Trim();

    normalized = Regex.Replace(normalized, "\\s+", " ");
    return normalized;
}

static string ParseThaiDateString(string value)
{
    var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
    if (digits.Length != 8)
    {
        return string.Empty;
    }

    if (!DateTime.TryParseExact(digits, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
    {
        return string.Empty;
    }

    // Thai ID cards commonly store Buddhist Era years.
    if (date.Year > 2400)
    {
        date = date.AddYears(-543);
    }

    return date.ToString("yyyy-MM-dd");
}

static string NormalizeGender(string genderRaw)
{
    var value = (genderRaw ?? string.Empty).Trim();
    if (value == "1") return "ชาย";
    if (value == "2") return "หญิง";
    return value;
}

static async Task<Dictionary<string, string>?> ReadFromDatabaseAsync(string citizenId)
{
    try
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            Console.WriteLine($"Connected to SmartClinic database, searching for citizen ID: {citizenId}");

            // Query to find patient by citizen ID
            var query = @"
                SELECT TOP 1 
                    CitizenId, 
                    FullName,
                    Address,
                    PhoneNumber,
                    BirthDate,
                    Gender
                FROM dbo.Patients
                WHERE CitizenId = @CitizenId
            ";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CitizenId", citizenId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var cardData = new Dictionary<string, string>();
                        
                        if (!reader.IsDBNull(0)) cardData["CitizenId"] = reader.GetString(0);
                        if (!reader.IsDBNull(1))
                        {
                            var fullNameValue = reader.GetString(1);
                            cardData["FullName"] = fullNameValue;
                            cardData["ThaiFullName"] = fullNameValue;
                        }
                        if (!reader.IsDBNull(2)) cardData["Address"] = reader.GetString(2);
                        if (!reader.IsDBNull(3)) cardData["PhoneNumber"] = reader.GetString(3);
                        if (!reader.IsDBNull(4))
                        {
                            var birthRaw = reader.GetValue(4);
                            if (birthRaw is DateTime birthDateTime)
                            {
                                cardData["BirthDate"] = birthDateTime.ToString("yyyy-MM-dd");
                            }
                            else if (birthRaw is DateOnly birthDateOnly)
                            {
                                cardData["BirthDate"] = birthDateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                            }
                        }
                        if (!reader.IsDBNull(5)) cardData["Gender"] = reader.GetString(5);
                        
                        cardData["Source"] = "database";
                        
                        var fullName = cardData.ContainsKey("FullName") ? cardData["FullName"] : "Unknown";
                        Console.WriteLine($"Found patient in database: {fullName}");
                        return cardData;
                    }
                }
            }

            Console.WriteLine($"No patient found in database with citizen ID: {citizenId}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database Error: {ex.Message}");
    }

    return null;
}
