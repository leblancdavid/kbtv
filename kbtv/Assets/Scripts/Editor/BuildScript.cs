using UnityEditor;

public class BuildScript {
    [MenuItem("Build/Build Windows")]
    public static void BuildWindows() {
        BuildPipeline.BuildPlayer(
            new string[] { "Assets/Scenes/SampleScene.unity" },
            "Build/Windows/KBTV.exe",
            BuildTarget.StandaloneWindows64,
            BuildOptions.Development
        );
    }
}
