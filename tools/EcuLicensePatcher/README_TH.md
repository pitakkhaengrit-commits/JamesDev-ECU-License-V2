# Experimental ECU License V2 patcher

**NOT VERIFIED FOR PRODUCTION. This tool does not contain ECU-Tuning-Solutions.exe.**

This experimental tool modifies a COPY of the .NET Framework 4.8 launcher `EcuTuningCommunity.LicenseStore.HasValidLicense(string)` to call `License.Client.exe check`. It preserves the embedded ECU payload in the assembly but has not been tested to ensure the loader still functions. It does not rewrite `ActivationForm`: a V2 activation must be completed in License.Client *before* running the patched ECU application.

## Usage on isolated Windows VM

1. Copy original community-v2 EXE to a clean VM. Keep pristine original and hash.
2. Download patcher artifact from GitHub Actions.
3. Run `EcuLicensePatcher.exe original.exe ECU-Tuning-V2-TEST.exe` from CMD.
4. Copy `License.Client.exe` and `public.pem` to the **same folder as** `ECU-Tuning-V2-TEST.exe`.
5. Run `License.Client.exe hwid`, issue ETS2 using a **separate** admin installation, and run `License.Client.exe activate <TOKEN>`.
6. Run patched EXE only within the VM; check ActivationForm, embedded payload and ECU UI behavior. **Do not connect any vehicle.**

The patch is **fail-closed only when License.Client.exe exists and returns nonzero**; missing helper returns false. Application launch failures from Process.Start may still raise an exception rather than showing friendly UI. The original `ETS1` verifier remains in the binary and the original ActivationForm may still accept it; this proof of concept does **not** replace or fully migrate the entire licensing flow. For a complete migration, also update the activation form and remove legacy acceptance. The helper currently launches a console process with a hidden window; test display and startup behavior.

Do not publish the original EXE, customer licenses or RSA private.pem in the GitHub repository. Inspect the original EXE for suspicious embedded components before running it.
