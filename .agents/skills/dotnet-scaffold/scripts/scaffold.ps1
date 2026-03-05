<#
.SYNOPSIS
  Scaffold a Clean Architecture .NET 10 solution modelled on Hmz.Kolafi.
.PARAMETER Prefix       Company prefix  (e.g. Hmz)
.PARAMETER Project      Project name    (e.g. Kolafi)
.PARAMETER Root         Root output dir (defaults to current directory)
#>
param(
  [Parameter(Mandatory)][string]$Prefix,
  [Parameter(Mandatory)][string]$Project,
  [string]$Root = "."
)

$ns = "$Prefix.$Project"           # e.g. Hmz.Kolafi
$dir = Join-Path $Root $ns
Write-Host "Scaffolding $ns in $dir" -ForegroundColor Cyan

# ── helpers ──────────────────────────────────────────────────────────────────
function New-Dir { param($p) New-Item -ItemType Directory -Force -Path $p | Out-Null }
function New-File { param($p, $c) New-Dir (Split-Path $p); Set-Content -Path $p -Value $c -Encoding UTF8 }

# ── directories ───────────────────────────────────────────────────────────────
New-Dir $dir
foreach ($d in @(
    "src\$ns.Core\ContributorAggregate\Events",
    "src\$ns.Core\ContributorAggregate\Handlers",
    "src\$ns.Core\ContributorAggregate\Specifications",
    "src\$ns.Core\Interfaces",
    "src\$ns.Core\Services",
    "src\$ns.UseCases\Contributors\Create",
    "src\$ns.UseCases\Contributors\Get",
    "src\$ns.UseCases\Contributors\List",
    "src\$ns.UseCases\Contributors\Update",
    "src\$ns.UseCases\Contributors\Delete",
    "src\$ns.Infrastructure\Data\Config",
    "src\$ns.Infrastructure\Data\Migrations",
    "src\$ns.Infrastructure\Data\Queries",
    "src\$ns.Infrastructure\Email",
    "src\$ns.Web\Configurations",
    "src\$ns.Web\Contributors",
    "src\$ns.Web\Extensions",
    "src\$ns.Web\Properties",
    "src\$ns.Web\wwwroot",
    "src\$ns.AspireHost",
    "src\$ns.ServiceDefaults",
    "tests\$ns.UnitTests\Core\Services",
    "tests\$ns.IntegrationTests\Data",
    "tests\$ns.FunctionalTests\Contributors",
    "tests\$ns.AspireTests"
  )) { New-Dir "$dir\$d" }

# ── solution file (.slnx) ────────────────────────────────────────────────────
New-File "$dir\$ns.slnx" @"
<Solution>
  <Configurations>
    <Platform Name="Any CPU" />
    <Platform Name="x64" />
    <Platform Name="x86" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path=".editorconfig" />
    <File Path="Directory.Build.props" />
    <File Path="Directory.Packages.props" />
  </Folder>
  <Folder Name="/src/">
    <Project Path="src/$ns.Core/$ns.Core.csproj" />
    <Project Path="src/$ns.Infrastructure/$ns.Infrastructure.csproj" />
    <Project Path="src/$ns.UseCases/$ns.UseCases.csproj" />
    <Project Path="src/$ns.Web/$ns.Web.csproj" />
  </Folder>
  <Folder Name="/src/_aspire/">
    <Project Path="src/$ns.AspireHost/$ns.AspireHost.csproj" />
    <Project Path="src/$ns.ServiceDefaults/$ns.ServiceDefaults.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/$ns.AspireTests/$ns.AspireTests.csproj" />
    <Project Path="tests/$ns.FunctionalTests/$ns.FunctionalTests.csproj">
      <BuildDependency Project="src/$ns.Web/$ns.Web.csproj" />
    </Project>
    <Project Path="tests/$ns.IntegrationTests/$ns.IntegrationTests.csproj">
      <BuildDependency Project="src/$ns.Infrastructure/$ns.Infrastructure.csproj" />
    </Project>
    <Project Path="tests/$ns.UnitTests/$ns.UnitTests.csproj" />
  </Folder>
</Solution>
"@

# ── Directory.Build.props ─────────────────────────────────────────────────────
New-File "$dir\Directory.Build.props" @'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <PropertyGroup>
    <NoWarn>1591</NoWarn>
  </PropertyGroup>
</Project>
'@

