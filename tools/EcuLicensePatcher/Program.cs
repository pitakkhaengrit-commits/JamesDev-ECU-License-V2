using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Security.Cryptography;

// Patch a COPY. Use the target .NET Framework metadata for ALL injected references.
// Importing reflection members from a .NET 8 process would inject incompatible
// System.Private.CoreLib/System.Runtime references into a .NET Framework EXE.
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: EcuLicensePatcher.exe <original-community-v2.exe> <output-patched.exe>");
    return 2;
}

try
{
    string input = Path.GetFullPath(args[0]);
    string output = Path.GetFullPath(args[1]);
    if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Never overwrite the original EXE.");
    if (!File.Exists(input)) throw new FileNotFoundException("Original EXE not found", input);
    if (File.Exists(output)) throw new IOException("Output already exists; choose a new filename.");

    using var module = ModuleDefMD.Load(input);
    if (module.CorLibTypes.AssemblyRef.Name.ToString() != "mscorlib")
        throw new InvalidDataException("This tool supports only the original .NET Framework build.");

    var store = module.Find("EcuTuningCommunity.LicenseStore", false)
        ?? throw new InvalidDataException("EcuTuningCommunity.LicenseStore not found.");
    var hasLicense = store.Methods.SingleOrDefault(m =>
        m.Name == "HasValidLicense" && m.Parameters.Count == 1 &&
        m.MethodSig?.RetType.ElementType == ElementType.Boolean)
        ?? throw new InvalidDataException("HasValidLicense(string) signature mismatch.");

    var appDomain = new TypeRefUser(module, "System", "AppDomain", module.CorLibTypes.AssemblyRef);
    var file = new TypeRefUser(module, "System.IO", "File", module.CorLibTypes.AssemblyRef);
    var path = new TypeRefUser(module, "System.IO", "Path", module.CorLibTypes.AssemblyRef);
    var systemRef = module.GetAssemblyRefs().FirstOrDefault(a => a.Name.ToString() == "System")
        ?? new AssemblyRefUser(new AssemblyNameInfo("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
    var process = new TypeRefUser(module, "System.Diagnostics", "Process", systemRef);
    var processSig = new ClassSig(process);
    var appDomainSig = new ClassSig(appDomain);
    var getDomain = new MemberRefUser(module, "get_CurrentDomain",
        MethodSig.CreateStatic(appDomainSig), appDomain);
    var getBaseDir = new MemberRefUser(module, "get_BaseDirectory",
        MethodSig.CreateInstance(module.CorLibTypes.String), appDomain);
    var combine = new MemberRefUser(module, "Combine",
        MethodSig.CreateStatic(module.CorLibTypes.String,
            module.CorLibTypes.String, module.CorLibTypes.String), path);
    var exists = new MemberRefUser(module, "Exists",
        MethodSig.CreateStatic(module.CorLibTypes.Boolean, module.CorLibTypes.String), file);
    var start = new MemberRefUser(module, "Start",
        MethodSig.CreateStatic(processSig, module.CorLibTypes.String, module.CorLibTypes.String), process);
    var wait = new MemberRefUser(module, "WaitForExit",
        MethodSig.CreateInstance(module.CorLibTypes.Void), process);
    var exitCode = new MemberRefUser(module, "get_ExitCode",
        MethodSig.CreateInstance(module.CorLibTypes.Int32), process);

    // Log markers in %TEMP% so silent startup failure can be diagnosed.
    var getTemp = new MemberRefUser(module, "GetTempPath",
        MethodSig.CreateStatic(module.CorLibTypes.String), path);
    var append = new MemberRefUser(module, "AppendAllText",
        MethodSig.CreateStatic(module.CorLibTypes.Void,
            module.CorLibTypes.String, module.CorLibTypes.String), file);

    var body = new CilBody { InitLocals = true, MaxStack = 3 };
    hasLicense.Body = body;
    var helperPath = new Local(module.CorLibTypes.String);
    var proc = new Local(processSig);
    body.Variables.Add(helperPath);
    body.Variables.Add(proc);
    var found = Instruction.Create(OpCodes.Ldloc, helperPath);
    var started = Instruction.Create(OpCodes.Ldloc, proc);
    var il = body.Instructions;
    il.Add(Instruction.Create(OpCodes.Call, getDomain));
    il.Add(Instruction.Create(OpCodes.Callvirt, getBaseDir));
    il.Add(Instruction.Create(OpCodes.Ldstr, "License.Client.exe"));
    il.Add(Instruction.Create(OpCodes.Call, combine));
    il.Add(Instruction.Create(OpCodes.Stloc, helperPath));
    il.Add(Instruction.Create(OpCodes.Ldloc, helperPath));
    il.Add(Instruction.Create(OpCodes.Call, exists));
    il.Add(Instruction.Create(OpCodes.Brtrue, found));
    il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
    il.Add(Instruction.Create(OpCodes.Ret));
    il.Add(found);
    il.Add(Instruction.Create(OpCodes.Ldstr, "check"));
    il.Add(Instruction.Create(OpCodes.Call, start));
    il.Add(Instruction.Create(OpCodes.Stloc, proc));
    il.Add(Instruction.Create(OpCodes.Ldloc, proc));
    il.Add(Instruction.Create(OpCodes.Brtrue, started));
    il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
    il.Add(Instruction.Create(OpCodes.Ret));
    il.Add(started);
    il.Add(Instruction.Create(OpCodes.Callvirt, wait));
    il.Add(Instruction.Create(OpCodes.Ldloc, proc));
    il.Add(Instruction.Create(OpCodes.Callvirt, exitCode));
    il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
    il.Add(Instruction.Create(OpCodes.Ceq));
    il.Add(Instruction.Create(OpCodes.Ret));

    var program = module.Find("EcuTuningCommunity.Program", false);
    var main = program?.Methods.FirstOrDefault(m => m.Name == "Main" && m.Body is not null)
        ?? throw new InvalidDataException("Expected Program.Main not found.");
    static List<Instruction> Marker(string tag, IMethod getTemp, IMethod combine, IMethod append)
    {
        return new()
        {
            Instruction.Create(OpCodes.Call, getTemp),
            Instruction.Create(OpCodes.Ldstr, "JamesDev-ECU-V2-diagnostic.log"),
            Instruction.Create(OpCodes.Call, combine),
            Instruction.Create(OpCodes.Ldstr, tag + "\\r\\n"),
            Instruction.Create(OpCodes.Call, append)
        };
    }
    var mainIl = main.Body.Instructions;
    foreach (var marker in Marker("Main entered", getTemp, combine, append).AsEnumerable().Reverse())
        mainIl.Insert(0, marker);
    foreach (var target in mainIl.Where(i => i.OpCode == OpCodes.Call &&
                 i.Operand is IMethod m &&
                 (m.Name == "HasValidLicense" || m.Name == "LaunchCore")).ToArray())
    {
        string name = ((IMethod)target.Operand).Name.ToString();
        int index = mainIl.IndexOf(target);
        foreach (var marker in Marker("Before " + name, getTemp, combine, append))
            mainIl.Insert(index++, marker);
    }

    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    module.Write(output);
    using var verify = ModuleDefMD.Load(output);
    if (verify.GetAssemblyRefs().Any(a => a.Name.ToString() is "System.Private.CoreLib" or "System.Runtime"))
    {
        File.Delete(output);
        throw new InvalidDataException("Incompatible .NET Core assembly reference found; output discarded.");
    }
    Console.WriteLine("TEST copy written: " + output);
    Console.WriteLine("Original SHA256: " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))));
    Console.WriteLine("Patched SHA256:  " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))));
    Console.WriteLine("After test, check %TEMP%\\JamesDev-ECU-V2-diagnostic.log");
    Console.WriteLine("Experimental only; use an isolated VM, no ECU connected.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("PATCH FAILED: " + ex);
    return 1;
}
