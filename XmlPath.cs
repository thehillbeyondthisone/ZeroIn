using System.IO;

namespace ZeroIn
{
    public class XmlPath
    {
        public static string ViewsRootDir;
        public static string WindowsRootDir;

        public static void Set(string pluginDir)
        {
            ViewsRootDir = $"{pluginDir}\\UI\\Views";
            WindowsRootDir = $"{pluginDir}\\UI\\Windows";
        }
    }
}
