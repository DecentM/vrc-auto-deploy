#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace DecentM.AutoDeploy
{
    public static class PackageExporter
    {
#if UNITY_EDITOR
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
#endif
    }
}
