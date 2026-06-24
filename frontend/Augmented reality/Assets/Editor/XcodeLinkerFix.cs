#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEngine;

public static class XcodeLinkerFix
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("[XcodeLinkerFix] Post-processing Xcode project to fix linker flags...");

        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        // Retrieve GUIDs for both targets
        string mainTargetGuid = project.GetUnityMainTargetGuid();
        string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

        // Strip the problematic classic linker flags if they exist
        RemoveLinkerFlags(project, mainTargetGuid);
        RemoveLinkerFlags(project, frameworkTargetGuid);

        project.WriteToFile(projectPath);
        Debug.Log("[XcodeLinkerFix] Successfully removed -ld_classic and -ld64 from OTHER_LDFLAGS.");
    }

    private static void RemoveLinkerFlags(PBXProject project, string targetGuid)
    {
        if (string.IsNullOrEmpty(targetGuid))
            return;

        project.UpdateBuildProperty(
            targetGuid,
            "OTHER_LDFLAGS",
            new string[0],
            new string[] { "-ld_classic", "-ld64" }
        );
    }
}
#endif
