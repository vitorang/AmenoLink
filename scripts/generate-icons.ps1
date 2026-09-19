$ErrorActionPreference = "Stop"

if (-not (Test-Path "AmenoLink") -or -not (Test-Path "AmenoLink.WebUI")) {
    Write-Host "ERRO: Este script deve ser executado a partir da raiz do repositorio AmenoLink." -ForegroundColor Red
    Write-Host "Exemplo de uso: .\scripts\generate-icons.ps1" -ForegroundColor Yellow
    exit 1
}

$sourcePng = "assets\icon.png"
if (-not (Test-Path $sourcePng)) {
    Write-Host "ERRO: Arquivo '$sourcePng' nao encontrado." -ForegroundColor Red
    exit 1
}

# Verifica se ffmpeg está disponível
if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Host "ERRO: ffmpeg nao foi encontrado no PATH do sistema." -ForegroundColor Red
    exit 1
}

Write-Host "Gerando icones a partir de '$sourcePng' usando ffmpeg..." -ForegroundColor Cyan

# 1. Gera icon.ico (256x256) para o executavel C#
Write-Host "  -> Gerando AmenoLink\icon.ico (256x256)..."
ffmpeg -y -v error -i $sourcePng -vf "scale=256:256" AmenoLink\icon.ico
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO ao gerar AmenoLink\icon.ico." -ForegroundColor Red
    exit 1
}

# 2. Gera favicon.ico (64x64) para a WebUI
Write-Host "  -> Gerando AmenoLink.WebUI\public\favicon.ico (64x64)..."
ffmpeg -y -v error -i $sourcePng -vf "scale=64:64" AmenoLink.WebUI\public\favicon.ico
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO ao gerar favicon.ico." -ForegroundColor Red
    exit 1
}

Write-Host "Icones gerados com sucesso!" -ForegroundColor Green
