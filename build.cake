#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var solutionDir = Directory("./");
var solution = solutionDir + File("icfpc2017.sln");
var buildLibDir = Directory("./lib/bin") + Directory(configuration);
var buildWorkerDir = Directory("./worker/bin") + Directory(configuration);
var buildPunterDir = Directory("./punter/bin") + Directory(configuration);
var buildSettings = new DotNetCoreBuildSettings
{
    Configuration = configuration
};


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildLibDir);
    CleanDirectory(buildWorkerDir);
    CleanDirectory(buildPunterDir);
});

Task("Restore")
    .Does(() => {
        DotNetCoreRestore(solution);
    });


Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreBuild(solution, buildSettings);
});


//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
