<#
.SYNOPSIS
    TermForge 快速编译与测试脚本
#>
param (
    [string]$Configuration = "Debug",
    [switch]$RunTests = $true
)

$ErrorActionPreference = "Stop"

Write-Host "==> 正在还原并编译 AgentTerminal.sln [$Configuration]..." -ForegroundColor Cyan
dotnet build AgentTerminal.sln -c $Configuration

if ($RunTests) {
    Write-Host "==> 正在执行单元测试..." -ForegroundColor Cyan
    dotnet test tests/AgentTerminal.Tests/AgentTerminal.Tests.csproj -c $Configuration --no-build --logger "console;verbosity=normal"
}

Write-Host "==> 构建与验证完成!" -ForegroundColor Green
