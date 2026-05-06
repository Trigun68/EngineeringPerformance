<#
.SYNOPSIS
    Регенерирует SQL-скрипты схемы из EF Core миграций.

.DESCRIPTION
    Источник истины для DDL — EF Core миграции в EngineeringPerformance.Infrastructure/Migrations.
    Этот скрипт вызывает `dotnet ef migrations script --idempotent` и сохраняет
    итоговый SQL в migrations/schema/_generated.sql.

    Пользоваться этим скриптом нужно после каждой новой EF-миграции, чтобы
    SQL-набор оставался синхронным с моделью.

    Существующие файлы migrations/schema/00X_*.sql — legacy,
    оставлены для обратной совместимости утилит развёртывания. Новые изменения
    схемы должны попадать в БД только через EF (Update-Database / DbInitializer)
    или через генерируемый _generated.sql.

.EXAMPLE
    .\Regenerate-SqlSchema.ps1
#>
[CmdletBinding()]
param(
    [string] $OutputFile = 'migrations/schema/_generated.sql',

    [switch] $Idempotent = $true
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $repoRoot

$infra = 'EngineeringPerformance.Infrastructure'
$startup = 'EngineeringPerformance.App'
$output = Join-Path $repoRoot $OutputFile

$args = @('ef', 'migrations', 'script',
    '--project', $infra,
    '--startup-project', $startup,
    '--output', $output)

if ($Idempotent) {
    $args += '--idempotent'
}

Write-Host "Regenerating $OutputFile from EF migrations..." -ForegroundColor Cyan
& dotnet @args

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef migrations script failed with exit code $LASTEXITCODE"
}

Write-Host "OK. Файл $OutputFile синхронизирован с EF-моделью." -ForegroundColor Green
