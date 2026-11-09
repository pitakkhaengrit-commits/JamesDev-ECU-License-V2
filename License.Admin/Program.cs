using System.Security.Cryptography;
using License.Core;

string privateKeyPath = Path.Combine(AppContext.BaseDirectory, "private.pem");
string publicKeyPath = Path.Combine(AppContext.BaseDirectory, "public.pem");

void Execute(string[] command)
{
    if (command.Length == 1 && command[0] == "init")
    {
        if (File.Exists(privateKeyPath) || File.Exists(publicKeyPath))
            throw new InvalidOperationException("Key file already exists; refusing to overwrite.");
        using var rsa = RSA.Create(2048);
        File.WriteAllText(privateKeyPath, rsa.ExportRSAPrivateKeyPem());
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        Console.WriteLine("Created private.pem and public.pem beside the EXE. Keep private.pem SECRET.");
    }
    else if (command.Length == 3 && command[0] == "issue")
    {
        if (!DateTime.TryParseExact(command[2], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date) || date.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Expiry must be YYYY-MM-DD and not in the past.");
        var token = LicenseCodec.Sign(LicenseCodec.NewLicense(command[1], command[2]), File.ReadAllText(privateKeyPath));
        Console.WriteLine("\nLICENSE TOKEN (copy entire line):\n" + token + "\n");
    }
    else Console.WriteLine("Usage: License.Admin init | issue <HWID> <YYYY-MM-DD>");
}

if (args.Length > 0)
{
    try { Execute(args); }
    catch (Exception ex) { Console.Error.WriteLine("ERROR: " + ex.Message); Environment.ExitCode = 1; }
}
else
{
    while (true)
    {
        Console.WriteLine("\n=== JamesDev ECU License V2 - ADMIN ===");
        Console.WriteLine("1. Generate RSA keys (first use only)");
        Console.WriteLine("2. Issue a license");
        Console.WriteLine("0. Exit");
        Console.Write("Select: ");
        string? choice = Console.ReadLine();
        if (choice is null or "0") break;
        try
        {
            if (choice == "1") Execute(new[] { "init" });
            else if (choice == "2")
            {
                Console.Write("Customer HWID: ");
                string hwid = Console.ReadLine()?.Trim() ?? "";
                Console.Write("Expiry (YYYY-MM-DD): ");
                string expiry = Console.ReadLine()?.Trim() ?? "";
                Execute(new[] { "issue", hwid, expiry });
            }
            else Console.WriteLine("Unknown option.");
        }
        catch (Exception ex) { Console.WriteLine("ERROR: " + ex.Message); }
        Console.WriteLine("Press ENTER to return to menu...");
        Console.ReadLine();
    }
}
