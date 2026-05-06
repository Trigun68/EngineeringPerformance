<#
.SYNOPSIS
    Запуск SQL-скриптов развёртывания БД из каталога migrations репозитория.

.DESCRIPTION
    Выполняет sqlcmd с выбранным entry-point скриптом, предварительно переходя в каталог migrations.
    Это необходимо: директивы :r внутри скриптов разрешают пути относительно файла-владельца,
    но рабочий каталог процесса должен совпадать с каталогом migrations, чтобы -i и вложенные :r
    находили schema\ и data\.

    Доступные скрипты:
    - 000_MasterMigration.sql          — полный цикл: структура + данные
    - FullMigration_01_Structure.sql   — только DDL (001, 006, 007)
    - FullMigration_02_DataImport.sql — только данные (002, 003-заглушка, 004, 005)

    Подробнее: install/README.md и docs/database/migration.md

.EXAMPLE
    .\Migrate.ps1 -Server localhost -Database EngineeringPerformance -Script 000_MasterMigration.sql

.EXAMPLE
    .\Migrate.ps1 -Server . -Database EngineeringPerformance -Script FullMigration_01_Structure.sql
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Server,

    [Parameter(Mandatory = $true)]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [ValidateSet(
        '000_MasterMigration.sql',
        'FullMigration_01_Structure.sql',
        'FullMigration_02_DataImport.sql'
    )]
    [string] $Script,

    [string] $SqlCmdUser,
    [string] $SqlCmdPassword
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$migrationsDir = Join-Path (Split-Path $here -Parent) 'migrations'
$sqlFile = Join-Path $migrationsDir $Script

if (-not (Test-Path -LiteralPath $sqlFile)) {
    throw "Файл не найден: $sqlFile"
}

$sqlcmdArgs = @('-S', $Server, '-d', $Database, '-i', $Script)
if ($SqlCmdUser) {
    $sqlcmdArgs += @('-U', $SqlCmdUser, '-P', $SqlCmdPassword)
} else {
    $sqlcmdArgs += '-E'
}

Push-Location $migrationsDir
try {
    & sqlcmd @sqlcmdArgs
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
