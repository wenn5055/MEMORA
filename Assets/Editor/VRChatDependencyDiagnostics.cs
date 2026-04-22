using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class VRChatDependencyDiagnostics
{
    private const string SessionKey = "MEMORA.VRChatDependencyDiagnostics.Shown";
    private const string VpmManifestRelativePath = "Packages/vpm-manifest.json";
    private const string ManifestRelativePath = "Packages/manifest.json";

    static VRChatDependencyDiagnostics()
    {
        EditorApplication.delayCall += RunStartupCheck;
    }

    [MenuItem("Tools/MEMORA/Diagnose VRChat Dependencies")]
    public static void DiagnoseFromMenu()
    {
        Diagnose(showDialog: true);
    }

    [MenuItem("Tools/MEMORA/Restore VRChat Dependencies")]
    public static void RestoreFromMenu()
    {
        DependencyReport report = BuildReport();
        if (!report.VpmManifestExists)
        {
            EditorUtility.DisplayDialog(
                "VRChat Dependency Restore",
                "This project does not have Packages/vpm-manifest.json, so there is nothing to restore through the VPM resolver.",
                "OK");
            return;
        }

        if (!TryRunResolver())
        {
            EditorUtility.DisplayDialog(
                "VRChat Dependency Restore",
                "The embedded VRChat package resolver is not available. Open the project through VRChat Creator Companion and let it restore the Worlds SDK packages.",
                "OK");
            return;
        }

        Debug.Log("Triggered the embedded VRChat package resolver.");
    }

    private static void RunStartupCheck()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunStartupCheck;
            return;
        }

        SessionState.SetBool(SessionKey, true);
        Diagnose(showDialog: true);
    }

    private static void Diagnose(bool showDialog)
    {
        DependencyReport report = BuildReport();
        if (!report.NeedsAttention)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "VRChat Dependency Diagnostics",
                    "No missing VRChat dependencies were detected between Packages/vpm-manifest.json and Packages/manifest.json.",
                    "OK");
            }

            return;
        }

        string message = BuildMessage(report);
        Debug.LogWarning(message);

        if (!showDialog)
        {
            return;
        }

        int action = EditorUtility.DisplayDialogComplex(
            "VRChat Dependencies Missing",
            message,
            "Run Resolver",
            "Later",
            "Log Help");

        if (action == 0)
        {
            if (!TryRunResolver())
            {
                EditorUtility.DisplayDialog(
                    "VRChat Dependencies Missing",
                    "The embedded resolver could not be started. Open the project through VRChat Creator Companion and let it restore the required packages.",
                    "OK");
            }
        }
        else if (action == 2)
        {
            Debug.Log(message);
        }
    }

    private static DependencyReport BuildReport()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string manifestPath = Path.Combine(projectRoot, ManifestRelativePath);
        string vpmManifestPath = Path.Combine(projectRoot, VpmManifestRelativePath);

        HashSet<string> manifestDependencies = ParseTopLevelObjectKeys(manifestPath, "dependencies");
        HashSet<string> vpmDependencies = ParseTopLevelObjectKeys(vpmManifestPath, "dependencies");

        List<string> requiredVrchatPackages = new List<string>();
        foreach (string packageId in vpmDependencies)
        {
            if (packageId.StartsWith("com.vrchat.", StringComparison.Ordinal))
            {
                requiredVrchatPackages.Add(packageId);
            }
        }

        requiredVrchatPackages.Sort(StringComparer.Ordinal);

        List<string> missingPackages = new List<string>();
        foreach (string packageId in requiredVrchatPackages)
        {
            if (!manifestDependencies.Contains(packageId))
            {
                missingPackages.Add(packageId);
            }
        }

        return new DependencyReport
        {
            ManifestExists = File.Exists(manifestPath),
            VpmManifestExists = File.Exists(vpmManifestPath),
            MissingPackages = missingPackages
        };
    }

    private static string BuildMessage(DependencyReport report)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("This project declares VRChat packages in Packages/vpm-manifest.json, but Unity's current Packages/manifest.json does not include all of them.");
        builder.AppendLine();
        builder.AppendLine("Missing packages:");
        foreach (string packageId in report.MissingPackages)
        {
            builder.AppendLine($"- {packageId}");
        }

        builder.AppendLine();
        builder.AppendLine("Likely symptom: missing UdonSharp / VRC.* namespaces and widespread compile errors.");
        builder.AppendLine();
        builder.AppendLine("Recovery steps:");
        builder.AppendLine("1. Open the cloned repository through VRChat Creator Companion.");
        builder.AppendLine("2. Let VRChat Package Management restore the Worlds SDK packages.");
        builder.AppendLine("3. Reopen Unity 2022.3.22f1 after the restore finishes.");
        builder.AppendLine("4. If needed, run Tools/MEMORA/Restore VRChat Dependencies to trigger the embedded resolver again.");

        return builder.ToString().TrimEnd();
    }

    private static bool TryRunResolver()
    {
        Type resolverType = FindType("VRC.PackageManagement.Resolver.Resolver");
        if (resolverType == null)
        {
            return false;
        }

        MethodInfo resolveManifest = resolverType.GetMethod("ResolveManifest", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (resolveManifest == null)
        {
            return false;
        }

        resolveManifest.Invoke(null, null);
        return true;
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName, false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static HashSet<string> ParseTopLevelObjectKeys(string path, string propertyName)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return result;
        }

        string json = File.ReadAllText(path);
        int objectStart = FindNamedObjectStart(json, propertyName);
        if (objectStart < 0)
        {
            return result;
        }

        int depth = 1;

        for (int index = objectStart + 1; index < json.Length; index++)
        {
            char current = json[index];

            if (current == '"')
            {
                string key = ReadJsonString(json, ref index);
                if (depth == 1 && IsPropertyKey(json, index))
                {
                    result.Add(key);
                }

                continue;
            }

            if (current == '{')
            {
                depth++;
                continue;
            }

            if (current == '}')
            {
                depth--;
                if (depth == 0)
                {
                    break;
                }
            }
        }

        return result;
    }

    private static int FindNamedObjectStart(string json, string propertyName)
    {
        string marker = "\"" + propertyName + "\"";
        int propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
        if (propertyIndex < 0)
        {
            return -1;
        }

        int colonIndex = json.IndexOf(':', propertyIndex + marker.Length);
        if (colonIndex < 0)
        {
            return -1;
        }

        for (int index = colonIndex + 1; index < json.Length; index++)
        {
            if (char.IsWhiteSpace(json[index]))
            {
                continue;
            }

            return json[index] == '{' ? index : -1;
        }

        return -1;
    }

    private static string ReadJsonString(string json, ref int index)
    {
        StringBuilder builder = new StringBuilder();
        bool escaping = false;

        for (int i = index + 1; i < json.Length; i++)
        {
            char current = json[i];
            if (escaping)
            {
                builder.Append(current);
                escaping = false;
                continue;
            }

            if (current == '\\')
            {
                escaping = true;
                continue;
            }

            if (current == '"')
            {
                index = i;
                return builder.ToString();
            }

            builder.Append(current);
        }

        index = json.Length - 1;
        return builder.ToString();
    }

    private static bool IsPropertyKey(string json, int closingQuoteIndex)
    {
        for (int index = closingQuoteIndex + 1; index < json.Length; index++)
        {
            if (char.IsWhiteSpace(json[index]))
            {
                continue;
            }

            return json[index] == ':';
        }

        return false;
    }

    private sealed class DependencyReport
    {
        public bool ManifestExists;
        public bool VpmManifestExists;
        public List<string> MissingPackages;

        public bool NeedsAttention
        {
            get
            {
                return ManifestExists && VpmManifestExists && MissingPackages.Count > 0;
            }
        }
    }
}
