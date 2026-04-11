using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StreetLampRotationTool
{
    private const string LampRootPath = "CarScene-v0/Lamp";
    private const string PolePrefix = "StreetPole_";
    private const string ArmPrefix = "StreetArm_";
    private const string FixturePrefix = "StreetFixture_";
    private const float RotationAngle = 90f;

    [MenuItem("Tools/Scene/Rotate Street Lamps 90")]
    public static void RotateStreetLamps90()
    {
        GameObject lampRoot = GameObject.Find(LampRootPath);
        if (lampRoot == null)
        {
            Debug.LogError("StreetLampRotationTool: Could not find CarScene-v0/Lamp.");
            return;
        }

        Transform[] selectedTransforms = Selection.transforms;
        if (selectedTransforms == null || selectedTransforms.Length == 0)
        {
            Debug.LogWarning("StreetLampRotationTool: Select one or more street lamp objects first.");
            return;
        }

        Transform lampRootTransform = lampRoot.transform;
        Dictionary<string, Transform> childrenByName = new Dictionary<string, Transform>(lampRootTransform.childCount);
        for (int i = 0; i < lampRootTransform.childCount; i++)
        {
            Transform child = lampRootTransform.GetChild(i);
            childrenByName[child.name] = child;
        }

        HashSet<string> selectedSuffixes = new HashSet<string>();
        for (int i = 0; i < selectedTransforms.Length; i++)
        {
            Transform selected = selectedTransforms[i];
            if (selected == null)
            {
                continue;
            }

            string suffix = GetLampSuffix(selected.name);
            if (!string.IsNullOrEmpty(suffix))
            {
                selectedSuffixes.Add(suffix);
            }
        }

        if (selectedSuffixes.Count == 0)
        {
            Debug.LogWarning("StreetLampRotationTool: None of the selected objects are street lamp parts.");
            return;
        }

        int rotatedGroupCount = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Rotate street lamps 90");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (string suffix in selectedSuffixes)
        {
            if (!childrenByName.TryGetValue(PolePrefix + suffix, out Transform poleTransform) ||
                !childrenByName.TryGetValue(ArmPrefix + suffix, out Transform armTransform) ||
                !childrenByName.TryGetValue(FixturePrefix + suffix, out Transform fixtureTransform))
            {
                Debug.LogWarning("StreetLampRotationTool: Skipping lamp group " + suffix + " because a matching pole, arm, or fixture is missing.");
                continue;
            }

            Vector3 pivot = poleTransform.position;
            Undo.RecordObjects(
                new Object[] { poleTransform, armTransform, fixtureTransform },
                "Rotate street lamp group " + suffix);

            RotateAroundPivot(poleTransform, pivot);
            RotateAroundPivot(armTransform, pivot);
            RotateAroundPivot(fixtureTransform, pivot);
            rotatedGroupCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (rotatedGroupCount == 0)
        {
            Debug.LogWarning("StreetLampRotationTool: No complete selected street lamp groups were rotated.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("StreetLampRotationTool: Rotated " + rotatedGroupCount + " selected street lamp groups by 90 degrees.");
    }

    private static string GetLampSuffix(string objectName)
    {
        if (objectName.StartsWith(PolePrefix))
        {
            return objectName.Substring(PolePrefix.Length);
        }

        if (objectName.StartsWith(ArmPrefix))
        {
            return objectName.Substring(ArmPrefix.Length);
        }

        if (objectName.StartsWith(FixturePrefix))
        {
            return objectName.Substring(FixturePrefix.Length);
        }

        return null;
    }

    private static void RotateAroundPivot(Transform target, Vector3 pivot)
    {
        Quaternion rotation = Quaternion.Euler(0f, RotationAngle, 0f);
        Vector3 offset = target.position - pivot;
        target.position = pivot + rotation * offset;
        target.rotation = rotation * target.rotation;
    }
}
