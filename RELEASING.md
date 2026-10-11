# Release Guide

Этот чеклист держит выпуск MaxBotNet воспроизводимым: версия, changelog, NuGet package и GitHub Release должны совпадать.

## 1. Pre-flight

- Убедитесь, что все user-facing изменения описаны в `CHANGELOG.md` в секции новой версии.
- Проверьте `src/Max.Bot/Max.Bot.csproj`: `<Version>`, `<PackageId>`, package metadata и target frameworks.
- Для полного локального multi-target прогона должны быть установлены SDK/runtime `.NET 10`, `.NET 9` и `.NET 8`.
- Проверьте секреты: `NUGET_API_KEY` в GitHub Actions и локальные токены (`MAX_BOT_TOKEN`, webhook secrets) не должны попадать в исходники.

## 2. Local verification

```powershell
dotnet --list-sdks
dotnet --list-runtimes
dotnet restore Max.Bot.sln
dotnet format Max.Bot.sln --verify-no-changes
dotnet format analyzers Max.Bot.sln --verify-no-changes --no-restore
dotnet build Max.Bot.sln --configuration Release --no-restore -warnaserror
dotnet test Max.Bot.sln --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=\"cobertura%2copencover\" /p:CoverletOutput=TestResults/Coverage/ /p:Threshold=60 /p:ThresholdType=line /p:ThresholdStat=total
dotnet pack src/Max.Bot/Max.Bot.csproj --configuration Release --no-build /p:ContinuousIntegrationBuild=true
```

Если локально отсутствует один из runtime, допустимо перед push выполнить частичный smoke-прогон по доступному target framework, например:

```powershell
dotnet test tests/Max.Bot.Tests/Max.Bot.Tests.csproj -f net10.0
```

Полный multi-target gate всё равно должен пройти в GitHub Actions, где workflow устанавливает `.NET 10`, `.NET 9` и `.NET 8`.

Inspect the generated `.nupkg`/`.snupkg` locally (for example, by expanding the package) to ensure README, CHANGELOG, symbols and XML docs are embedded.

## 3. Versioning and tagging

1. Обновите `CHANGELOG.md` и `src/Max.Bot/Max.Bot.csproj` на один и тот же SemVer, например `0.6.1-alpha`.
2. Закоммитьте изменения.
3. После зелёного CI создайте и запушьте тег:

```powershell
git tag v0.6.1-alpha
git push origin v0.6.1-alpha
```

Tag format must stay `vX.Y.Z` or `vX.Y.Z-alpha`, because `.github/workflows/release.yml` derives `PackageVersion` from the tag name.

Если пушить теги нельзя (например, из окружения без прав на теги), релиз можно запустить вручную: **Actions → Release → Run workflow** на ветке `main` с параметром `version` (например, `0.7.0-alpha`). Workflow проверит, что тега ещё нет, выполнит те же проверки и публикацию и сам создаст тег `v<version>` на выбранном коммите вместе с GitHub Release.

## 4. GitHub Actions release

- Tag push (or a manual `workflow_dispatch` run with `version`) triggers `.github/workflows/release.yml`.
- Workflow steps: restore → format → analyzers → build → tests → validate version/changelog → `dotnet pack` → upload artifacts → `dotnet nuget push` → GitHub Release.
- Release notes are extracted from the matching `CHANGELOG.md` section.
- NuGet publishing requires the `NUGET_API_KEY` secret.
- GitHub Release creation requires workflow `contents: write` permission.
- Monitor the run in GitHub Actions. The run must be green before announcing the release.

## 5. Post-publish validation

1. Confirm package visibility on NuGet.org: `MaxMessenger.Bot` with the expected version.
2. Confirm GitHub Release:
   - tag matches the package version;
   - prerelease is enabled for `-alpha` versions;
   - release notes match `CHANGELOG.md`;
   - `.nupkg` and `.snupkg` artifacts are attached.
3. Smoke test the package in a clean project:

```powershell
dotnet new console -n MaxBotNet.ReleaseSmoke
cd MaxBotNet.ReleaseSmoke
dotnet add package MaxMessenger.Bot --version 0.6.1-alpha --prerelease
dotnet build
```

## 6. External-action boundary

`git push`, `git tag`, `git push origin v...`, NuGet publish and GitHub Release creation affect shared external state. Run them only after explicit release confirmation.
