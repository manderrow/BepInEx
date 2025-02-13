using System;
using System.IO;

namespace BepInEx.Preloader.Core;

/// <summary>
///     Doorstop environment variables, passed into the BepInEx preloader.
///     <para>https://github.com/NeighTools/UnityDoorstop/wiki#environment-variables</para>
/// </summary>
public static class EnvVars
{
    /// <summary>
    ///     Path to the assembly that was invoked via Doorstop. Contains the same value as in "targetAssembly" configuration
    ///     option in the config file.
    /// </summary>
    public static readonly string DOORSTOP_INVOKE_DLL_PATH = Environment.GetEnvironmentVariable("DOORSTOP_INVOKE_DLL_PATH");

    /// <summary>
    ///     Full path to the game's "Managed" folder that contains all the game's managed assemblies
    /// </summary>
    public static readonly string DOORSTOP_MANAGED_FOLDER_DIR = Environment.GetEnvironmentVariable("DOORSTOP_MANAGED_FOLDER_DIR");

    /// <summary>
    ///     Full path to the game executable currently running.
    /// </summary>
    public static readonly string DOORSTOP_PROCESS_PATH = Environment.GetEnvironmentVariable("DOORSTOP_PROCESS_PATH");

    /// <summary>
    ///     Array of paths where Mono searches DLLs from before assembly resolvers are invoked.
    /// </summary>
    public static readonly string[] DOORSTOP_DLL_SEARCH_DIRS =
            Environment.GetEnvironmentVariable("DOORSTOP_DLL_SEARCH_DIRS")?.Split(Path.PathSeparator) ??
            new string[0];

    /// <summary>
    ///     Path of the DLL that contains mono imports.
    /// </summary>
    public static readonly string DOORSTOP_MONO_LIB_PATH = Environment.GetEnvironmentVariable("DOORSTOP_MONO_LIB_PATH");
}
