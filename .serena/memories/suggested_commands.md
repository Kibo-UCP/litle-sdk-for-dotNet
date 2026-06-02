# Suggested Commands

## Build

```bash
# Legacy .NET Framework build (requires Mono on macOS or Windows MSBuild)
msbuild LitleSdkForNet/LitleSdkForNet.sln /p:Configuration=Release

# After .NET 10 migration
dotnet build LitleSdkForNet/LitleSdkForNet.sln
```

## Test

```bash
# After .NET 10 migration
dotnet test LitleSdkForNet/LitleSdkForNet.sln

# NUnit console runner (legacy)
nunit-console LitleSdkForNet/LitleSdkForNetTest/bin/Debug/LitleSdkForNetTest.dll
```

## Common Utilities

```bash
# Find XML-related source files
find LitleSdkForNet -name "*.cs" | xargs grep -l "XmlElement\|XmlAttribute"

# Check XML serialization output (useful for wire-compat validation)
grep -rn "Serialize\|Deserialize" LitleSdkForNet/LitleSdkForNet/
```
