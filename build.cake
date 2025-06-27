#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#addin nuget:?package=SharpZipLib&version=1.4.2
#addin nuget:?package=Cake.Compression&version=0.3.0
#addin nuget:?package=Cake.Json&version=7.0.1
#addin nuget:?package=Newtonsoft.Json&version=13.0.3

var target = Argument("target", "Build");
var isBleedingEdge = Argument("bleeding_edge", false);
var buildId = Argument("build_id", 0);
var lastBuildCommit = Argument("last_build_commit", "");

var buildVersion = "";
var currentCommit = RunGit("rev-parse HEAD");
var currentCommitShort = RunGit("log -n 1 --pretty=\"format:%h\"").Trim();
var currentBranch = RunGit("rev-parse --abbrev-ref HEAD");
var latestTag = RunGit("describe --tags --abbrev=0");

string RunGit(string command, string separator = "") 
{
    using(var process = StartAndReturnProcess("git", new ProcessSettings { Arguments = command, RedirectStandardOutput = true })) 
    {
        process.WaitForExit();
        return string.Join(separator, process.GetStandardOutput());
    }
}

Task("Cleanup")
    .Does(() =>
{
    Information("Removing old binaries");
    CreateDirectory("./bin");
    CleanDirectory("./bin");

    Information("Cleaning up old build objects");
    CleanDirectories(GetDirectories("./**/bin/"));
    CleanDirectories(GetDirectories("./**/obj/"));
});

Task("PullDependencies")
    .Does(() =>
{
    Information("Updating git submodules");
    StartProcess("git", "submodule update --init --recursive");

    Information("Restoring NuGet packages");
    DotNetRestore("./BepInEx.sln");
});

Task("Build")
    .IsDependentOn("Cleanup")
    .IsDependentOn("PullDependencies")
    .Does(() =>
{
    var bepinExProperties = Directory("./BepInEx/Properties");

    buildVersion = FindRegexMatchGroupInFile(File("Directory.Build.props"), "<BepInExVersionPrefix>(.+?)<\\/BepInExVersionPrefix>", 1, System.Text.RegularExpressions.RegexOptions.None).Value;

    var settings = new DotNetPublishSettings
    {
        Configuration = "Release",
    };

    if(isBleedingEdge)
    {
        settings.MSBuildSettings = new DotNetMSBuildSettings().WithProperty("BepInExVersionSuffix", "be." + buildId);

        buildVersion = buildVersion + "-be." + buildId;

        CopyFile(bepinExProperties + File("AssemblyInfo.cs"), bepinExProperties + File("AssemblyInfo.cs.bak"));

        FileAppendText(bepinExProperties + File("AssemblyInfo.cs"), 
            TransformText("\n[assembly: BepInEx.BuildInfo(\"BLEEDING EDGE Build #<%buildNumber%> from <%shortCommit%> at <%branchName%>\")]\n")
                .WithToken("buildNumber", buildId)
                .WithToken("shortCommit", currentCommit)
                .WithToken("branchName", currentBranch)
                .ToString());
    }


    settings.OutputDirectory = "./bin/";
    DotNetPublish("./BepInEx.Preloader/BepInEx.Preloader.csproj", settings);

    settings.OutputDirectory = "./bin/patcher/";
    DotNetPublish("./BepInEx.Patcher/BepInEx.Patcher.csproj", settings);
})
.Finally(() => 
{
    var bepinExProperties = Directory("./BepInEx/Properties");
    if(isBleedingEdge)
    {
        DeleteFile(bepinExProperties + File("AssemblyInfo.cs"));
        MoveFile(bepinExProperties + File("AssemblyInfo.cs.bak"), bepinExProperties + File("AssemblyInfo.cs"));
    }
});

