namespace InveneWebApp.Models;

public class PHIConfig
{
    public HashSet<string> Allowed { get; }
    public string DenyValue { get; }

    public PHIConfig(IEnumerable<string> allowed, string denyValue)
    {
        Allowed = new(allowed, StringComparer.OrdinalIgnoreCase);
        DenyValue = denyValue;
    }
}

