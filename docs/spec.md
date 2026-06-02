# .NET 10 Upgrade Spec

Repeatable steps to upgrade a Litle/Vantiv .NET SDK repo from legacy .NET Framework to .NET 10.

---

## 1. Convert to SDK-Style csproj (targeting net10.0)

### Main project (`LitleSdkForNet/LitleSdkForNet.csproj`)

Replace the entire legacy csproj with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Litle.Sdk</RootNamespace>
    <AssemblyName>LitleSdkForNet</AssemblyName>
    <SignAssembly>true</SignAssembly>
    <AssemblyOriginatorKeyFile>dotNetSDKKey.snk</AssemblyOriginatorKeyFile>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SSH.NET" Version="2024.2.0" />
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="9.0.4" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Properties\Settings.settings">
      <Generator>PublicSettingsSingleFileGenerator</Generator>
      <LastGenOutput>Settings.Designer.cs</LastGenOutput>
    </None>
    <Compile Update="Properties\Settings.Designer.cs">
      <AutoGen>True</AutoGen>
      <DesignTimeSharedInput>True</DesignTimeSharedInput>
      <DependentUpon>Settings.settings</DependentUpon>
    </Compile>
  </ItemGroup>

</Project>
```

Key changes:
- `SSH.NET` NuGet replaces the vendored `lib/Renci.SshNet.dll`
- `System.Configuration.ConfigurationManager` added for `ApplicationSettingsBase` support
- `GenerateAssemblyInfo=false` keeps existing `Properties/AssemblyInfo.cs`

### Test project (`LitleSdkForNetTest/LitleSdkForNetTest.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Litle.Sdk.Test</RootNamespace>
    <AssemblyName>LitleSdkForDotNetTest</AssemblyName>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Mozu.Core.JunitTestLogger" Version="2.2616.2" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\LitleSdkForNet\LitleSdkForNet.csproj" />
  </ItemGroup>

</Project>
```

Key changes:
- NUnit 2 → NUnit 3 (`3.14.0`) + `NUnit3TestAdapter` (`4.5.0`)
- `Microsoft.NET.Test.Sdk` (`17.9.0`) required for test discovery
- Moq upgraded from vendored 4.2 DLL → NuGet `4.20.72`
- `Mozu.Core.JunitTestLogger` for Jenkins JUnit XML reporting
- `coverlet.collector` for code coverage collection

---

## 2. Simplify the Solution File

- Change project type GUID from legacy `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}` to SDK-style `{9A19103F-16F7-4668-BE54-9A1E7A4F7556}` for both projects
- Remove all non-`AnyCPU` solution platforms (x86, x64, Mixed Platforms)
- Keep only `Debug|Any CPU` and `Release|Any CPU`

---

## 3. Remove Obsolete Artifacts

Delete the following files/directories:
- `LitleSdkForNet/LitleSdkForNet.nunit` (NUnit 2 runner config)
- `LitleSdkForNet/LitleSdkForNet/LitleSdkForNet.csproj.user`
- `LitleSdkForNet/LitleSdkForNet/Service References/` (contained vendored Moq DLL/PDB/XML)
- `LitleSdkForNet/LitleSdkForNet/lib/Renci.SshNet.dll`
- `LitleSdkForNet/LitleSdkForNetTest/LitleSdkForNetTest.csproj.user`
- `LitleSdkForNet/LitleSdkForNetTest/packages.config`
- `LitleSdkForNet/LitleSdkForNetTest/Program.cs` (unused entry point)
- `LitleSdkForNet/packages/` directory (NuGet packages folder)

---

## 4. Fix Source Code for .NET 10 API Compatibility

### `Communications.cs`

- **Remove** `ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;` (two occurrences) — .NET 10 handles TLS automatically and `ServicePointManager` is obsolete.
- **Add** `#pragma warning disable/restore SYSLIB0014` around `WebRequest.Create(uri)` calls (two occurrences) to suppress the obsolete WebRequest warning while keeping the existing HTTP implementation.

```csharp
#pragma warning disable SYSLIB0014 // WebRequest is obsolete
var req = (HttpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
```

---

## 5. Migrate Test Framework: NUnit 2 → NUnit 3

### Attribute renames (all test files)

| NUnit 2 | NUnit 3 |
|---------|---------|
| `[TestFixtureSetUp]` | `[OneTimeSetUp]` |
| `[TestFixtureTearDown]` | `[OneTimeTearDown]` |

This change applies to **all** files in `Functional/`, `Unit/`, and `Certification/` directories (~65 files).

### Platform-neutral temp paths (Unit tests)

In `TestBatch.cs`, `TestBatchRequest.cs`, `TestRFRRequest.cs`:

