using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Loading;
using CamelotCombatReporter.Plugins.Security;

namespace CamelotCombatReporter.Plugins.Sandbox;

/// <summary>
/// Sandboxed network access for plugins.
/// </summary>
public sealed class NetworkProxy : INetworkAccess, IDisposable
{
    private readonly string _pluginId;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly HttpClient _httpClient;
    private readonly HashSet<string>? _allowedHosts;

    public NetworkProxy(
        string pluginId,
        ISecurityAuditLogger auditLogger,
        IEnumerable<string>? allowedHosts = null,
        TimeSpan? timeout = null)
    {
        _pluginId = pluginId;
        _auditLogger = auditLogger;
        _allowedHosts = allowedHosts?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        _httpClient = new HttpClient
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30)
        };

        // Set a user agent to identify plugin requests
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"CamelotCombatReporter-Plugin/{pluginId}");
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        ValidateRequest(request.RequestUri);

        _auditLogger.LogAccess(_pluginId, SecurityAction.NetworkSend,
            $"{request.Method} {request.RequestUri}");

        return await _httpClient.SendAsync(request, ct);
    }

    public async Task<string> GetStringAsync(string url, CancellationToken ct = default)
    {
        var uri = new Uri(url);
        ValidateRequest(uri);

        _auditLogger.LogAccess(_pluginId, SecurityAction.NetworkConnect, url);

        return await _httpClient.GetStringAsync(uri, ct);
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
    {
        var uri = new Uri(url);
        ValidateRequest(uri);

        _auditLogger.LogAccess(_pluginId, SecurityAction.NetworkConnect, url);

        return await _httpClient.GetByteArrayAsync(uri, ct);
    }

    private void ValidateRequest(Uri? uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        // Only allow HTTP and HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            _auditLogger.LogViolation(_pluginId, SecurityAction.NetworkConnect, uri.ToString());
            throw new PluginSecurityException(_pluginId,
                $"Plugin '{_pluginId}' attempted to use unsupported protocol: {uri.Scheme}");
        }

        // Check allowed hosts if restricted
        if (_allowedHosts != null && !_allowedHosts.Contains(uri.Host))
        {
            _auditLogger.LogViolation(_pluginId, SecurityAction.NetworkConnect, uri.ToString());
            throw new PluginSecurityException(_pluginId,
                $"Plugin '{_pluginId}' attempted to connect to unauthorized host: {uri.Host}");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
