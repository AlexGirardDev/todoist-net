#addin "Cake.Incubator"

var target = Argument("target", "Default");
var extensionsVersion = Argument("version", "1.1.4");

var buildConfiguration = "Release";
var projectName = "Todoist.Net";
var testProjectName = "Todoist.Net.Tests";
var projectFolder = string.Format("./src/{0}/", projectName);
var testProjectFolder = string.Format("./src/{0}/", testProjectName);

Task("UpdateBuildVersion")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
	var buildNumber = BuildSystem.AppVeyor.Environment.Build.Number;

	BuildSystem.AppVeyor.UpdateBuildVersion(string.Format("{0}.{1}", extensionsVersion, buildNumber));
});

Task("NugetRestore")
  .Does(() =>
{
	DotNetCoreRestore();
});

Task("UpdateAssemblyVersion")
  .Does(() =>
{
	var assemblyFile = string.Format("{0}/Properties/AssemblyInfo.cs", projectFolder);

	AssemblyInfoSettings assemblySettings = new AssemblyInfoSettings();
	assemblySettings.Title = projectName;
	assemblySettings.FileVersion = extensionsVersion;
	assemblySettings.Version = extensionsVersion;
	assemblySettings.InternalsVisibleTo = new [] { testProjectName };

	CreateAssemblyInfo(assemblyFile, assemblySettings);
});

Task("Build")
  .IsDependentOn("NugetRestore")
  .IsDependentOn("UpdateAssemblyVersion")
  .Does(() =>
{
	MSBuild(string.Format("{0}.sln", projectName), new MSBuildSettings {
		Verbosity = Verbosity.Minimal,
		Configuration = buildConfiguration
    });
});

Task("Test")
  .IsDependentOn("Build")
  .Does(() =>
{
     var settings = new DotNetCoreTestSettings
     {
         Configuration = buildConfiguration
     };
	 var xunitSettings = new XUnit2Settings() 
	 {
		 OutputDirectory = ".",
		 XmlReport = true
	 };

     DotNetCoreTest(settings, testProjectFolder, xunitSettings);
});

Task("UploadTestResults")
  .IsDependentOn("Test")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
	BuildSystem.AppVeyor.UploadTestResults("src.xml", AppVeyorTestResultsType.XUnit);
});

Task("NugetPack")
  .IsDependentOn("Build")
  .Does(() =>
{
     var settings = new DotNetCorePackSettings
     {
         Configuration = buildConfiguration,
         OutputDirectory = "."
     };

     DotNetCorePack(projectFolder, settings);
});

Task("CreateArtifact")
  .IsDependentOn("NugetPack")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
	BuildSystem.AppVeyor.UploadArtifact(string.Format("{0}.{1}.nupkg", projectName, extensionsVersion));
	BuildSystem.AppVeyor.UploadArtifact(string.Format("{0}.{1}.symbols.nupkg", projectName, extensionsVersion));
});

Task("Default")
	.IsDependentOn("Test")
	.IsDependentOn("NugetPack");

Task("CI")
	.IsDependentOn("UploadTestResults")
	.IsDependentOn("CreateArtifact");

RunTarget(target);
