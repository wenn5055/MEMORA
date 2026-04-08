using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTriangleReportWindow : EditorWindow
{
    private const int DefaultTopCount = 20;

    private Vector2 _scroll;
    private SceneTriangleReport _report;
    private int _topCount = DefaultTopCount;
    private bool _includeInactive = true;
    private bool _sortDescending = true;
    private string _lastReportAssetPath;

    [MenuItem("Tools/MEMORA/Scene Triangle Report")]
    private static void OpenWindow()
    {
        var window = GetWindow<SceneTriangleReportWindow>("Scene Triangle Report");
        window.minSize = new Vector2(760f, 460f);
        window.RefreshReport();
    }

    [MenuItem("Tools/MEMORA/Run Scene Triangle Report")]
    private static void RunQuickReport()
    {
        var report = SceneTriangleAnalyzer.BuildReport(includeInactive: true);
        SceneTriangleAnalyzer.WriteConsoleSummary(report, DefaultTopCount);
        var assetPath = SceneTriangleAnalyzer.WriteTextReport(report, DefaultTopCount);

        EditorUtility.DisplayDialog(
            "Scene Triangle Report",
            $"Scene: {report.SceneName}\n" +
            $"Objects counted: {report.Entries.Count}\n" +
            $"Total triangles: {report.TotalTriangles:N0}\n" +
            $"Top object: {report.Entries.FirstOrDefault()?.ObjectPath ?? "None"}\n" +
            $"Report saved to:\n{assetPath}",
            "OK");
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(8f);

        if (_report == null)
        {
            EditorGUILayout.HelpBox("Click Refresh to analyze the active scene.", MessageType.Info);
            return;
        }

        DrawSummary();
        EditorGUILayout.Space(8f);
        DrawTable();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            _includeInactive = GUILayout.Toggle(_includeInactive, "Include Inactive", EditorStyles.toolbarButton, GUILayout.Width(110f));
            _sortDescending = GUILayout.Toggle(_sortDescending, "Sort Desc", EditorStyles.toolbarButton, GUILayout.Width(80f));

            GUILayout.Space(8f);
            GUILayout.Label("Top", GUILayout.Width(24f));
            _topCount = EditorGUILayout.IntField(_topCount, GUILayout.Width(50f));
            _topCount = Mathf.Max(1, _topCount);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                RefreshReport();
            }

            if (GUILayout.Button("Export TXT", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                ExportReport();
            }
        }
    }

    private void DrawSummary()
    {
        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Scene", _report.SceneName);
        EditorGUILayout.LabelField("Total Triangles", _report.TotalTriangles.ToString("N0"));
        EditorGUILayout.LabelField("Renderable Objects", _report.Entries.Count.ToString("N0"));
        EditorGUILayout.LabelField("Unique Mesh Assets", _report.UniqueMeshCount.ToString("N0"));
        EditorGUILayout.LabelField("Generated At", _report.GeneratedAtLocal.ToString("yyyy-MM-dd HH:mm:ss"));

        if (!string.IsNullOrEmpty(_lastReportAssetPath))
        {
            EditorGUILayout.LabelField("Last Export", _lastReportAssetPath);
        }
    }

    private void DrawTable()
    {
        EditorGUILayout.LabelField("Per Object", EditorStyles.boldLabel);

        using (var scrollView = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scrollView.scrollPosition;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Triangles", EditorStyles.boldLabel, GUILayout.Width(90f));
                GUILayout.Label("Type", EditorStyles.boldLabel, GUILayout.Width(120f));
                GUILayout.Label("Active", EditorStyles.boldLabel, GUILayout.Width(50f));
                GUILayout.Label("Enabled", EditorStyles.boldLabel, GUILayout.Width(55f));
                GUILayout.Label("Object Path", EditorStyles.boldLabel);
            }

            foreach (var entry in _report.Entries.Take(_topCount))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(entry.TriangleCount.ToString("N0"), GUILayout.Width(90f));
                    GUILayout.Label(entry.SourceType, GUILayout.Width(120f));
                    GUILayout.Label(entry.ActiveInHierarchy ? "Yes" : "No", GUILayout.Width(50f));
                    GUILayout.Label(entry.RendererEnabled ? "Yes" : "No", GUILayout.Width(55f));

                    if (GUILayout.Button(entry.ObjectPath, EditorStyles.linkLabel))
                    {
                        Selection.activeInstanceID = entry.InstanceId;
                        EditorGUIUtility.PingObject(EditorUtility.InstanceIDToObject(entry.InstanceId));
                    }
                }
            }
        }
    }

    private void RefreshReport()
    {
        _report = SceneTriangleAnalyzer.BuildReport(_includeInactive, _sortDescending);
        Repaint();
    }

    private void ExportReport()
    {
        if (_report == null)
        {
            RefreshReport();
        }

        _lastReportAssetPath = SceneTriangleAnalyzer.WriteTextReport(_report, _topCount);
        ShowNotification(new GUIContent("Triangle report exported"));
        Repaint();
    }
}

