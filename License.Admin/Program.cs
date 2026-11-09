using System.Security.Cryptography;
using License.Core;

try
{
    if (args.Length == 1 && args[0] == "init")
    {
        if (File.Exists("private.pem") || File.Exists("public.pem"))
            throw new InvalidOperationException("Key file already exists; refusing to overwrite");
        using var rsa = RSA.Create(2048);
        File.WriteAllText("private.pem", rsa.ExportRSAPrivateKeyPem());
        File.WriteAllText("public.pem", rsa.ExportSubjectPublicKeyInfoPem());
        Console.WriteLine("Created private.pem and public.pem. NEVER upload private.pem.");
    }
    else if (args.Length == 3 && args[0] == "issue")
    {
        if (!DateTime.TryParseExact(args[2], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date) || date.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Expiry must be YYYY-MM-DD and not in the past");
        var token = LicenseCodec.Sign(LicenseCodec.NewLicense(args[1], args[2]), File.ReadAllText("private.pem"));
        Console.WriteLine(token);
    }
    else Console.WriteLine("Usage: License.Admin init | issue <HWID> <YYYY-MM-DD>");
}
catch (Exception ex) { Console.Error.WriteLine(ex.Message); Environment.ExitCode = 1; }
