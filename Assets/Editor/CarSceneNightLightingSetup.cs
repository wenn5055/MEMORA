using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CarSceneNightLightingSetup
{
    private const string ScenePath = "Assets/Scenes/Experiment Daphne/CarScene.unity";
    private const string SkyboxSourcePath = "Assets/MilkyWay/Material/MilkyWay.mat";
    private const string MaterialRootFolder = "Assets/Materials";
    private const string NightRootFolder = "Assets/Materials/CarSceneNight";
    private const string CityWindowFolder = "Assets/Materials/CarSceneNight/CityWindows";
    private const string StreetLightFolder = "Assets/Materials/CarSceneNight/StreetLights";
    private const string SkyboxFolder = "Assets/Materials/CarSceneNight/Skybox";
    private const string VehicleRootPath = "CarVehicleRoot";
    private const string CarVisualPath = "CarVehicleRoot/Car_reduced";
    private const string LampRootPath = "CarScene-v0/Lamp";
    private const string CityRootPath = "CarScene-v0/City";
    private const string DirectionalLightPath = "ExampleWorldSetup/Directional Light";
    private const string HeadlightRootName = "NightHeadlights";
    private const string LeftHeadlightName = "LeftHeadlight";
    private const string RightHeadlightName = "RightHeadlight";
    private const string StreetLampLightChildName = "NightRoadLampLight";
    private const string LightScriptClass = "Light";
    private const int LightClassId = 108;
    private const string NatureMaterialFolder = "Assets/Environment Assets/Simple Nature/Materials";
    private const string SimpleNatureBackgroundMaterialPath = NatureMaterialFolder + "/SimpleNaturePack_BG.mat";
    private const string SimpleNatureTextureMaterialPath = NatureMaterialFolder + "/SimpleNaturePack_Texture_01.mat";

    private static readonly HashSet<string> HighSkylineBuildings = new HashSet<string>
    {
        "Building",
        "Building.003",
        "Building.006",
        "Building.009",
        "Building.012",
    };

    private static readonly HashSet<string> MidSkylineBuildings = new HashSet<string>
    {
        "Building.001",
        "Building.004",
        "Building.007",
        "Building.010",
        "Building.013",
    };

    private static readonly HashSet<string> LowSkylineBuildings = new HashSet<string>
    {
        "Building.002",
        "Building.005",
        "Building.008",
        "Building.011",
        "Building.014",
    };

    private enum SkylineTier
    {
        Low,
        Mid,
        High,
    }

    [MenuItem("Tools/Scene/Apply CarScene Night Lighting")]
    public static void ApplyNightLighting()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        EnsureFolder(MaterialRootFolder);
        EnsureFolder(NightRootFolder);
        EnsureFolder(CityWindowFolder);
        EnsureFolder(StreetLightFolder);
        EnsureFolder(SkyboxFolder);

        Material nightSkybox = ConfigureNightSkybox();
        if (nightSkybox == null)
        {
            Debug.LogError("CarSceneNightLightingSetup: Could not configure the night skybox.");
            return;
        }

        ApplyEnvironment(nightSkybox);

        Light moonLight = FindComponentByPath<Light>(DirectionalLightPath);
        if (moonLight == null)
        {
            Debug.LogError("CarSceneNightLightingSetup: Could not find the scene directional light.");
            return;
        }

        ConfigureMoonLight(moonLight);

        CarVehicleController controller = FindComponentByPath<CarVehicleController>(VehicleRootPath);
        if (controller == null)
        {
            Debug.LogError("CarSceneNightLightingSetup: Could not find CarVehicleController on CarVehicleRoot.");
            return;
        }

        ConfigureCarHeadlights(controller);
        ConfigureStreetLights();
        ConfigureCitySkyline();
        ConfigureNatureMaterials();
        HideLightSceneIcons();

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(moonLight);
        AssetDatabase.SaveAssets();
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("CarSceneNightLightingSetup: Applied CarScene night lighting.");
    }

    private static Material ConfigureNightSkybox()
    {
        Material sourceSkybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxSourcePath);
        if (sourceSkybox == null)
        {
            return null;
        }

        Material nightSkybox = GetOrCreateMaterialVariant(sourceSkybox, SkyboxFolder, "Night");
        if (nightSkybox == null)
        {
            return null;
        }

        if (nightSkybox.HasProperty("_Tint"))
        {
            nightSkybox.SetColor("_Tint", new Color(0.96f, 0.98f, 1f, 1f));
        }

        if (nightSkybox.HasProperty("_Exposure"))
        {
            nightSkybox.SetFloat("_Exposure", 0.6f);
        }

        if (nightSkybox.HasProperty("_Rotation"))
        {
            nightSkybox.SetFloat("_Rotation", 180f);
        }

        EditorUtility.SetDirty(nightSkybox);
        return nightSkybox;
    }

    private static void ApplyEnvironment(Material nightSkybox)
    {
        RenderSettings.skybox = nightSkybox;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.065f, 0.072f, 0.092f, 1f);
        RenderSettings.reflectionIntensity = 0.07f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.fog = false;
        RenderSettings.subtractiveShadowColor = new Color(0.04f, 0.05f, 0.08f, 1f);
    }

    private static void ConfigureMoonLight(Light moonLight)
    {
        Undo.RecordObject(moonLight, "Configure moon light");
        moonLight.color = new Color(0.48f, 0.53f, 0.64f, 1f);
        moonLight.intensity = 0.18f;
        moonLight.shadows = LightShadows.None;
        moonLight.bounceIntensity = 0f;
        moonLight.renderMode = LightRenderMode.Auto;
        RenderSettings.sun = moonLight;
    }

    private static void ConfigureCarHeadlights(CarVehicleController controller)
    {
        Transform vehicleRoot = controller.transform;
        Transform headlightRoot = GetOrCreateChild(vehicleRoot, HeadlightRootName);
        headlightRoot.localPosition = Vector3.zero;
        headlightRoot.localRotation = Quaternion.identity;
        headlightRoot.localScale = Vector3.one;

        Bounds localBounds = CalculateRendererBoundsLocal(vehicleRoot, FindTransformByPath(CarVisualPath));
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        float xOffset = Mathf.Max(0.28f, extents.x * 0.55f);
        float yPosition = Mathf.Max(center.y + extents.y * 0.12f, localBounds.min.y + extents.y * 0.55f);
        float zPosition = localBounds.max.z - Mathf.Max(0.05f, extents.z * 0.06f);
        Vector3 leftLocalPosition = new Vector3(center.x - xOffset, yPosition, zPosition);
        Vector3 rightLocalPosition = new Vector3(center.x + xOffset, yPosition, zPosition);

        Light leftLight = GetOrCreateSpotLight(headlightRoot, LeftHeadlightName, leftLocalPosition);
        Light rightLight = GetOrCreateSpotLight(headlightRoot, RightHeadlightName, rightLocalPosition);
        controller.headlights = new[] { leftLight, rightLight };
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureStreetLights()
    {
        Transform lampRoot = FindTransformByPath(LampRootPath);
        if (lampRoot == null)
        {
            Debug.LogWarning("CarSceneNightLightingSetup: Could not find the lamp root.");
            return;
        }

        Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        for (int i = 0; i < lampRoot.childCount; i++)
        {
            Transform child = lampRoot.GetChild(i);
            if (!child.name.StartsWith("StreetFixture_"))
            {
                continue;
            }

            bool enablePhysicalLight = ShouldEnableRoadLight(child.name);
            Color fixtureEmission = enablePhysicalLight
                ? new Color(0.7f, 0.5f, 0.26f, 1f)
                : new Color(0.018f, 0.012f, 0.006f, 1f);

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material[] sharedMaterials = renderer.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < sharedMaterials.Length; m++)
                {
                    Material sourceMaterial = sharedMaterials[m];
                    if (sourceMaterial == null)
                    {
                        continue;
                    }

                    string cacheKey = sourceMaterial.name + (enablePhysicalLight ? "_Lit" : "_Dim");
                    if (!materialCache.TryGetValue(cacheKey, out Material configuredMaterial))
                    {
                        configuredMaterial = GetOrCreateMaterialVariant(sourceMaterial, StreetLightFolder, enablePhysicalLight ? "NightLit" : "NightDim");
                        ConfigureEmissionMaterial(configuredMaterial, fixtureEmission);
                        materialCache[cacheKey] = configuredMaterial;
                    }

                    if (sharedMaterials[m] != configuredMaterial)
                    {
                        sharedMaterials[m] = configuredMaterial;
                        changed = true;
                    }
                }

                if (changed)
                {
                    Undo.RecordObject(renderer, "Assign street light night material");
                    renderer.sharedMaterials = sharedMaterials;
                    EditorUtility.SetDirty(renderer);
                }
            }

            Light lampLight = GetOrCreatePointLight(child, StreetLampLightChildName);
            lampLight.enabled = enablePhysicalLight;
            lampLight.intensity = 2.4f;
            lampLight.range = 16f;
            lampLight.color = new Color(1f, 0.83f, 0.6f, 1f);
            lampLight.shadows = LightShadows.Soft;
            lampLight.shadowStrength = 0.78f;
            lampLight.renderMode = LightRenderMode.ForcePixel;
            lampLight.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            lampLight.transform.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(lampLight);
        }
    }

    private static void ConfigureNatureMaterials()
    {
        ConfigureNatureMaterial(SimpleNatureBackgroundMaterialPath, new Color(0.22f, 0.24f, 0.21f, 1f));
        ConfigureNatureMaterial(SimpleNatureTextureMaterialPath, new Color(0.28f, 0.31f, 0.23f, 1f));
    }

    private static void ConfigureNatureMaterial(string materialPath, Color tint)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Debug.LogWarning("CarSceneNightLightingSetup: Could not find nature material at " + materialPath);
            return;
        }

        Undo.RecordObject(material, "Configure night nature material");

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        if (material.HasProperty("_GlossyReflections"))
        {
            material.SetFloat("_GlossyReflections", 0f);
        }

        if (material.HasProperty("_SpecularHighlights"))
        {
            material.SetFloat("_SpecularHighlights", 0f);
        }

        EditorUtility.SetDirty(material);
    }

    private static void HideLightSceneIcons()
    {
        try
        {
            System.Type annotationUtilityType = typeof(Editor).Assembly.GetType("UnityEditor.AnnotationUtility");
            if (annotationUtilityType == null)
            {
                return;
            }

            MethodInfo setIconEnabled = annotationUtilityType.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (setIconEnabled != null)
            {
                setIconEnabled.Invoke(null, new object[] { LightClassId, string.Empty, 0 });
            }

            MethodInfo setGizmoEnabled = annotationUtilityType.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (setGizmoEnabled != null)
            {
                setGizmoEnabled.Invoke(null, new object[] { LightClassId, string.Empty, 0, false });
            }

            SceneView.RepaintAll();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CarSceneNightLightingSetup: Could not hide light scene icons. " + ex.Message);
        }
    }

    private static void ConfigureCitySkyline()
    {
        Transform cityRoot = FindTransformByPath(CityRootPath);
        if (cityRoot == null)
        {
            Debug.LogWarning("CarSceneNightLightingSetup: Could not find the city root.");
            return;
        }

        Dictionary<string, Material> tierMaterialCache = new Dictionary<string, Material>();

        for (int i = 0; i < cityRoot.childCount; i++)
        {
            Transform building = cityRoot.GetChild(i);
            SkylineTier tier = GetSkylineTier(building.name);
            MeshRenderer[] renderers = building.GetComponentsInChildren<MeshRenderer>(true);
            bool hasUsableWindowRenderers = HasUsableWindowRenderers(building, renderers);

            for (int r = 0; r < renderers.Length; r++)
            {
                MeshRenderer renderer = renderers[r];
                if (IsDetachedWindowRenderer(building, renderer))
                {
                    SetRendererEnabled(renderer, false);
                    continue;
                }

                SetRendererEnabled(renderer, true);

                if (!ShouldProcessSkylineRenderer(building, renderer, hasUsableWindowRenderers))
                {
                    continue;
                }

                Material[] sharedMaterials = renderer.sharedMaterials;
                bool changed = false;

                for (int m = 0; m < sharedMaterials.Length; m++)
                {
                    Material sourceMaterial = sharedMaterials[m];
                    if (sourceMaterial == null)
                    {
                        continue;
                    }

                    string cacheKey = sourceMaterial.name + "_" + tier;
                    if (!tierMaterialCache.TryGetValue(cacheKey, out Material nightWindowMaterial))
                    {
                        nightWindowMaterial = GetOrCreateMaterialVariant(sourceMaterial, CityWindowFolder, tier.ToString());
                        ConfigureEmissionMaterial(nightWindowMaterial, GetSkylineEmissionColor(tier));
                        tierMaterialCache[cacheKey] = nightWindowMaterial;
                    }

                    if (sharedMaterials[m] != nightWindowMaterial)
                    {
                        sharedMaterials[m] = nightWindowMaterial;
                        changed = true;
                    }
                }

                if (changed)
                {
                    Undo.RecordObject(renderer, "Assign skyline night window material");
                    renderer.sharedMaterials = sharedMaterials;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }
    }

    private static bool HasUsableWindowRenderers(Transform building, MeshRenderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (IsWindowRenderer(renderer) && !IsDetachedWindowRenderer(building, renderer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldProcessSkylineRenderer(Transform building, MeshRenderer renderer, bool hasUsableWindowRenderers)
    {
        if (renderer == null)
        {
            return false;
        }

        if (renderer.transform == building)
        {
            return !hasUsableWindowRenderers;
        }

        return IsWindowRenderer(renderer) && !IsDetachedWindowRenderer(building, renderer);
    }

    private static bool IsWindowRenderer(MeshRenderer renderer)
    {
        return renderer != null && renderer.gameObject.name.StartsWith("Win_");
    }

    private static bool IsDetachedWindowRenderer(Transform building, MeshRenderer renderer)
    {
        if (!IsWindowRenderer(renderer))
        {
            return false;
        }

        return renderer.transform.localPosition.sqrMagnitude > 400f;
    }

    private static void SetRendererEnabled(Renderer renderer, bool isEnabled)
    {
        if (renderer == null || renderer.enabled == isEnabled)
        {
            return;
        }

        Undo.RecordObject(renderer, isEnabled ? "Enable skyline renderer" : "Disable detached skyline renderer");
        renderer.enabled = isEnabled;
        EditorUtility.SetDirty(renderer);
    }

    private static SkylineTier GetSkylineTier(string buildingName)
    {
        if (HighSkylineBuildings.Contains(buildingName))
        {
            return SkylineTier.High;
        }

        if (MidSkylineBuildings.Contains(buildingName))
        {
            return SkylineTier.Mid;
        }

        if (LowSkylineBuildings.Contains(buildingName))
        {
            return SkylineTier.Low;
        }

        return SkylineTier.Low;
    }

    private static Color GetSkylineEmissionColor(SkylineTier tier)
    {
        switch (tier)
        {
            case SkylineTier.High:
                return new Color(4.2f, 3.35f, 1.95f, 1f);
            case SkylineTier.Mid:
                return new Color(2.45f, 1.9f, 1.08f, 1f);
            default:
                return new Color(1.28f, 0.96f, 0.54f, 1f);
        }
    }

    private static bool ShouldEnableRoadLight(string fixtureName)
    {
        if (!TryGetFixtureIndex(fixtureName, out int fixtureIndex))
        {
            return false;
        }

        return fixtureIndex % 3 == 2;
    }

    private static bool TryGetFixtureIndex(string fixtureName, out int fixtureIndex)
    {
        fixtureIndex = -1;
        int lastUnderscoreIndex = fixtureName.LastIndexOf('_');
        if (lastUnderscoreIndex < 0 || lastUnderscoreIndex >= fixtureName.Length - 1)
        {
            return false;
        }

        return int.TryParse(fixtureName.Substring(lastUnderscoreIndex + 1), out fixtureIndex);
    }

    private static void ConfigureEmissionMaterial(Material material, Color emissionColor)
    {
        if (material == null)
        {
            return;
        }

        bool emissionIsBlack = emissionColor.maxColorComponent <= 0.0001f;

        if (material.HasProperty("_EmissionColor"))
        {
            if (emissionIsBlack)
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            else
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
                material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        if (material.HasProperty("emissiveFactor"))
        {
            material.SetColor("emissiveFactor", emissionIsBlack ? Color.black : emissionColor);
        }

        EditorUtility.SetDirty(material);
    }

    private static Material GetOrCreateMaterialVariant(Material sourceMaterial, string folderPath, string suffix)
    {
        if (sourceMaterial == null)
        {
            return null;
        }

        EnsureFolder(folderPath);
        string sourceName = sourceMaterial.name;
        string sourceAssetPath = AssetDatabase.GetAssetPath(sourceMaterial);
        string repeatedSuffix = "_" + suffix;
        if (!string.IsNullOrEmpty(sourceAssetPath) && sourceAssetPath.StartsWith(folderPath + "/"))
        {
            while (sourceName.EndsWith(repeatedSuffix))
            {
                sourceName = sourceName.Substring(0, sourceName.Length - repeatedSuffix.Length);
            }
        }
        string fileName = SanitizeFileName(sourceName + "_" + suffix + ".mat");
        string assetPath = folderPath + "/" + fileName;
        Material variant = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        if (variant == null)
        {
            variant = new Material(sourceMaterial);
            AssetDatabase.CreateAsset(variant, assetPath);
        }
        else
        {
            EditorUtility.CopySerialized(sourceMaterial, variant);
        }

        EditorUtility.SetDirty(variant);
        return variant;
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return;
        }

        string parentPath = Path.GetDirectoryName(assetFolderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(assetFolderPath);
        if (!string.IsNullOrEmpty(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
        {
            EnsureFolder(parentPath);
        }

        AssetDatabase.CreateFolder(parentPath, folderName);
    }

    private static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
        {
            name = name.Replace(invalid[i], '_');
        }

        return name.Replace(' ', '_');
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, "Create " + childName);
        child = childObject.transform;
        child.SetParent(parent, false);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    private static Light GetOrCreateSpotLight(Transform parent, string childName, Vector3 localPosition)
    {
        Transform child = GetOrCreateChild(parent, childName);
        child.localPosition = localPosition;
        child.localRotation = Quaternion.Euler(8f, 0f, 0f);
        child.localScale = Vector3.one;

        Light light = child.GetComponent<Light>();
        if (light == null)
        {
            light = Undo.AddComponent<Light>(child.gameObject);
        }

        light.type = LightType.Spot;
        light.color = new Color(1f, 0.94f, 0.84f, 1f);
        light.intensity = 6.25f;
        light.range = 24f;
        light.spotAngle = 68f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.enabled = false;
        EditorUtility.SetDirty(light);
        return light;
    }

    private static Light GetOrCreatePointLight(Transform parent, string childName)
    {
        Transform child = GetOrCreateChild(parent, childName);
        Light light = child.GetComponent<Light>();
        if (light == null)
        {
            light = Undo.AddComponent<Light>(child.gameObject);
        }

        light.type = LightType.Point;
        return light;
    }

    private static Bounds CalculateRendererBoundsLocal(Transform root, Transform targetRoot)
    {
        Renderer[] renderers = targetRoot != null ? targetRoot.GetComponentsInChildren<Renderer>(true) : root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 extents = worldBounds.extents;
            Vector3 center = worldBounds.center;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 localCorner = root.InverseTransformPoint(worldCorner);
                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        if (!hasBounds)
        {
            localBounds = new Bounds(new Vector3(0f, 0.5f, 1.8f), new Vector3(1.6f, 1.2f, 3.6f));
        }

        return localBounds;
    }

    private static T FindComponentByPath<T>(string path) where T : Component
    {
        Transform target = FindTransformByPath(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindTransformByPath(string path)
    {
        string[] parts = path.Split('/');
        if (parts.Length == 0)
        {
            return null;
        }

        GameObject rootObject = GameObject.Find(parts[0]);
        if (rootObject == null)
        {
            return null;
        }

        Transform current = rootObject.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }
}