# ── Directory.Packages.props ──────────────────────────────────────────────────
New-File "$dir\Directory.Packages.props" @'
<Project>
  <ItemGroup>
    <PackageVersion Include="Ardalis.GuardClauses"                              Version="5.0.0" />
    <PackageVersion Include="Ardalis.HttpClientTestExtensions"                  Version="4.2.0" />
    <PackageVersion Include="Ardalis.ListStartupServices"                       Version="1.1.4" />
    <PackageVersion Include="Ardalis.Result"                                    Version="10.1.0" />
    <PackageVersion Include="Ardalis.Result.AspNetCore"                         Version="10.1.0" />
    <PackageVersion Include="Ardalis.SharedKernel"                              Version="4.1.0" />
    <PackageVersion Include="Ardalis.SmartEnum"                                 Version="8.2.0" />
    <PackageVersion Include="Ardalis.Specification"                             Version="9.3.1" />
    <PackageVersion Include="Ardalis.Specification.EntityFrameworkCore"         Version="9.3.1" />
    <PackageVersion Include="Azure.Identity"                                    Version="1.17.0" />
    <PackageVersion Include="coverlet.collector"                                Version="6.0.4" />
    <PackageVersion Include="FastEndpoints"                                     Version="7.1.1" />
    <PackageVersion Include="FastEndpoints.ApiExplorer"                         Version="2.2.0" />
    <PackageVersion Include="FastEndpoints.Swagger"                             Version="7.1.1" />
    <PackageVersion Include="MailKit"                                           Version="4.14.1" />
    <PackageVersion Include="Mediator.Abstractions"                             Version="3.0.1" />
    <PackageVersion Include="Mediator.SourceGenerator"                          Version="3.0.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing"                  Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory"            Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational"          Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design"              Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite"              Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer"           Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration"                Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging"                      Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions"         Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk"                            Version="18.0.1" />
    <PackageVersion Include="NimblePros.Metronome"                              Version="0.4.1" />
    <PackageVersion Include="NSubstitute"                                       Version="5.3.0" />
    <PackageVersion Include="ReportGenerator"                                   Version="5.5.0" />
    <PackageVersion Include="Scalar.AspNetCore"                                 Version="2.10.3" />
    <PackageVersion Include="Serilog.AspNetCore"                                Version="9.0.0" />
    <PackageVersion Include="Serilog.Sinks.OpenTelemetry"                       Version="4.2.0" />
    <PackageVersion Include="Shouldly"                                          Version="4.3.0" />
    <PackageVersion Include="SQLite"                                            Version="3.13.0" />
    <PackageVersion Include="Swashbuckle.AspNetCore"                            Version="6.5.0" />
    <PackageVersion Include="Swashbuckle.AspNetCore.Annotations"                Version="6.5.0" />
    <PackageVersion Include="Testcontainers"                                    Version="4.3.0" />
    <PackageVersion Include="Testcontainers.MsSql"                              Version="4.3.0" />
    <PackageVersion Include="xunit"                                             Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio"                         Version="3.1.5" />
    <PackageVersion Include="Aspire.Hosting.AppHost"                            Version="13.0.0" />
    <PackageVersion Include="Aspire.Hosting.SqlServer"                          Version="13.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience"              Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.ServiceDiscovery"             Version="10.0.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol"      Version="1.14.0" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting"                  Version="1.14.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore"          Version="1.13.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http"                Version="1.13.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Runtime"             Version="1.13.0" />
    <PackageVersion Include="Aspire.Hosting.Testing"                            Version="9.5.1" />
    <PackageVersion Include="Vogen"                                             Version="8.0.2" />
  </ItemGroup>
</Project>
'@

# ── global.json ───────────────────────────────────────────────────────────────
New-File "$dir\global.json" @'
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMajor",
    "allowPrerelease": true
  }
}
'@

# ── nuget.config ──────────────────────────────────────────────────────────────
New-File "$dir\nuget.config" @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageRestore>
        <add key="enabled" value="True" />
        <add key="automatic" value="True" />
    </packageRestore>
    <packageSources>
        <clear />
        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
    <packageSourceMapping>
        <packageSource key="nuget.org">
            <package pattern="*" />
        </packageSource>
    </packageSourceMapping>
</configuration>
'@

# ── .runsettings ──────────────────────────────────────────────────────────────
New-File "$dir\.runsettings" @'
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>0</MaxCpuCount>
    <DisableParallelization>false</DisableParallelization>
  </RunConfiguration>
  <xUnit>
    <ParallelizeAssembly>true</ParallelizeAssembly>
    <ParallelizeTestCollections>true</ParallelizeTestCollections>
    <MaxParallelThreads>0</MaxParallelThreads>
  </xUnit>
</RunSettings>
'@

