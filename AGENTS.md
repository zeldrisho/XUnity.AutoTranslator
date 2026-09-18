# Agent Instructions

## Build and Test
- Use NuGet via the SDK-style `.csproj` files; Unity and loader assemblies are referenced from `libs/`.
- Use Windows with a .NET SDK supporting `net6.0` and .NET Framework targeting packs for `net35` and `net471`; post-build targets invoke Windows commands and `tools/xzip.exe`.
- Run commands from the repository root; prefer Debug for development to avoid Release packaging.

| Task | Command |
|------|---------|
| Build managed core | `dotnet build src/XUnity.AutoTranslator.Plugin.Core/XUnity.AutoTranslator.Plugin.Core.csproj -c Debug -f net35` |
| Build IL2CPP core | `dotnet build src/XUnity.AutoTranslator.Plugin.Core/XUnity.AutoTranslator.Plugin.Core.csproj -c Debug -f net6.0` |
| Test string helpers | `dotnet test test/XUnity.Common.Tests/XUnity.Common.Tests.csproj --filter FullyQualifiedName~StringExtensionTests` |
| Test templating | `dotnet test test/XUnity.AutoTranslator.Plugin.Core.Tests/XUnity.AutoTranslator.Plugin.Core.Tests.csproj --filter FullyQualifiedName~TemplatedStringTests` |

- Replace the test-class filter with the affected class or method for narrower checks.
- Tests deriving from `TranslatorTest` call external translation services; do not treat the entire core test project as an offline unit suite.
- `test/XUnity.RuntimeHooker.ConsoleTests/` and `test/XUnity.RuntimeHooker.Benchmark/` are executables, not xUnit suites.

## Key Conventions
- Preserve both `MANAGED` (`net35`) and `IL2CPP` (`net6.0`) paths in shared projects; do not introduce newer framework APIs into managed code.
- Keep translator implementations in `src/Translators/` and loader-specific integration in the corresponding `src/XUnity.AutoTranslator.Plugin.*` projects.
- Change shared version numbers in `Directory.Build.props`; its build target generates `GeneratedInfo.cs`.
- Update resource `.resx` inputs rather than hand-editing `Properties/Resources.Designer.cs`.
- Keep generated `bin/`, `obj/`, and `dist/` outputs out of commits.
- `libs/Koikatsu/` is ignored game-specific input; do not assume `XUnity.AutoTranslator.Koikatsu.sln` builds from a clean checkout.

## References
| Need | File / section |
|------|----------------|
| Installation and configuration | [Installation](README.md#installation), [Configuration](README.md#configuration) |
| Translation API and endpoints | [Integration](README.md#integrating-with-auto-translator), [Translator implementation](README.md#implementing-a-translator) |
| Resource redirection API | [Resource redirector implementation](README.md#implementing-a-resource-redirector) |
| IL2CPP limitations | [IL2CPP support](README.md#il2cpp-support) |
| Plugin release history | [CHANGELOG.md](CHANGELOG.md) |
| ResourceRedirector release history | [CHANGELOG - ResourceRedirector.md](CHANGELOG%20-%20ResourceRedirector.md) |
| Versioning and generated metadata | [Directory.Build.props](Directory.Build.props) |
