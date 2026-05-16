param(
    [string]$ProjectPath = "..\backend\src\Subly.Api\Subly.Api.csproj"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$fullProjectPath = Join-Path $scriptDir $ProjectPath

dotnet run --project $fullProjectPath -- --seed
