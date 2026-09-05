<#
.SYNOPSIS
    TermForge UI 自动化端到端测试执行脚本 (FlaUI + xUnit)
#>
param (
    [string]$Configuration = "Debug",
    [string]$Filter = ""
)

$ErrorActionPreference = "Stop"

Write-Host "==> 正在编译 AgentTerminal.sln [$Configuration]..." -ForegroundColor Cyan
dotnet build AgentTerminal.sln -c $Configuration

Write-Host "==> 正在执行 UI 自动化端到端测试..." -ForegroundColor Cyan
$testArgs = @(
    "test",
    "tests/AgentTerminal.UITests/AgentTerminal.UITests.csproj",
    "-c", $Configuration,
    "--no-build",
    "--logger", "console;verbosity=normal"
)

if (![string]::IsNullOrWhiteSpace($Filter)) {
    $testArgs += @("--filter", $Filter)
}

& dotnet @testArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "UI 自动化测试执行失败 (Exit Code: $LASTEXITCODE)"
}

Write-Host "==> 所有 UI 自动化测试执行成功并通过质量门禁!" -ForegroundColor Green
