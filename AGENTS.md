# G-Helper workspace instructions

## Project

- G-Helper is a Windows Forms control utility for ASUS laptops and handhelds.
- The solution is `app/GHelper.sln`; the application project is `app/GHelper.csproj`.
- Target platform is Windows x64 on .NET 10 (`net10.0-windows`, `PlatformTarget=x64`).
- Main source is under `app/`. Important boundaries include `Display/`, `Gpu/`, `Mode/`, `Battery/`, `Fan/`, `Peripherals/`, `USB/`, `Helpers/`, and `UI/`. Designer-generated WinForms files are `*.Designer.cs` and should only change when the corresponding UI layout changes.
- Read `docs/README.md` and the relevant files under `docs/` before changing user-facing behavior or hardware support.

## Build and publish

- Restore/build: `dotnet build app/GHelper.sln`
- CI build: `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug`
- Release single-file publish: `GITHUB_ACTIONS=true dotnet publish app/GHelper.sln --configuration Release --runtime win-x64 -p:PublishSingleFile=true --no-self-contained`
- Release output: `app/bin/x64/Release/net10.0-windows/win-x64/publish/GHelper.exe`
- The project has no test project or lint command. Use `git diff --check` and focused `rg` checks alongside builds.
- Local builds may run the `KillRunningGHelper` MSBuild target unless `GITHUB_ACTIONS=true`; do not unexpectedly terminate a user's running app.
- Publish may create `GHelper.zip` through the local `ZipSingleExe` target. Do not commit `bin/` or `obj/` outputs.

## Editing conventions

- Keep changes narrow and follow existing C# and WinForms patterns. Prefer existing helpers and controls over new abstractions.
- Keep hardware/platform operations defensive and log failures with the existing `Logger.WriteLine(...)` convention.
- Preserve x64/Windows compatibility and avoid assuming non-ASUS hardware paths are available.
- Keep localized strings in the existing `app/Properties/Strings*.resx` resources when user-facing text must change; avoid hardcoding new UI text unless the surrounding code already does so.
- Treat `*.Designer.cs` as generated layout code: edit it only as part of a deliberate UI change and keep the paired form code consistent.

## Repository workflow

- `origin` is the personal fork and `upstream` is `https://github.com/seerge/g-helper.git`.
- Work on the single `main` branch as requested. Sync official changes with `git fetch upstream` and `git merge upstream/main`, then push only to `origin`.
- Do not push custom changes to `upstream` or create a Pull Request unless explicitly requested.
- The repository is GPL-3.0; preserve existing copyright/license notices when distributing changes.