Task("MakeDist")
    .IsDependentOn("Build")
    .Does(() =>
{
    var distDir = Directory("./bin/dist");
    var distPatcherDir = distDir + Directory("patcher");

    CreateDirectory(distDir);
    CreateDirectory(distPatcherDir);

    var changelog = TransformText("<%commit_count%> commits since <%last_tag%>\r\n\r\nChangelog (excluding merges):\r\n<%commit_log%>")
                        .WithToken("commit_count", RunGit($"rev-list --count {latestTag}..HEAD"))
                        .WithToken("last_tag", latestTag)
                        .WithToken("commit_log", RunGit($"--no-pager log --no-merges --pretty=\"format:* (%h) [%an] %s\" {latestTag}..HEAD", "\r\n"))
                        .ToString();

    void PackageBepin(string os, string arch)
    {
        var distArchDir = distDir + Directory($"{os}_{arch}");
        var bepinDir = distArchDir + Directory("BepInEx");

        CreateDirectory(distArchDir);
        CreateDirectory(bepinDir + Directory("core"));

        CopyFiles("./bin/*.*", bepinDir + Directory("core"));
        FileWriteText(distArchDir + File("changelog.txt"), changelog);
    }

    PackageBepin("win", "x64");
    PackageBepin("win", "x86");
    PackageBepin("linux", "x64");
    PackageBepin("linux", "x86");
    PackageBepin("macos", "x64");
    CopyFileToDirectory(File("./bin/patcher/BepInEx.Patcher.exe"), distPatcherDir);
});

Task("Pack")
    .IsDependentOn("MakeDist")
    .Does(() =>
{
    var distDir = Directory("./bin/dist");
    var commitPrefix = isBleedingEdge ? $"_{currentCommitShort}_" : "_";

    Information("Packing BepInEx");
    ZipCompress(distDir + Directory("win_x86"), distDir + File($"BepInEx_win_x86{commitPrefix}{buildVersion}.zip"));
    ZipCompress(distDir + Directory("win_x64"), distDir + File($"BepInEx_win_x64{commitPrefix}{buildVersion}.zip"));
    ZipCompress(distDir + Directory("linux_x86"), distDir + File($"BepInEx_linux_x86{commitPrefix}{buildVersion}.zip"));
    ZipCompress(distDir + Directory("linux_x64"), distDir + File($"BepInEx_linux_x64{commitPrefix}{buildVersion}.zip"));
    ZipCompress(distDir + Directory("macos_x64"), distDir + File($"BepInEx_macos_x64{commitPrefix}{buildVersion}.zip"));

    Information("Packing BepInEx.Patcher");
    ZipCompress(distDir + Directory("patcher"), distDir + File($"BepInEx_Patcher{commitPrefix}{buildVersion}.zip"));

    if(isBleedingEdge) 
    {
        var changelog = "";

        if(!string.IsNullOrEmpty(lastBuildCommit)) {
            changelog = TransformText("<ul><%changelog%></ul>")
                        .WithToken("changelog", RunGit($"--no-pager log --no-merges --pretty=\"format:<li>(<code>%h</code>) [%an] %s</li>\" {lastBuildCommit}..HEAD"))
                        .ToString();
        }

        FileWriteText(distDir + File("info.json"), 
            SerializeJsonPretty(new Dictionary<string, object>{
                ["id"] = buildId.ToString(),
                ["date"] = DateTime.Now.ToString("o"),
                ["changelog"] = changelog,
                ["hash"] = currentCommit,
                ["artifacts"] = new Dictionary<string, object>[] {
                    new Dictionary<string, object> {
                        ["file"] = $"BepInEx_x64{commitPrefix}{buildVersion}.zip",
                        ["description"] = "BepInEx for x64 machines"
                    },
                    new Dictionary<string, object> {
                        ["file"] = $"BepInEx_x86{commitPrefix}{buildVersion}.zip",
                        ["description"] = "BepInEx for x86 machines"
                    },
                    new Dictionary<string, object> {
                        ["file"] = $"BepInEx_unix{commitPrefix}{buildVersion}.zip",
                        ["description"] = "BepInEx for Unix with GCC (Linux, MacOS)"
                    },
                    new Dictionary<string, object> {
                        ["file"] = $"BepInEx_Patcher{commitPrefix}{buildVersion}.zip",
                        ["description"] = "Hardpatcher for BepInEx. IMPORTANT: USE ONLY IF DOORSTOP DOES NOT WORK FOR SOME REASON!"
                    }
                }
            }));
    }
});

RunTarget(target);