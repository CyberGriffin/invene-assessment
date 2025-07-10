using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using InveneWebApp.Models;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace InveneWebApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _config;

    public IndexModel(ILogger<IndexModel> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public void OnGet()
    {

    }

    /// <summary>
    /// Handles POST requests for file uploads.
    /// Processes each file and redacts PHI.
    /// </summary>
    /// <returns>A File download prompt or redirect</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var allowed = _config.GetSection("PHI:Allowed").Get<List<string>>() ?? new List<string>();
        var denyValue = _config.GetSection("PHI:DenyValue").Get<string>() ?? string.Empty;
        PHIConfig phiConfig = new(allowed, denyValue);

        var filesToZip = new List<(string FileName, byte[] Content)>();

        foreach (var file in Request.Form.Files)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            var stringBuilder = new StringBuilder();
            bool parentWasRedacted = false;
            while (await reader.ReadLineAsync() is { } line)
            {
                var processedLine = ProcessLine(line, phiConfig, ref parentWasRedacted);
                stringBuilder.AppendLine(processedLine);
            }

            var content = Encoding.UTF8.GetBytes(stringBuilder.ToString());
            var filename = $"{Path.GetFileNameWithoutExtension(file.FileName)}_sanitized.txt";
            filesToZip.Add((filename, content));
        }

        if (filesToZip.Count == 1)
        {
            return File(filesToZip[0].Content, "text/plain", filesToZip[0].FileName);
        }
        else if (filesToZip.Count > 1)
        {
            using var zipStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))            foreach (var (filename, content) in filesToZip)
            {
                var entry = archive.CreateEntry(filename);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(content, 0, content.Length);
            }
            zipStream.Position = 0;
            return File(zipStream.ToArray(), "application/zip", "sanitized_files.zip");
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Processes a line of input, redacting PHI based on configuration.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="phiConfig"></param>
    /// <param name="parentWasRedacted"></param>
    /// <returns>A processed line with redacted PHI.</returns>
    private static string ProcessLine(string line, PHIConfig phiConfig, ref bool parentWasRedacted)
    {
        // Removing the list markers for easier matching
        var cleaned = ListMarkerRegex.Replace(line, "");

        // Match with any key, with or without a value
        var keyMatch = KeyRegex.Match(line);
        var keyMatchClean = KeyRegex.Match(cleaned);

        if (keyMatchClean.Success)
        {
            // Found key
            var keyClean = keyMatchClean.Groups[1].Value.Trim();
            var key = keyMatch.Groups[1].Value.Trim();
            var valueMatch = ValueRegex.Match(cleaned);
            var value = valueMatch.Success ? valueMatch.Groups[1].Value.Trim() : string.Empty;

            if (phiConfig.Allowed.Contains(keyClean))
            {
                parentWasRedacted = false;
                return $"{key}: {value}";
            }
            parentWasRedacted = true;
            return value != string.Empty ? $"{key}: [{phiConfig.DenyValue}]" : $"{key}:";
        }
        else
        {
            // Did not find key (likely a list item)
            if (parentWasRedacted)
            {
                var listMarkerMatch = ListMarkerRegex.Match(line);
                return listMarkerMatch.Success ? $"{listMarkerMatch.Value}[{phiConfig.DenyValue}]" : $"[{phiConfig.DenyValue}]";
            }
            return line;
        }
    }

    private static readonly Regex ListMarkerRegex = new(@"^\s*([-*•+]|(\d+\.)|(\d+\))|[a-zA-Z]\))\s*", RegexOptions.Compiled);
    private static readonly Regex KeyRegex = new(@"^(.+?):", RegexOptions.Compiled);
    private static readonly Regex ValueRegex = new(@"^[^:]+:\s*(.*)$", RegexOptions.Compiled);
}
