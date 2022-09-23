using UnityEditor;
using DecentM.AutoDeploy;

public static class Exporter
{
    [MenuItem("DecentM/Export AutoDeploy Package")]
    public static void Export()
    {
        PackageExporter.ExportPackage("DecentM.AutoDeploy", new string[]
        {
            "Assets/DecentM/AutoDeploy"
        });
    }
}
