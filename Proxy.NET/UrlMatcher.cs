using Microsoft.Extensions.Options;

public class UrlMatcher : IUrlMatcher
{
    private readonly HashSet<string> _allowedDomains;
    
    public UrlMatcher(IOptions<ProxyOptions> options)
    {
        _allowedDomains = new HashSet<string>(options.Value.AllowedDomains, StringComparer.OrdinalIgnoreCase);
    }
    public bool IsMatch(string url)
    {
        return _allowedDomains.Any(x=> url.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}