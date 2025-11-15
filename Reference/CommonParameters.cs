using AOSharp.Core;
using System;
using System.IO;

public class CommonParameters
{
    public const string BasePath = "AOSharp";
    public const string AppPath = "AOSP";

    private static string _pluginName;

    public static void Init(string pluginName)
    {
        _pluginName = pluginName;

        var pluginRootPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{BasePath}\\{AppPath}\\{_pluginName}";
        Directory.CreateDirectory(pluginRootPath);
        PluginDataPath = pluginRootPath;
        PlayerSettingsPath = $"{pluginRootPath}\\{DynelManager.LocalPlayer.Name}";
    }

    public static string PluginDataPath { get; protected set; }

    public static string PlayerSettingsPath { get; protected set; }
}