internal static class SceneTriangleAnalyzer
{
    private const string ReportDirectory = "Assets/Reports";

    internal static SceneTriangleReport BuildReport(bool includeInactive, bool sortDescending = true)
    {
        var scene = SceneManager.GetActiveScene();
        var entries = new List<SceneTriangleEntry>();
        var uniqueMeshPaths = new HashSet<string>();

        foreach (var root in scene.GetRootGameObjects())
        {
            CollectMeshFilterEntries(root, includeInactive, entries, uniqueMeshPaths);
            CollectSkinnedMeshEntries(root, includeInactive, entries, uniqueMeshPaths);
        }

        IEnumerable<SceneTriangleEntry> sortedEntries = sortDescending
            ? entries.OrderByDescending(entry => entry.TriangleCount).ThenBy(entry => entry.ObjectPath, StringComparer.Ordinal)
            : entries.OrderBy(entry => entry.ObjectPath, StringComparer.Ordinal);

        return new SceneTriangleReport(
            scene.name,
            sortedEntries.ToList(),
            uniqueMeshPaths.Count,
            DateTime.Now);
    }

    internal static void WriteConsoleSummary(SceneTriangleReport report, int topCount)
    {
        Debug.Log(BuildPlainTextReport(report, topCount));
    }

    internal static string WriteTextReport(SceneTriangleReport report, int topCount)
    {
        if (!AssetDatabase.IsValidFolder(ReportDirectory))
        {
            AssetDatabase.CreateFolder("Assets", "Reports");
        }

        var safeSceneName = string.IsNullOrWhiteSpace(report.SceneName) ? "UntitledScene" : report.SceneName;
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            safeSceneName = safeSceneName.Replace(invalidChar, '_');
        }

