param (
    [string]$NewVersion
)

$ErrorActionPreference = "Stop"
$rootDir = Get-Location
$versionFile = Join-Path $rootDir "VERSION"

# Se uma versão foi passada como argumento ex: .\set-version.ps1 0.2.0, atualiza o arquivo VERSION
if ($NewVersion) {
    Set-Content -Path $versionFile -Value $NewVersion -NoNewline
    $version = $NewVersion.Trim()
} else {
    if (-not (Test-Path $versionFile)) {
        Write-Host "ERRO: Arquivo VERSION nao encontrado na raiz e nenhuma versao foi informada." -ForegroundColor Red
        Write-Host "Uso: .\set-version.ps1 <nova_versao>" -ForegroundColor Yellow
        exit 1
    }
    $version = (Get-Content $versionFile).Trim()
}

Write-Host "Aplicando versao '$version' nos arquivos fonte..." -ForegroundColor Cyan

# 1. C# (.csproj)
$csharpProject = Join-Path $rootDir "AmenoLink\AmenoLink.csproj"
if (Test-Path $csharpProject) {
    $csprojContent = Get-Content $csharpProject -Raw
    if ($csprojContent -match "<Version>[^<]+</Version>") {
        $csprojContent = $csprojContent -replace "<Version>[^<]+</Version>", "<Version>$version</Version>"
    } else {
        $csprojContent = $csprojContent -replace "<PropertyGroup>", "<PropertyGroup>`r`n    <Version>$version</Version>"
    }
    Set-Content -Path $csharpProject -Value $csprojContent -NoNewline
    Write-Host "  [C#] AmenoLink.csproj -> $version" -ForegroundColor Green
}

# 2. Python (pyproject.toml)
$pyProjectFile = Join-Path $rootDir "clients\python\pyproject.toml"
if (Test-Path $pyProjectFile) {
    $pyContent = Get-Content $pyProjectFile -Raw
    $pyContent = $pyContent -replace 'version\s*=\s*"[^"]+"', "version = `"$version`""
    Set-Content -Path $pyProjectFile -Value $pyContent -NoNewline
    Write-Host "  [Python] pyproject.toml -> $version" -ForegroundColor Green
}

# 3. Dart (pubspec.yaml)
$pubspecFile = Join-Path $rootDir "clients\dart\amenolink\pubspec.yaml"
if (Test-Path $pubspecFile) {
    $pubContent = Get-Content $pubspecFile -Raw
    $pubContent = $pubContent -replace 'version:\s*[^\r\n]+', "version: $version"
    Set-Content -Path $pubspecFile -Value $pubContent -NoNewline
    Write-Host "  [Dart] pubspec.yaml -> $version" -ForegroundColor Green
}

# 4. WebUI (package.json)
$packageJsonFile = Join-Path $rootDir "AmenoLink.WebUI\package.json"
if (Test-Path $packageJsonFile) {
    $pkgContent = Get-Content $packageJsonFile -Raw
    $pkgContent = $pkgContent -replace '"version":\s*"[^"]+"', "`"version`": `"$version`""
    Set-Content -Path $packageJsonFile -Value $pkgContent -NoNewline
    Write-Host "  [WebUI] package.json -> $version" -ForegroundColor Green
}

# 5. Python Example Requirements (requirements.txt)
$reqFile = Join-Path $rootDir "examples\python\requirements.txt"
if (Test-Path $reqFile) {
    $reqContent = Get-Content $reqFile -Raw
    $reqContent = $reqContent -replace 'amenolink-[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.]+)?-py3', "amenolink-$version-py3"
    Set-Content -Path $reqFile -Value $reqContent -NoNewline
    Write-Host "  [Python Example] requirements.txt -> amenolink-$version" -ForegroundColor Green
}

# 6. README.md
$readmeFile = Join-Path $rootDir "README.md"
if (Test-Path $readmeFile) {
    $readmeContent = Get-Content $readmeFile -Raw
    $readmeContent = $readmeContent -replace 'amenolink-[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.]+)?-py3', "amenolink-$version-py3"
    Set-Content -Path $readmeFile -Value $readmeContent -NoNewline
    Write-Host "  [Docs] README.md -> amenolink-$version" -ForegroundColor Green
}

Write-Host "Versao $version aplicada com sucesso a todos os projetos!" -ForegroundColor Cyan