# ── .gitignore (dotnet standard) ──────────────────────────────────────────────
& dotnet new gitignore --output "$dir" --force | Out-Null

# ── Core project ──────────────────────────────────────────────────────────────
$coreCsproj = "src\$ns.Core\$ns.Core.csproj"
New-File "$dir\$coreCsproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Content Remove="`$HOME\.nuget\packages\vogen\**\Vogen.targets" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Ardalis.GuardClauses" />
    <PackageReference Include="Ardalis.Result" />
    <PackageReference Include="Ardalis.SharedKernel" />
    <PackageReference Include="Ardalis.SmartEnum" />
    <PackageReference Include="Ardalis.Specification" />
    <PackageReference Include="Mediator.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Vogen" />
  </ItemGroup>
</Project>
"@

New-File "$dir\src\$ns.Core\GlobalUsings.cs" @"
global using Ardalis.GuardClauses;
global using Ardalis.Result;
global using Ardalis.SharedKernel;
global using Ardalis.SmartEnum;
global using Ardalis.Specification;
global using Mediator;
global using Microsoft.Extensions.Logging;
"@

# ── UseCases project ──────────────────────────────────────────────────────────
New-File "$dir\src\$ns.UseCases\$ns.UseCases.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ardalis.Result" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\$ns.Core\$ns.Core.csproj" />
  </ItemGroup>
</Project>
"@

New-File "$dir\src\$ns.UseCases\GlobalUsings.cs" @"
global using Ardalis.Result;
global using Ardalis.SharedKernel;
global using Mediator;
"@

# ── Infrastructure project ────────────────────────────────────────────────────
New-File "$dir\src\$ns.Infrastructure\$ns.Infrastructure.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ardalis.SharedKernel" />
    <PackageReference Include="Ardalis.Specification.EntityFrameworkCore" />
    <PackageReference Include="Azure.Identity" />
    <PackageReference Include="MailKit" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" ExcludeAssets="contentFiles" PrivateAssets="all" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="Microsoft.Extensions.Configuration" />
    <PackageReference Include="Microsoft.Extensions.Logging" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="NimblePros.Metronome" />
    <PackageReference Include="SQLite" />
    <PackageReference Include="Vogen" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\$ns.Core\$ns.Core.csproj" />
    <ProjectReference Include="..\$ns.UseCases\$ns.UseCases.csproj" />
  </ItemGroup>
</Project>
"@

New-File "$dir\src\$ns.Infrastructure\GlobalUsings.cs" @"
global using System.Net.Mail;
global using System.Reflection;
global using Ardalis.GuardClauses;
global using Ardalis.SharedKernel;
global using Ardalis.Specification.EntityFrameworkCore;
global using MailKit.Net.Smtp;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using MimeKit;
"@

# ── Web project ───────────────────────────────────────────────────────────────
New-File "$dir\src\$ns.Web\$ns.Web.csproj" @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
    <OutputType>Exe</OutputType>
    <WebProjectMode>true</WebProjectMode>
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Ardalis.ListStartupServices" />
    <PackageReference Include="Ardalis.Result" />
    <PackageReference Include="Ardalis.Result.AspNetCore" />
    <PackageReference Include="FastEndpoints" />
    <PackageReference Include="FastEndpoints.Swagger" />
    <PackageReference Include="Mediator.SourceGenerator">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Scalar.AspNetCore" />
    <PackageReference Include="Serilog.AspNetCore" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\$ns.Infrastructure\$ns.Infrastructure.csproj" />
    <ProjectReference Include="..\$ns.UseCases\$ns.UseCases.csproj" />
    <ProjectReference Include="..\$ns.ServiceDefaults\$ns.ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
"@

New-File "$dir\src\$ns.Web\GlobalUsings.cs" @"
global using Ardalis.Result;
global using FastEndpoints;
global using FastEndpoints.Swagger;
global using Mediator;
global using Microsoft.EntityFrameworkCore;
global using Serilog;
global using Serilog.Extensions.Logging;
"@

New-File "$dir\src\$ns.Web\Program.cs" @"
using $ns.Web.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .AddLoggerConfigs();

using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
var startupLogger = loggerFactory.CreateLogger<Program>();

startupLogger.LogInformation(""Starting web host"");

builder.Services.AddOptionConfigs(builder.Configuration, startupLogger, builder);
builder.Services.AddServiceConfigs(startupLogger, builder);

builder.Services.AddFastEndpoints()
                .SwaggerDocument(o => { o.ShortSchemaNames = true; });

var app = builder.Build();

await app.UseAppMiddlewareAndSeedDatabase();

app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
"@

# ── AspireHost project ────────────────────────────────────────────────────────
New-File "$dir\src\$ns.AspireHost\$ns.AspireHost.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="13.0.0" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" />
    <PackageReference Include="Aspire.Hosting.SqlServer" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\$ns.Web\$ns.Web.csproj" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\$ns.ServiceDefaults\$ns.ServiceDefaults.csproj" IsAspireProjectResource="false" />
  </ItemGroup>
</Project>
"@

# ── ServiceDefaults project ───────────────────────────────────────────────────
New-File "$dir\src\$ns.ServiceDefaults\$ns.ServiceDefaults.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsAspireSharedProject>true</IsAspireSharedProject>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
  </ItemGroup>
</Project>
"@

# ── Test projects ─────────────────────────────────────────────────────────────
New-File "$dir\tests\$ns.UnitTests\$ns.UnitTests.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="ReportGenerator" />
    <PackageReference Include="xunit" />
    <PackageReference Include="Mediator.Abstractions" />
    <PackageReference Include="Mediator.SourceGenerator" />
  </ItemGroup>
  <ItemGroup>
    <None Update="xunit.runner.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\$ns.Core\$ns.Core.csproj" />
    <ProjectReference Include="..\..\src\$ns.UseCases\$ns.UseCases.csproj" />
  </ItemGroup>
</Project>
"@

New-File "$dir\tests\$ns.IntegrationTests\$ns.IntegrationTests.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="xunit" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
  </ItemGroup>
  <ItemGroup>
    <None Update="xunit.runner.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\$ns.Infrastructure\$ns.Infrastructure.csproj" />
  </ItemGroup>
</Project>
"@

New-File "$dir\tests\$ns.FunctionalTests\$ns.FunctionalTests.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Testcontainers" />
    <PackageReference Include="Testcontainers.MsSql" />
    <PackageReference Include="Ardalis.HttpClientTestExtensions" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\$ns.Infrastructure\$ns.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\$ns.UseCases\$ns.UseCases.csproj" />
    <ProjectReference Include="..\..\src\$ns.Web\$ns.Web.csproj" />
  </ItemGroup>
</Project>
"@

New-File "$dir\tests\$ns.AspireTests\$ns.AspireTests.csproj" @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing"/>
    <PackageReference Include="coverlet.collector"/>
    <PackageReference Include="Microsoft.NET.Test.Sdk"/>
    <PackageReference Include="xunit"/>
    <PackageReference Include="xunit.runner.visualstudio"/>
  </ItemGroup>
  <ItemGroup>
    <Using Include="System.Net" />
    <Using Include="Microsoft.Extensions.DependencyInjection" />
    <Using Include="Aspire.Hosting.ApplicationModel" />
    <Using Include="Aspire.Hosting.Testing" />
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\$ns.AspireHost\$ns.AspireHost.csproj" />
  </ItemGroup>
</Project>
"@

# ── xunit.runner.json for test parallelism ────────────────────────────────────
$xunitJson = @'
{
  "shadowCopy": false,
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 0
}
'@
New-File "$dir\tests\$ns.UnitTests\xunit.runner.json"        $xunitJson
New-File "$dir\tests\$ns.IntegrationTests\xunit.runner.json" $xunitJson
New-File "$dir\tests\$ns.FunctionalTests\xunit.runner.json"  $xunitJson

# ── GlobalUsings.cs for test projects + AspireHost ─────────────────────────────
New-File "$dir\tests\$ns.UnitTests\GlobalUsings.cs" @"
global using System.Runtime.CompilerServices;
global using Ardalis.SharedKernel;
global using Shouldly;
global using Mediator;
global using Microsoft.Extensions.Logging;
global using NSubstitute;
global using Xunit;
"@

New-File "$dir\tests\$ns.IntegrationTests\GlobalUsings.cs" @"
global using Ardalis.SharedKernel;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using NSubstitute;
global using Shouldly;
global using Xunit;
"@

New-File "$dir\tests\$ns.FunctionalTests\GlobalUsings.cs" @"
global using Ardalis.HttpClientTestExtensions;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Shouldly;
global using Xunit;
"@

New-File "$dir\src\$ns.AspireHost\GlobalUsings.cs" @"
global using Aspire.Hosting;
"@

Write-Host ""
Write-Host "✅ Solution scaffolded at: $dir" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  cd `"$dir`""
Write-Host "  dotnet build $ns.slnx"
Write-Host "  dotnet ef migrations add InitialCreate --project src\$ns.Infrastructure --startup-project src\$ns.Web --output-dir Data\Migrations"
Write-Host "  dotnet run --project src\$ns.Web"
