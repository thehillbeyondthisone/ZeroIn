using System;
using System.IO;
using Newtonsoft.Json;
using AOSharp.Core;

namespace Shared
{
    /// <summary>
    /// Base configuration class for ZeroIn
    /// </summary>
    public abstract class BuddyBaseConfig<T> where T : BuddyBaseConfig<T>, new()
    {
        public abstract string FileName { get; }
        public abstract T LoadDefaults { get; }

        public static T LoadConfig(string path)
        {
            try
            {
                string fullPath = Path.Combine(path, new T().FileName + ".json");

                if (File.Exists(fullPath))
                {
                    string json = File.ReadAllText(fullPath);
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load config: {ex.Message}");
            }

            return new T().LoadDefaults;
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AOSharp",
                    "AOSP",
                    "ZeroIn",
                    DynelManager.LocalPlayer.Name
                );

                Directory.CreateDirectory(path);
                string fullPath = Path.Combine(path, FileName + ".json");
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(fullPath, json);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to save config: {ex.Message}");
            }
        }
    }
}
