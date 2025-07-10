using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using InveneWebApp.Models;
using System.Text;

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

    public async Task OnPostAsync()
    {
        // TODO handle file upload

        var allowed = _config.GetSection("PHI:Allowed").Get<List<string>>() ?? new List<string>();
        var denyValue = _config.GetSection("PHI:DenyValue").Get<string>() ?? string.Empty;
        PHIConfig phiConfig = new(allowed, denyValue);

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

            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var outputPath = Path.Combine(wwwrootPath, $"{file.FileName.Split(".").FirstOrDefault()}_sanitized.txt");
            await System.IO.File.WriteAllTextAsync(outputPath, stringBuilder.ToString());
        }
    }

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
