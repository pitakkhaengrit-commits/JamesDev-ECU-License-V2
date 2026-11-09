# JamesDev ECU License V2

Independent offline RSA-2048/SHA-256 licensing example for software you own or are authorized to modify. Does not modify an existing ECU EXE.

## Build Windows executables
GitHub Actions > Build Windows EXE > Run workflow. Download the ECU-License-V2-Windows artifact after the run succeeds. Or install .NET 8 SDK and run:

```powershell
dotnet publish License.Admin/License.Admin.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/admin
dotnet publish License.Client/License.Client.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/client
```

## Issue a license
1. On the trusted administrator computer, run `License.Admin.exe init` once. It creates `private.pem` and `public.pem` in the working directory.
2. On the client computer, run `License.Client.exe hwid`.
3. On the administrator computer, run `License.Admin.exe issue <HWID> <YYYY-MM-DD>` and copy the ETS2 token.
4. Put `public.pem` beside `License.Client.exe` on the client computer.
5. Run `License.Client.exe activate <TOKEN>` and then `License.Client.exe check`.

**Never commit or distribute private.pem.** Keep it only on the administrator computer. The public.pem is intended for distribution. This is a console-based demonstration, not a WinForms UI or a complete integration with the existing ECU application. The HWID is not a hardware security boundary.
