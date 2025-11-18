using AOSharp.Pathfinding;
using Newtonsoft.Json;
using System;
using System.IO;

namespace AutomatonRoamba
{
    public class RoamPath
    {
        public SPath SPath;
        public TargetingRules Rules;

        public RoamPath()
        {
            SPath = SPath.Create();
            Rules = new TargetingRules();
        }

        public static RoamPath Load(string path)
        {
            RoamPath roamPath;

            try
            {
                roamPath = JsonConvert.DeserializeObject<RoamPath>(File.ReadAllText(path));
            }
            catch
            {
                roamPath = new RoamPath();
            }

            roamPath.SPath.IsLocked = true;

            return roamPath;
        }

        public void Save(string savePath)
        {
            try
            {
                File.WriteAllText(savePath, JsonConvert.SerializeObject(this, Formatting.Indented));
                AutomatonRoamba.Log.Information($"File saved at '{savePath}'");
            }
            catch (Exception ex)
            {
                AutomatonRoamba.Log.Warning(ex.Message);
            }
        }
    }
}