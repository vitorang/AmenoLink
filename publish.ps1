$ErrorActionPreference = "Stop"

$rootDir = Get-Location
$distDir = Join-Path $rootDir "dist"
$amenoLinkDistDir = Join-Path $distDir "AmenoLink"
$pythonDistDir = Join-Path $distDir "clients\python"

Write-Host "Iniciando processo de publicacao..."

# 1. Limpa a pasta dist anterior se existir
if (Test-Path $distDir) {
    Write-Host "Limpando pasta dist antiga..."
    Remove-Item -Path $distDir -Recurse -Force
}

New-Item -ItemType Directory -Path $amenoLinkDistDir -Force | Out-Null
New-Item -ItemType Directory -Path $pythonDistDir -Force | Out-Null

# 2. Compila e publica o projeto C# (Desktop/Host)
Write-Host "Publicando aplicacao C# (AmenoLink)..."
$csharpProject = Join-Path $rootDir "AmenoLink\AmenoLink.csproj"

dotnet publish $csharpProject -c Release -o $amenoLinkDistDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "C# (AmenoLink) publicado com sucesso." -ForegroundColor Green
} else {
    Write-Host "ERRO: Falha ao publicar o projeto C#." -ForegroundColor Red
    exit 1
}

# 3. Empacota a biblioteca cliente Python
Write-Host "Empacotando cliente Python (uv build)..."
$pythonClientDir = Join-Path $rootDir "clients\python"

Push-Location $pythonClientDir
try {
    if (Test-Path "dist") {
        Remove-Item -Path "dist" -Recurse -Force
    }

    uv build
    if ($LASTEXITCODE -eq 0 -and (Test-Path "dist")) {
        Copy-Item -Path "dist\*" -Destination $pythonDistDir -Recurse -Force
        Write-Host "Cliente Python empacotado com sucesso." -ForegroundColor Green
    } else {
        Write-Host "ERRO: Falha ao executar 'uv build' no cliente Python." -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}
