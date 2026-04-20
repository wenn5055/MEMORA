using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneFirefly01Visuals
{
    private const string FireflyRootPath = "fireflies_notrees/RailingFirefliesDetail/Firefly_01";
    private const string AbdomenPath = FireflyRootPath + "/Abdomen";
    private const string ModelPath = FireflyRootPath + "/Firefly";
    private const string MaterialPath = "Assets/Materials/FireflyModel_Transparent.mat";

    [MenuItem("Tools/Fireflies/Tune Firefly 01 Visuals")]
    public static void Apply()
    {
        GameObject abdomen = GameObject.Find(AbdomenPath);
        GameObject model = GameObject.Find(ModelPath);

        if (abdomen == null || model == null)
        {
            Debug.LogError("Could not find Firefly_01 abdomen/model objects in the active scene.");
            return;
        }

        ConfigureAbdomenParticle(abdomen);
        ConfigureModelMaterial(model);

        EditorSceneManager.MarkSceneDirty(abdomen.scene);
        Debug.Log("Tuned Firefly_01 abdomen glow and model transparency.");
    }

    private static void ConfigureAbdomenParticle(GameObject abdomen)
    {
        ParticleSystem particleSystem = abdomen.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            Debug.LogWarning("Abdomen has no ParticleSystem; skipping particle tuning.");
            return;
        }

        Undo.RecordObject(particleSystem, "Tune Firefly 01 abdomen particle");
        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.98f, 0.55f, 0.75f));
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f);

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.001f;
        shape.randomDirectionAmount = 0f;
        shape.sphericalDirectionAmount = 0f;
        shape.randomPositionAmount = 0f;

                var velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = false;

        var force = particleSystem.forceOverLifetime;
        force.enabled = false;

        var noise = particleSystem.noise;
        noise.enabled = false;

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildGlowGradient());

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, BuildSizeCurve());

        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Tune Firefly 01 abdomen particle renderer");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.lengthScale = 1f;
            renderer.velocityScale = 0f;
            renderer.cameraVelocityScale = 0f;
            renderer.maxParticleSize = 0.08f;
            renderer.minParticleSize = 0f;
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            EditorUtility.SetDirty(renderer);
        }

        particleSystem.Play(true);
        EditorUtility.SetDirty(particleSystem);
    }

    private static void ConfigureModelMaterial(GameObject model)
    {
        Renderer renderer = model.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("Firefly model has no renderer; skipping material tuning.");
            return;
        }

        Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (targetMaterial == null)
        {
            targetMaterial = new Material(Shader.Find("Standard"));
            targetMaterial.name = "FireflyModel_Transparent";
            AssetDatabase.CreateAsset(targetMaterial, MaterialPath);
        }

        targetMaterial.SetFloat("_Mode", 3f);
        targetMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        targetMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        targetMaterial.SetInt("_ZWrite", 0);
        targetMaterial.DisableKeyword("_ALPHATEST_ON");
        targetMaterial.EnableKeyword("_ALPHABLEND_ON");
        targetMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        targetMaterial.renderQueue = 3000;
        targetMaterial.color = new Color(0.92f, 0.98f, 0.75f, 0.58f);
        targetMaterial.SetColor("_EmissionColor", new Color(0.08f, 0.12f, 0.03f, 1f));
        targetMaterial.SetFloat("_Metallic", 0f);
        targetMaterial.SetFloat("_Glossiness", 0.18f);
        EditorUtility.SetDirty(targetMaterial);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(renderer, "Assign Firefly 01 transparent material");
        renderer.sharedMaterial = targetMaterial;
        EditorUtility.SetDirty(renderer);
    }

    private static Gradient BuildGlowGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.78f, 1f, 0.34f), 0f),
                new GradientColorKey(new Color(1f, 0.98f, 0.7f), 0.42f),
                new GradientColorKey(new Color(0.78f, 1f, 0.34f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.18f, 0f),
                new GradientAlphaKey(0.65f, 0.18f),
                new GradientAlphaKey(1f, 0.48f),
                new GradientAlphaKey(0.58f, 0.82f),
                new GradientAlphaKey(0.14f, 1f),
            });
        return gradient;
    }

    private static AnimationCurve BuildSizeCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.68f),
            new Keyframe(0.28f, 1.08f),
            new Keyframe(0.5f, 1.32f),
            new Keyframe(0.78f, 1.0f),
            new Keyframe(1f, 0.62f));
    }
}
