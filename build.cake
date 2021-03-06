#r "System.IO.Compression.FileSystem"
#r "System.Xml"

//#tool "nuget:?package=xunit.runner.console"
//#tool "nuget:?package=OpenCover"
#addin "Cake.Incubator"

using System.IO.Compression;
using System.Net;
using System.Xml;

string Target = Argument<string>("Target", "Default");

bool IsCleanEnabled = Argument<bool>("DoClean", true);
bool IsRestoreEnabled = Argument<bool>("DoRestore", true);
bool IsBuildEnabled = Argument<bool>("DoBuild", true);
bool IsTestEnabled = Argument<bool>("DoTest", true);
bool IsPackEnabled = Argument<bool>("DoPack", true);

string Configuration = HasArgument("Configuration") 
    ? Argument<string>("Configuration") 
    : EnvironmentVariable("Configuration") ?? "Release";

var SolutionDir = Context.Environment.WorkingDirectory.FullPath;
var SolutionFile = System.IO.Path.Combine(SolutionDir, "Revo.sln");

var PackagesDir = System.IO.Path.Combine(SolutionDir, "build", "packages");
var ReportsDir = System.IO.Path.Combine(SolutionDir, "build", "reports");

bool IsCiBuild = AppVeyor.IsRunningOnAppVeyor;

int? BuildNumber =
    HasArgument("BuildNumber") ? (int?)Argument<int>("BuildNumber") :
    AppVeyor.IsRunningOnAppVeyor ? (int?)AppVeyor.Environment.Build.Number :
    EnvironmentVariable("BuildNumber") != null ? (int?)int.Parse(EnvironmentVariable("BuildNumber")) : null;

string VersionSuffix = HasArgument("VersionSuffix") ? Argument<string>("VersionSuffix") : null;

// load VersionSuffix (if explicitly specified in Common.props)
if (VersionSuffix == null)
{
  var xmlDocument = new XmlDocument();
  xmlDocument.Load(System.IO.Path.Combine(SolutionDir, "Common.props"));

  var node = xmlDocument.SelectSingleNode("Project/PropertyGroup/VersionSuffix") as XmlElement;
  if (node != null)
  {
    VersionSuffix = node.InnerText;
  }
}

// append the VersionSuffix for non-release CI builds
string ciTag = AppVeyor.Environment.Repository.Tag.IsTag ? AppVeyor.Environment.Repository.Tag.Name : null;
string ciBranch = AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Repository.Branch : null;

if (BuildNumber.HasValue && ciTag == null && (string.IsNullOrWhiteSpace(ciBranch) || ciBranch != "master"))
{
  VersionSuffix = VersionSuffix != null
    ? $"{VersionSuffix}-build{BuildNumber:00000}"
    : $"build{BuildNumber:00000}";
}

string GetXunitXmlReportFilePath(FilePath projectFile)
{
  return new DirectoryPath(ReportsDir).CombineWithFilePath(projectFile.GetFilenameWithoutExtension()).FullPath + ".xml";
}

Task("Default")
    .IsDependentOn("Pack");

Task("Clean")
  .Does(() =>
  {
    if (IsCleanEnabled)
    {
      CleanDirectories(new []{ PackagesDir, ReportsDir });

      var msbuildSettings = new MSBuildSettings
        {
          Verbosity = Verbosity.Minimal,
          ToolVersion = MSBuildToolVersion.VS2017,
          Configuration = Configuration,
          PlatformTarget = PlatformTarget.MSIL,
          ArgumentCustomization = args => args
        };

      msbuildSettings.Targets.Add("Clean");

      MSBuild(SolutionFile, msbuildSettings);
    }
  });

Task("Restore")
  .IsDependentOn("Clean")
  .Does(() =>
  {
    if (IsRestoreEnabled)
    {
      NuGetRestore(
        SolutionFile,
        new NuGetRestoreSettings ()
        {
          Verbosity = NuGetVerbosity.Normal
        });
    }
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() =>
  {
    if (IsBuildEnabled)
    {
      MSBuild(SolutionFile,
      new MSBuildSettings
      {
        Verbosity = Verbosity.Minimal,
        ToolVersion = MSBuildToolVersion.VS2017,
        Configuration = Configuration,
        PlatformTarget = PlatformTarget.MSIL,
        ArgumentCustomization = args => args
          .Append($"/p:VersionSuffix={VersionSuffix}")
          .Append("/p:ci=true")
          .AppendSwitch("/p:DebugType", "=", Configuration == "Release" ? "portable" : "full")
          .AppendSwitch("/p:ContinuousIntegrationBuild", "=", IsCiBuild ? "true" : "false")
          .AppendSwitch("/p:DeterministicSourcePaths", "=", "false") // Temporary workaround for https://github.com/dotnet/sourcelink/issues/91
      });
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() =>
  {
    if (IsTestEnabled)
    {
      var projectFiles = GetFiles("./**/Revo.*.Tests.csproj");
      foreach (var projectFile in projectFiles)
      {
        var arguments = new ProcessArgumentBuilder()
          .Append("-configuration " + Configuration)
          .Append("-nobuild")
          .Append($"-xml {GetXunitXmlReportFilePath(projectFile)}");

        var parsedProject = ParseProject(projectFile.FullPath, configuration: "Debug");
        if (parsedProject.TargetFrameworkVersions.Contains("netcoreapp2.0"))
        {
          arguments = arguments.Append("-framework netcoreapp2.0");
          arguments = arguments.Append("-fxversion 2.0.7");
        }
      else if (parsedProject.TargetFrameworkVersions.Contains("netcoreapp2.1"))
        {
          arguments = arguments.Append("-framework netcoreapp2.1");
          arguments = arguments.Append("-fxversion 2.1.3");
        }

        var dotnetTestSettings = new DotNetCoreToolSettings
        {
          WorkingDirectory = projectFile.GetDirectory().FullPath
        };

        DotNetCoreTool(projectFile.FullPath, "xunit", arguments, dotnetTestSettings);
      }
    }
  });

Task("Pack")
  .IsDependentOn("Test")
  .Does(() =>
  {
    if (IsPackEnabled)
    {
      foreach (var projectFile in GetFiles("./**/Revo.*.csproj")) // without the "Revo.*" prefix, it also matches stuff from ./tools
      {
        if (!projectFile.GetFilename().FullPath.StartsWith("Revo.")
        || projectFile.GetFilename().FullPath.EndsWith(".Tests.csproj")
        || projectFile.GetFilename().FullPath.StartsWith("Revo.Examples."))
        {
          continue;
        }

        DotNetCorePack(
          projectFile.FullPath,
          new DotNetCorePackSettings
          {
            Configuration = Configuration,
            OutputDirectory = PackagesDir,
            NoBuild = true,
            NoRestore = true,
            IncludeSymbols = true,
            Verbosity = DotNetCoreVerbosity.Minimal,
            VersionSuffix = VersionSuffix
          });
      }
    }
  });

RunTarget(Target);