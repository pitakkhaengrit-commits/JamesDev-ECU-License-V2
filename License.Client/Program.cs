using License.Core;

string store = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JamesDev ECU License V2", "license.key");
string publicKey = Path.Combine(AppContext.BaseDirectory, "public.pem");
try
{
    if (args.Length == 1 && args[0] == "hwid") Console.WriteLine(LicenseCodec.HardwareId());
    else if (args.Length == 2 && args[0] == "activate")
    {
        if (!LicenseCodec.Verify(args[1], File.ReadAllText(publicKey), LicenseCodec.HardwareId(), out var reason))
            throw new InvalidOperationException(reason);
        Directory.CreateDirectory(Path.GetDirectoryName(store)!);
        File.WriteAllText(store, args[1].Trim());
        Console.WriteLine("Activated: " + reason);
    }
    else if (args.Length == 1 && args[0] == "check")
    {
        if (!LicenseCodec.Verify(File.ReadAllText(store), File.ReadAllText(publicKey), LicenseCodec.HardwareId(), out var reason))
            throw new InvalidOperationException(reason);
        Console.WriteLine(reason);
    }
    else Console.WriteLine("Usage: License.Client hwid | activate <TOKEN> | check");
}
catch (Exception ex) { Console.Error.WriteLine(ex.Message); Environment.ExitCode = 1; }
