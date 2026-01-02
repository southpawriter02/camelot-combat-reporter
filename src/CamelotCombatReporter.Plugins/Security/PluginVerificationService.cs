using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CamelotCombatReporter.Plugins.Manifest;

namespace CamelotCombatReporter.Plugins.Security;

/// <summary>
/// Service for verifying plugin code signatures.
/// </summary>
public sealed class PluginVerificationService
{
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly HashSet<string> _trustedThumbprints;
    private readonly HashSet<string> _officialThumbprints;

    public PluginVerificationService(
        ISecurityAuditLogger auditLogger,
        IEnumerable<string>? trustedThumbprints = null,
        IEnumerable<string>? officialThumbprints = null)
    {
        _auditLogger = auditLogger;
        _trustedThumbprints = trustedThumbprints?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                             ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _officialThumbprints = officialThumbprints?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                              ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies a plugin's signature and determines its trust level.
    /// </summary>
    public async Task<SignatureVerificationResult> VerifyAsync(
        string pluginDirectory,
        PluginManifest manifest,
        CancellationToken ct = default)
    {
        var assemblyPath = Path.Combine(pluginDirectory, manifest.EntryPoint.Assembly);

        if (!File.Exists(assemblyPath))
        {
            return SignatureVerificationResult.Failed("Assembly file not found");
        }

        try
        {
            // Step 1: Calculate hash of plugin assembly
            var assemblyHash = await ComputeFileHashAsync(assemblyPath, ct);

            // Step 2: Check for Authenticode signature
            var authenticodeResult = await VerifyAuthenticodeAsync(assemblyPath, ct);

            if (!authenticodeResult.IsSigned)
            {
                _auditLogger.LogSecurityEvent(manifest.Id,
                    SecurityEventType.UnsignedPlugin,
                    "Plugin assembly is not signed");

                return SignatureVerificationResult.Unsigned();
            }

            if (!authenticodeResult.IsValid)
            {
                _auditLogger.LogSecurityEvent(manifest.Id,
                    SecurityEventType.InvalidSignature,
                    $"Invalid signature: {authenticodeResult.Error}");

                return SignatureVerificationResult.Invalid(authenticodeResult.Error ?? "Signature validation failed");
            }

            // Step 3: Check if signature matches manifest thumbprint
            if (manifest.Signing?.Thumbprint != null)
            {
                if (!string.Equals(authenticodeResult.Thumbprint, manifest.Signing.Thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    _auditLogger.LogSecurityEvent(manifest.Id,
                        SecurityEventType.InvalidSignature,
                        $"Thumbprint mismatch: expected {manifest.Signing.Thumbprint}, got {authenticodeResult.Thumbprint}");

                    return SignatureVerificationResult.Invalid("Certificate thumbprint does not match manifest");
                }
            }

            // Step 4: Determine trust level based on thumbprint
            var trustLevel = DetermineTrustLevel(authenticodeResult.Thumbprint);

            _auditLogger.LogSecurityEvent(manifest.Id,
                SecurityEventType.SignatureValid,
                $"Valid signature with trust level: {trustLevel}");

            return SignatureVerificationResult.Valid(trustLevel, authenticodeResult.Thumbprint);
        }
        catch (Exception ex)
        {
            _auditLogger.LogSecurityEvent(manifest.Id,
                SecurityEventType.SignatureError,
                $"Verification error: {ex.Message}");

            return SignatureVerificationResult.Failed($"Verification error: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a thumbprint to the trusted list.
    /// </summary>
    public void AddTrustedThumbprint(string thumbprint)
    {
        _trustedThumbprints.Add(thumbprint);
    }

    /// <summary>
    /// Removes a thumbprint from the trusted list.
    /// </summary>
    public void RemoveTrustedThumbprint(string thumbprint)
    {
        _trustedThumbprints.Remove(thumbprint);
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private Task<AuthenticodeResult> VerifyAuthenticodeAsync(string filePath, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                // Try to get the certificate from the signed file
                var certificate = X509Certificate.CreateFromSignedFile(filePath);
                var cert2 = new X509Certificate2(certificate);

                // Verify certificate chain
                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid; // Allow expired for now

                var chainValid = chain.Build(cert2);

                if (!chainValid)
                {
                    var errors = string.Join(", ", chain.ChainStatus.Select(s => s.StatusInformation));
                    return new AuthenticodeResult(
                        IsSigned: true,
                        IsValid: false,
                        Thumbprint: cert2.Thumbprint,
                        Error: $"Certificate chain validation failed: {errors}");
                }

                return new AuthenticodeResult(
                    IsSigned: true,
                    IsValid: true,
                    Thumbprint: cert2.Thumbprint,
                    Error: null);
            }
            catch (CryptographicException)
            {
                // No Authenticode signature
                return new AuthenticodeResult(
                    IsSigned: false,
                    IsValid: false,
                    Thumbprint: null,
                    Error: "Assembly is not signed");
            }
        }, ct);
    }

    private PluginTrustLevel DetermineTrustLevel(string? thumbprint)
    {
        if (thumbprint == null)
        {
            return PluginTrustLevel.Untrusted;
        }

        if (_officialThumbprints.Contains(thumbprint))
        {
            return PluginTrustLevel.OfficialTrusted;
        }

        if (_trustedThumbprints.Contains(thumbprint))
        {
            return PluginTrustLevel.SignedTrusted;
        }

        return PluginTrustLevel.Untrusted;
    }

    private record AuthenticodeResult(
        bool IsSigned,
        bool IsValid,
        string? Thumbprint,
        string? Error);
}

/// <summary>
/// Result of signature verification.
/// </summary>
public sealed record SignatureVerificationResult
{
    public bool IsSuccess { get; init; }
    public bool IsSigned { get; init; }
    public bool IsValid { get; init; }
    public PluginTrustLevel TrustLevel { get; init; }
    public string? Thumbprint { get; init; }
    public string? Error { get; init; }

    private SignatureVerificationResult() { }

    public static SignatureVerificationResult Valid(PluginTrustLevel trustLevel, string? thumbprint) =>
        new()
        {
            IsSuccess = true,
            IsSigned = true,
            IsValid = true,
            TrustLevel = trustLevel,
            Thumbprint = thumbprint
        };

    public static SignatureVerificationResult Unsigned() =>
        new()
        {
            IsSuccess = true,
            IsSigned = false,
            IsValid = false,
            TrustLevel = PluginTrustLevel.Untrusted
        };

    public static SignatureVerificationResult Invalid(string error) =>
        new()
        {
            IsSuccess = false,
            IsSigned = true,
            IsValid = false,
            TrustLevel = PluginTrustLevel.Untrusted,
            Error = error
        };

    public static SignatureVerificationResult Failed(string error) =>
        new()
        {
            IsSuccess = false,
            IsSigned = false,
            IsValid = false,
            TrustLevel = PluginTrustLevel.Untrusted,
            Error = error
        };
}