- Replace hardcoded Windows paths like `"C:\\Somewhere\\Over\\"` with `Path.Combine(Path.GetTempPath(), "Somewhere", "Over", mockFileName)`
- Add `using System.IO;`
- Update assertions to use `Path.DirectorySeparatorChar` for platform neutrality

---

## 6. Add CI/CD Pipeline Files

### `Dockerfile`

```dockerfile
FROM 542216209467.dkr.ecr.us-east-1.amazonaws.com/kibo/base-images:dotnet-10-build-1 AS build

WORKDIR /src/LitleSdkForNet
COPY ["LitleSdkForNet/LitleSdkForNet.sln", "./"]
COPY ["LitleSdkForNet/LitleSdkForNet/LitleSdkForNet.csproj", "LitleSdkForNet/"]
COPY ["LitleSdkForNet/LitleSdkForNetTest/LitleSdkForNetTest.csproj", "LitleSdkForNetTest/"]

RUN dotnet restore  --source https://api.nuget.org/v3/index.json --source https://nexus.kibo-dev-ext.com/repository/nuget-localbuild/  LitleSdkForNet.sln
WORKDIR /src
COPY . .
WORKDIR /src/LitleSdkForNet
RUN bash ./sonarscanner/sonarnet.sh start || true
ARG BUILD_VER=0.0.0-alphagit
ENV BUILD_VER=$BUILD_VER
RUN dotnet build /p:Version=${BUILD_VER}  ./LitleSdkForNet.sln  -c Release --no-restore &&\
	(dotnet test ./LitleSdkForNetTest/LitleSdkForNetTest.csproj --framework net10.0 --results-directory /buildoutput/testoutput/LitleSdkForNetTest -l kibo-junit --no-build -c Release --no-restore --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~Unit" || true) && \
	dotnet pack -c Release --no-build --no-restore --include-symbols /p:Version=${BUILD_VER} -o /buildoutput/nugs ./LitleSdkForNet.sln
RUN bash ./sonarscanner/sonarnet.sh end || true
```

Key points:
- Uses Kibo `dotnet-10-build-1` base image
- Separate COPY for `.csproj` files enables Docker layer caching for restore
- `--no-restore` on build/test/pack reuses the cached restore
- `|| true` wraps test step so build continues even if tests fail
- Collects XPlat Code Coverage and JUnit test results

### `Jenkinsfile`

```groovy
@Library('kibo-pipeline-shared-lib')_

ngProjectPipeline (
    KIBO_MAJOR_VERSION: 2,
    SUPPORTS_NUGET: true,
    FAIL_ON_TEST_FAILURE: true,
    DOCKERFILE : './Dockerfile'
)
```

### `.dockerignore`

```
.git
.gitignore
**/bin
**/obj
**/*.user
*.md
LICENSE
CHANGELOG
CONTRIBUTORS
kit.bat
```

### `.gitignore` addition

Add to existing `.gitignore`:
```
**/TestResults/
```

---

## 7. Summary of Dependency Changes

| Dependency | Old | New |
|-----------|-----|-----|
| Target Framework | .NET Framework 4.x | `net10.0` |
| Renci.SshNet | Vendored DLL in `lib/` | `SSH.NET` NuGet 2024.2.0 |
| System.Configuration | Framework built-in | `System.Configuration.ConfigurationManager` 9.0.4 |
| NUnit | 2.x (vendored via packages/) | 3.14.0 (NuGet) |
| NUnit Adapter | NUnitTestAdapter.WithFramework 2.0.0 | NUnit3TestAdapter 4.5.0 |
| Test SDK | N/A | Microsoft.NET.Test.Sdk 17.9.0 |
| Moq | 4.2 (vendored DLL) | 4.20.72 (NuGet) |
| Test Logger | N/A | Mozu.Core.JunitTestLogger 2.2616.2 |
| Coverage | N/A | coverlet.collector 6.0.4 |

---

## 8. Checklist for Repeating on Another Branch

1. [ ] Replace both `.csproj` files with SDK-style versions (Section 1)
2. [ ] Update `.sln` project type GUIDs and remove extra platforms (Section 2)
3. [ ] Delete obsolete files and directories (Section 3)
4. [ ] Fix `Communications.cs` — remove `ServicePointManager` lines, add `SYSLIB0014` pragmas (Section 4)
5. [ ] Find/replace `[TestFixtureSetUp]` → `[OneTimeSetUp]` in all test files (Section 5)
6. [ ] Fix hardcoded Windows paths in `TestBatch.cs`, `TestBatchRequest.cs`, `TestRFRRequest.cs` (Section 5)
7. [ ] Add `Dockerfile`, `Jenkinsfile`, `.dockerignore` (Section 6)
8. [ ] Add `**/TestResults/` to `.gitignore` (Section 6)
9. [ ] Run `dotnet restore` and `dotnet build` to verify
10. [ ] Run `dotnet test --filter "FullyQualifiedName~Unit"` to verify tests pass
