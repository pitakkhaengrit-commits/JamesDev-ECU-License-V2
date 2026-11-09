using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace License.Core;

public sealed record LicenseData(string Product, string Hwid, string ExpiresUtc, string LicenseId);

public static class LicenseCodec
{
    private const string Product = "JamesDev-ECU-License-V2";
    public static string HardwareId()
    {
        string? guid = null;
        if (OperatingSystem.IsWindows())
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                guid = key?.GetValue("MachineGuid") as string;
                if (!string.IsNullOrWhiteSpace(guid)) break;
            }
        }
        guid ??= Environment.MachineName + "|" + Environment.OSVersion.VersionString;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("ECU-TUNING-SOLUTIONS-HWID-V1|" + guid.Trim().ToUpperInvariant()));
        return string.Join("-", Enumerable.Range(0, 8).Select(i => Convert.ToHexString(hash.AsSpan(i * 2, 2))));
    }

    public static string Normalize(string input) => new(input.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Decode(string text)
    {
        text = text.Replace('-', '+').Replace('_', '/');
        text = text.PadRight((text.Length + 3) / 4 * 4, '=');
        return Convert.FromBase64String(text);
    }
    public static string Sign(LicenseData license, string privatePem)
    {
        if (license.Product != Product) throw new ArgumentException("Invalid product");
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(license);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privatePem);
        byte[] signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return "ETS2." + Url(payload) + "." + Url(signature);
    }
    public static bool Verify(string? token, string publicPem, string currentHwid, out string reason)
    {
        reason = "Invalid license";
        try
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 8192) return false;
            var parts = token.Trim().Split('.');
            if (parts.Length != 3 || parts[0] != "ETS2") return false;
            byte[] payload = Decode(parts[1]);
            byte[] signature = Decode(parts[2]);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicPem);
            if (!rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            { reason = "Invalid signature"; return false; }
            var data = JsonSerializer.Deserialize<LicenseData>(payload);
            if (data is null || data.Product != Product) { reason = "Wrong product"; return false; }
            if (Normalize(data.Hwid) != Normalize(currentHwid)) { reason = "HWID mismatch"; return false; }
            if (!DateTime.TryParseExact(data.ExpiresUtc, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var expiry))
            { reason = "Invalid expiry"; return false; }
            if (DateTime.UtcNow.Date > expiry.Date) { reason = "Expired"; return false; }
            reason = "Valid through " + data.ExpiresUtc;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException or ArgumentException)
        { reason = "Malformed or invalid license"; return false; }
    }
    public static LicenseData NewLicense(string hwid, string expiry) =>
        new(Product, Normalize(hwid), expiry, Guid.NewGuid().ToString("N"));
}
