using System.IO;
using UnityEditor;

namespace DecentM.AutoDeploy
{
    public static class PackageExporter
    {
        public static void ExportPackage(string name, string[] paths)
        {
            string output = $"PackageExporter/{name}.unitypackage";

            DirectoryInfo dir = new FileInfo(output).Directory;

            if (dir != null && !dir.Exists)
                dir.Create();

            AssetDatabase.ExportPackage(
                paths,
                output,
                ExportPackageOptions.Recurse
            );
        }
    }
}
