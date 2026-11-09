using License.Core;

string store = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JamesDev ECU License V2", "license.key");
string publicKey = Path.Combine(AppContext.BaseDirectory, "public.pem");

void Execute(string[] command)
{
    if (command.Length == 1 && command[0] == "hwid")
        Console.WriteLine("Your HWID: " + LicenseCodec.HardwareId());
    else if (command.Length == 2 && command[0] == "activate")
    {
        if (!LicenseCodec.Verify(command[1], File.ReadAllText(publicKey), LicenseCodec.HardwareId(), out var reason))
            throw new InvalidOperationException(reason);
        Directory.CreateDirectory(Path.GetDirectoryName(store)!);
        File.WriteAllText(store, command[1].Trim());
        Console.WriteLine("Activated: " + reason);
    }
    else if (command.Length == 1 && command[0] == "check")
    {
        if (!LicenseCodec.Verify(File.ReadAllText(store), File.ReadAllText(publicKey), LicenseCodec.HardwareId(), out var reason))
            throw new InvalidOperationException(reason);
        Console.WriteLine("License: " + reason);
    }
    else Console.WriteLine("Usage: License.Client hwid | activate <TOKEN> | check");
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
        Console.WriteLine("\n=== JamesDev ECU License V2 - CLIENT ===");
        Console.WriteLine("1. Show this PC's HWID");
        Console.WriteLine("2. Activate license");
        Console.WriteLine("3. Check license");
        Console.WriteLine("0. Exit");
        Console.Write("Select: ");
        string? choice = Console.ReadLine();
        if (choice is null or "0") break;
        try
        {
            if (choice == "1") Execute(new[] { "hwid" });
            else if (choice == "2")
            {
                Console.Write("Paste ETS2 license token: ");
                string token = Console.ReadLine()?.Trim() ?? "";
                Execute(new[] { "activate", token });
            }
            else if (choice == "3") Execute(new[] { "check" });
            else Console.WriteLine("Unknown option.");
        }
        catch (Exception ex) { Console.WriteLine("ERROR: " + ex.Message); }
        Console.WriteLine("Press ENTER to return to menu...");
        Console.ReadLine();
    }
}
