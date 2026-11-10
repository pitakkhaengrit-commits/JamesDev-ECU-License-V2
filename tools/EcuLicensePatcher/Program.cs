using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: EcuLicensePatcher.exe <original-community-v2.exe> <output-patched.exe>");
    return 2;
}
var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Input and output must be different; never overwrite the original.");
if (!File.Exists(input)) throw new FileNotFoundException("Original EXE not found", input);
if (File.Exists(output)) throw new IOException("Output already exists. Refusing to overwrite.");
using var module = ModuleDefMD.Load(input);
var licenseStore = module.Find("EcuTuningCommunity.LicenseStore", false)
    ?? throw new InvalidDataException("Expected LicenseStore was not found. Wrong EXE version?");
var method = licenseStore.Methods.SingleOrDefault(m => m.Name == "HasValidLicense" && m.Parameters.Count == 1)
    ?? throw new InvalidDataException("Expected HasValidLicense(string) not found.");
if (method.MethodSig?.RetType.ElementType != ElementType.Boolean)
    throw new InvalidDataException("Unexpected HasValidLicense return type.");

var importer = new Importer(module);
var getCurrentDomain = importer.Import(typeof(AppDomain).GetProperty("CurrentDomain")!.GetMethod!);
var getBaseDirectory = importer.Import(typeof(AppDomain).GetProperty("BaseDirectory")!.GetMethod!);
var combine = importer.Import(typeof(Path).GetMethod("Combine", new[] { typeof(string), typeof(string) })!);
var fileExists = importer.Import(typeof(File).GetMethod("Exists", new[] { typeof(string) })!);
var ctor = importer.Import(typeof(ProcessStartInfo).GetConstructor(new[] { typeof(string), typeof(string) })!);
var setUseShell = importer.Import(typeof(ProcessStartInfo).GetProperty("UseShellExecute")!.SetMethod!);
var setCreateNoWindow = importer.Import(typeof(ProcessStartInfo).GetProperty("CreateNoWindow")!.SetMethod!);
var processStart = importer.Import(typeof(Process).GetMethod("Start", new[] { typeof(ProcessStartInfo) })!);
var wait = importer.Import(typeof(Process).GetMethod("WaitForExit", Type.EmptyTypes)!);
var exitCode = importer.Import(typeof(Process).GetProperty("ExitCode")!.GetMethod!);

var body = new CilBody { InitLocals = true, MaxStack = 3 };
method.Body = body;
var exe = new Local(module.CorLibTypes.String);
var psi = new Local(importer.Import(typeof(ProcessStartInfo)).ToTypeSig());
var process = new Local(importer.Import(typeof(Process)).ToTypeSig());
body.Variables.Add(exe);
body.Variables.Add(psi);
body.Variables.Add(process);
var yes = Instruction.Create(OpCodes.Ldloc, exe);
var hasProcess = Instruction.Create(OpCodes.Ldloc, process);
var il = body.Instructions;
il.Add(Instruction.Create(OpCodes.Call, getCurrentDomain));
il.Add(Instruction.Create(OpCodes.Callvirt, getBaseDirectory));
il.Add(Instruction.Create(OpCodes.Ldstr, "License.Client.exe"));
il.Add(Instruction.Create(OpCodes.Call, combine));
il.Add(Instruction.Create(OpCodes.Stloc, exe));
il.Add(Instruction.Create(OpCodes.Ldloc, exe));
il.Add(Instruction.Create(OpCodes.Call, fileExists));
il.Add(Instruction.Create(OpCodes.Brtrue, yes));
il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
il.Add(Instruction.Create(OpCodes.Ret));
il.Add(yes);
il.Add(Instruction.Create(OpCodes.Ldstr, "check"));
il.Add(Instruction.Create(OpCodes.Newobj, ctor));
il.Add(Instruction.Create(OpCodes.Stloc, psi));
il.Add(Instruction.Create(OpCodes.Ldloc, psi));
il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
il.Add(Instruction.Create(OpCodes.Callvirt, setUseShell));
il.Add(Instruction.Create(OpCodes.Ldloc, psi));
il.Add(Instruction.Create(OpCodes.Ldc_I4_1));
il.Add(Instruction.Create(OpCodes.Callvirt, setCreateNoWindow));
il.Add(Instruction.Create(OpCodes.Ldloc, psi));
il.Add(Instruction.Create(OpCodes.Call, processStart));
il.Add(Instruction.Create(OpCodes.Stloc, process));
il.Add(Instruction.Create(OpCodes.Ldloc, process));
il.Add(Instruction.Create(OpCodes.Brtrue, hasProcess));
il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
il.Add(Instruction.Create(OpCodes.Ret));
il.Add(hasProcess);
il.Add(Instruction.Create(OpCodes.Callvirt, wait));
il.Add(Instruction.Create(OpCodes.Ldloc, process));
il.Add(Instruction.Create(OpCodes.Callvirt, exitCode));
il.Add(Instruction.Create(OpCodes.Ldc_I4_0));
il.Add(Instruction.Create(OpCodes.Ceq));
il.Add(Instruction.Create(OpCodes.Ret));

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
module.Write(output);
Console.WriteLine("Patched copy written: " + output);
Console.WriteLine("Original SHA256: " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))));
Console.WriteLine("Patched SHA256:  " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))));
Console.WriteLine("This is an experimental patch; TEST ONLY IN ISOLATED WINDOWS VM.");
return 0;