        var assetPath = $"{ReportDirectory}/{safeSceneName}_TriangleReport.txt";
        File.WriteAllText(assetPath, BuildPlainTextReport(report, topCount), Encoding.UTF8);
        AssetDatabase.Refresh();
        return assetPath;
    }

    private static void CollectMeshFilterEntries(
        GameObject root,
        bool includeInactive,
        ICollection<SceneTriangleEntry> entries,
        ISet<string> uniqueMeshPaths)
    {
        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(includeInactive))
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer == null)
            {
                continue;
            }

            entries.Add(CreateEntry(meshFilter.gameObject, meshFilter.sharedMesh, renderer, nameof(MeshFilter)));
            AddUniqueMeshPath(meshFilter.sharedMesh, uniqueMeshPaths);
        }
    }

    private static void CollectSkinnedMeshEntries(
        GameObject root,
        bool includeInactive,
        ICollection<SceneTriangleEntry> entries,
        ISet<string> uniqueMeshPaths)
    {
        foreach (var skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive))
        {
            if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
            {
                continue;
            }

            entries.Add(CreateEntry(skinnedMeshRenderer.gameObject, skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer, nameof(SkinnedMeshRenderer)));
            AddUniqueMeshPath(skinnedMeshRenderer.sharedMesh, uniqueMeshPaths);
        }
    }

    private static SceneTriangleEntry CreateEntry(GameObject gameObject, Mesh mesh, Renderer renderer, string sourceType)
    {
        return new SceneTriangleEntry(
            gameObject.GetInstanceID(),
            GetHierarchyPath(gameObject.transform),
            sourceType,
            CountTriangles(mesh),
            gameObject.activeInHierarchy,
            renderer.enabled,
            mesh.name);
    }

    private static void AddUniqueMeshPath(Mesh mesh, ISet<string> uniqueMeshPaths)
    {
        var assetPath = AssetDatabase.GetAssetPath(mesh);
        if (!string.IsNullOrEmpty(assetPath))
        {
            uniqueMeshPaths.Add(assetPath);
        }
    }

    private static int CountTriangles(Mesh mesh)
    {
        if (mesh == null)
        {
            return 0;
        }

        var triangleCount = 0;
        for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            triangleCount += (int)mesh.GetIndexCount(subMeshIndex) / 3;
        }

        return triangleCount;
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return string.Empty;
        }

        var path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.name}/{path}";
        }

        return path;
    }

    private static string BuildPlainTextReport(SceneTriangleReport report, int topCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Scene Triangle Report - {report.SceneName}");
        builder.AppendLine($"Generated: {report.GeneratedAtLocal:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Total Triangles: {report.TotalTriangles:N0}");
        builder.AppendLine($"Renderable Objects: {report.Entries.Count:N0}");
        builder.AppendLine($"Unique Mesh Assets: {report.UniqueMeshCount:N0}");
        builder.AppendLine();
        builder.AppendLine($"Top {Mathf.Min(topCount, report.Entries.Count)} Objects");
        builder.AppendLine("Triangles\tType\tActive\tEnabled\tMesh\tObject Path");

        foreach (var entry in report.Entries.Take(topCount))
        {
            builder.AppendLine($"{entry.TriangleCount}\t{entry.SourceType}\t{entry.ActiveInHierarchy}\t{entry.RendererEnabled}\t{entry.MeshName}\t{entry.ObjectPath}");
        }

        builder.AppendLine();
        builder.AppendLine("All Objects");
        builder.AppendLine("Triangles\tType\tActive\tEnabled\tMesh\tObject Path");

        foreach (var entry in report.Entries)
        {
            builder.AppendLine($"{entry.TriangleCount}\t{entry.SourceType}\t{entry.ActiveInHierarchy}\t{entry.RendererEnabled}\t{entry.MeshName}\t{entry.ObjectPath}");
        }

        return builder.ToString();
    }
}

internal sealed class SceneTriangleReport
{
    internal SceneTriangleReport(string sceneName, List<SceneTriangleEntry> entries, int uniqueMeshCount, DateTime generatedAtLocal)
    {
        SceneName = sceneName;
        Entries = entries;
        UniqueMeshCount = uniqueMeshCount;
        GeneratedAtLocal = generatedAtLocal;
        TotalTriangles = entries.Sum(entry => entry.TriangleCount);
    }

    internal string SceneName { get; }
    internal List<SceneTriangleEntry> Entries { get; }
    internal int UniqueMeshCount { get; }
    internal DateTime GeneratedAtLocal { get; }
    internal int TotalTriangles { get; }
}

internal sealed class SceneTriangleEntry
{
    internal SceneTriangleEntry(
        int instanceId,
        string objectPath,
        string sourceType,
        int triangleCount,
        bool activeInHierarchy,
        bool rendererEnabled,
        string meshName)
    {
        InstanceId = instanceId;
        ObjectPath = objectPath;
        SourceType = sourceType;
        TriangleCount = triangleCount;
        ActiveInHierarchy = activeInHierarchy;
        RendererEnabled = rendererEnabled;
        MeshName = meshName;
    }

    internal int InstanceId { get; }
    internal string ObjectPath { get; }
    internal string SourceType { get; }
    internal int TriangleCount { get; }
    internal bool ActiveInHierarchy { get; }
    internal bool RendererEnabled { get; }
    internal string MeshName { get; }
}
