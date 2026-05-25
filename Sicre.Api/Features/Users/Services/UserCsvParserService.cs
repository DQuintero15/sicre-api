using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Sicre.Api.Features.Users.Services;

public interface IUserCsvParserService
{
    Task<ParsedUserCsvResult> ParseAsync(IFormFile file);
}

public sealed class UserCsvParserService : IUserCsvParserService
{
    private static readonly HashSet<string> TruthyValues = ["x", "1", "true", "si", "yes", "si"];
    private static readonly UTF8Encoding Utf8Strict = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );
    private static readonly Encoding Latin1Encoding = Encoding.Latin1;
    private static readonly Encoding Windows1252Encoding = GetWindows1252Encoding();

    static UserCsvParserService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<ParsedUserCsvResult> ParseAsync(IFormFile file)
    {
        try
        {
            var content = await ReadFileContentAsync(file);
            var lines = SplitLines(content);

            if (lines.Count == 0)
            {
                return ParsedUserCsvResult.Fail(
                    HttpStatusCode.BadRequest,
                    "El archivo CSV no contiene datos."
                );
            }

            var headerLineNumber = GetFirstNonEmptyLineNumber(lines);
            if (headerLineNumber == -1)
            {
                return ParsedUserCsvResult.Fail(
                    HttpStatusCode.BadRequest,
                    "El archivo CSV no contiene encabezados."
                );
            }

            var headerLine = lines[headerLineNumber - 1];
            var delimiter = DetectDelimiter(headerLine);
            return ParseRows(content, headerLineNumber, delimiter);
        }
        catch (CsvHelperException)
        {
            return ParsedUserCsvResult.Fail(
                HttpStatusCode.BadRequest,
                "El archivo CSV no pudo procesarse. Verifica su formato."
            );
        }
    }

    private static ParsedUserCsvResult ParseRows(
        string content,
        int headerLineNumber,
        char delimiter
    )
    {
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = false,
            IgnoreBlankLines = false,
            BadDataFound = null,
            MissingFieldFound = null,
            DetectColumnCountChanges = false,
        };

        using var textReader = new StringReader(content);
        using var parser = new CsvParser(textReader, configuration);

        Dictionary<string, int>? headerMap = null;
        var rows = new List<ParsedUserCsvRow>();

        while (parser.Read())
        {
            var record = parser.Record;
            if (record == null)
                continue;

            var rowNumber = parser.Row;
            if (rowNumber < headerLineNumber)
                continue;

            if (rowNumber == headerLineNumber)
            {
                headerMap = BuildHeaderMap(record);
                if (!headerMap.ContainsKey("nombre") || !headerMap.ContainsKey("email"))
                {
                    return ParsedUserCsvResult.Fail(
                        HttpStatusCode.BadRequest,
                        "El CSV debe incluir los encabezados nombre y email."
                    );
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(parser.RawRecord))
                continue;

            if (headerMap == null)
            {
                return ParsedUserCsvResult.Fail(
                    HttpStatusCode.BadRequest,
                    "El archivo CSV no contiene encabezados."
                );
            }

            rows.Add(MapRow(record, rowNumber, headerMap));
        }

        return ParsedUserCsvResult.Ok(rows);
    }

    private static ParsedUserCsvRow MapRow(
        IReadOnlyList<string> fields,
        int rowNumber,
        IReadOnlyDictionary<string, int> headerMap
    )
    {
        // Single-role model: first truthy column wins in priority order
        string? desiredRole = null;

        if (
            desiredRole == null
            && IsTruthyValue(GetFieldValue(fields, GetOptionalIndex(headerMap, "administrador")))
        )
            desiredRole = "Administrator";

        if (
            desiredRole == null
            && IsTruthyValue(GetFieldValue(fields, GetOptionalIndex(headerMap, "responsable")))
        )
            desiredRole = "ReportResponsible";

        if (
            desiredRole == null
            && IsTruthyValue(GetFieldValue(fields, GetOptionalIndex(headerMap, "supervisor")))
        )
            desiredRole = "ComplianceSupervisor";

        if (
            desiredRole == null
            && IsTruthyValue(GetFieldValue(fields, GetOptionalIndex(headerMap, "auditoria")))
        )
            desiredRole = "Auditor";

        return new ParsedUserCsvRow
        {
            RowNumber = rowNumber,
            Name = CleanValue(GetFieldValue(fields, headerMap["nombre"])),
            Email = CleanValue(GetFieldValue(fields, headerMap["email"])),
            ProcessName = CleanValue(GetFieldValue(fields, GetOptionalIndex(headerMap, "proceso"))),
            PositionName = CleanValue(GetFieldValue(fields, GetOptionalIndex(headerMap, "cargo"))),
            BranchName = CleanValue(GetFieldValue(fields, GetOptionalIndex(headerMap, "sede"))),
            DesiredRole = desiredRole,
        };
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headers)
    {
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["nombre"] = ["nombre"],
            ["email"] = ["email", "correo", "correoelectronico"],
            ["proceso"] = ["proceso"],
            ["cargo"] = ["cargo"],
            ["administrador"] = ["administrador", "administador"],
            ["responsable"] = ["responsable"],
            ["supervisor"] = ["supervisor"],
            ["auditoria"] = ["auditoria"],
            ["sede"] = ["sede", "sucursal", "branch"],
        };

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headers.Count; i++)
        {
            var normalizedHeader = NormalizeKey(headers[i]);
            foreach (var alias in aliases)
            {
                if (alias.Value.Any(value => NormalizeKey(value) == normalizedHeader))
                {
                    map.TryAdd(alias.Key, i);
                    break;
                }
            }
        }

        return map;
    }

    private static async Task<string> ReadFileContentAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        if (bytes.Length == 0)
            return string.Empty;

        if (TryDecodeByBom(bytes, out var bomDecoded))
            return bomDecoded;

        if (TryDecode(bytes, Utf8Strict, out var utf8Decoded))
            return utf8Decoded;

        if (TryDecode(bytes, Windows1252Encoding, out var cp1252Decoded))
            return cp1252Decoded;

        return RemoveBomPrefix(Latin1Encoding.GetString(bytes));
    }

    private static bool TryDecodeByBom(byte[] bytes, out string decoded)
    {
        var bomEncodings = new Encoding[]
        {
            Encoding.UTF8,
            Encoding.Unicode,
            Encoding.BigEndianUnicode,
            Encoding.UTF32,
            new UTF32Encoding(bigEndian: true, byteOrderMark: true),
        };

        foreach (var encoding in bomEncodings)
        {
            var preamble = encoding.GetPreamble();
            if (preamble.Length == 0 || bytes.Length < preamble.Length)
                continue;

            if (!bytes.AsSpan(0, preamble.Length).SequenceEqual(preamble))
                continue;

            decoded = RemoveBomPrefix(encoding.GetString(bytes));
            return true;
        }

        decoded = string.Empty;
        return false;
    }

    private static bool TryDecode(byte[] bytes, Encoding encoding, out string decoded)
    {
        try
        {
            decoded = RemoveBomPrefix(encoding.GetString(bytes));
            return true;
        }
        catch (DecoderFallbackException)
        {
            decoded = string.Empty;
            return false;
        }
    }

    private static Encoding GetWindows1252Encoding()
    {
        try
        {
            return Encoding.GetEncoding(1252);
        }
        catch (ArgumentException)
        {
            return Latin1Encoding;
        }
        catch (NotSupportedException)
        {
            return Latin1Encoding;
        }
    }

    private static string RemoveBomPrefix(string value)
    {
        return value.Length > 0 && value[0] == '﻿' ? value[1..] : value;
    }

    private static List<string> SplitLines(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').ToList();
    }

    private static int GetFirstNonEmptyLineNumber(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                return i + 1;
        }

        return -1;
    }

    private static char DetectDelimiter(string headerLine)
    {
        var commaCount = CountDelimiterOutsideQuotes(headerLine, ',');
        var semicolonCount = CountDelimiterOutsideQuotes(headerLine, ';');
        return semicolonCount > commaCount ? ';' : ',';
    }

    private static int CountDelimiterOutsideQuotes(string value, char delimiter)
    {
        var count = 0;
        var inQuotes = false;

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '"')
            {
                if (inQuotes && i + 1 < value.Length && value[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && value[i] == delimiter)
                count++;
        }

        return count;
    }

    private static int GetOptionalIndex(IReadOnlyDictionary<string, int> headers, string key)
    {
        return headers.TryGetValue(key, out var index) ? index : -1;
    }

    private static string? GetFieldValue(IReadOnlyList<string> fields, int index)
    {
        if (index < 0 || index >= fields.Count)
            return null;

        return fields[index];
    }

    private static bool IsTruthyValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return TruthyValues.Contains(NormalizeKey(value));
    }

    internal static string CleanValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    internal static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cleaned = CleanValue(value).ToLowerInvariant();
        var decomposed = cleaned.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed class ParsedUserCsvResult
{
    public bool IsValid { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public List<ParsedUserCsvRow> Rows { get; init; } = [];

    public static ParsedUserCsvResult Ok(List<ParsedUserCsvRow> rows) =>
        new()
        {
            IsValid = true,
            StatusCode = HttpStatusCode.OK,
            Rows = rows,
        };

    public static ParsedUserCsvResult Fail(HttpStatusCode statusCode, string errorMessage) =>
        new()
        {
            IsValid = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };
}

public sealed class ParsedUserCsvRow
{
    public int RowNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty;
    public string PositionName { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;

    /// <summary>
    /// Single role for this user. Null means no role column was marked truthy;
    /// the import service will use the configured default role.
    /// </summary>
    public string? DesiredRole { get; init; }
}
