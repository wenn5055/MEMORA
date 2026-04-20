using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnsureRailingFirefliesProgramAsset
{
    private const string ScriptPath = "Assets/Scripts/Udon/RailingFirefliesController.cs";
    private const string ProgramAssetPath = "Assets/Scripts/Udon/RailingFirefliesControllerProgram.asset";
    private const string TargetPath = "fireflies_notrees/RailingFirefliesDetail";

    [MenuItem("Tools/Fireflies/Ensure Railing Fireflies Program Asset")]
    public static void Apply()
    {
        MonoScript sourceScript = AssetDatabase.LoadAssetAtPath<MonoScript>(ScriptPath);
        if (sourceScript == null)
        {
            Debug.LogError($"Could not load source script at '{ScriptPath}'.");
            return;
        }

        UdonSharpProgramAsset programAsset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(ProgramAssetPath);
        if (programAsset == null)
        {
            programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            AssetDatabase.CreateAsset(programAsset, ProgramAssetPath);
        }

        programAsset.sourceCsScript = sourceScript;
        EditorUtility.SetDirty(programAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ProgramAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        UdonSharpProgramAsset.CompileAllCsPrograms(false, true);

        GameObject target = GameObject.Find(TargetPath);
        if (target == null)
        {
            Debug.LogError($"Could not find '{TargetPath}' in the active scene.");
            return;
        }

        RailingFirefliesController controller = target.GetComponent<RailingFirefliesController>();
        if (controller == null)
        {
            Debug.LogError("RailingFirefliesDetail does not have a RailingFirefliesController component.");
            return;
        }

        MethodInfo runSetup = typeof(UdonSharpEditorUtility).GetMethod(
            "RunBehaviourSetupWithUndo",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (runSetup == null)
        {
            Debug.LogError("Could not locate UdonSharpEditorUtility.RunBehaviourSetupWithUndo via reflection.");
            return;
        }

        runSetup.Invoke(null, new object[] { controller });

        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(controller);
        if (controller.gameObject.TryGetComponent<VRC.Udon.UdonBehaviour>(out var udonBehaviour))
        {
            EditorUtility.SetDirty(udonBehaviour);
        }

        EditorSceneManager.MarkSceneDirty(target.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Ensured RailingFirefliesController program asset and rebound the scene object.");
    }
}
