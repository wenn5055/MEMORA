using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureFireflyAbdomenParticle
{
    private const string AbdomenPath = "fireflies_notrees/RailingFirefliesDetail/Firefly_01/Abdomen";

    [MenuItem("Tools/Fireflies/Configure Firefly 01 Abdomen Glow")]
    public static void Apply()
    {
        GameObject abdomen = GameObject.Find(AbdomenPath);
        if (abdomen == null)
        {
            Debug.LogError($"Could not find '{AbdomenPath}' in the active scene.");
            return;
        }

        ParticleSystem particleSystem = abdomen.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            Debug.LogError($"'{AbdomenPath}' does not have a ParticleSystem.");
            return;
        }

        Undo.RecordObject(particleSystem, "Configure firefly abdomen particle");

        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.028f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 1f, 0.68f, 0.45f));
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f);

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.002f;
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
            Undo.RecordObject(renderer, "Configure firefly abdomen renderer");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.lengthScale = 1f;
            renderer.velocityScale = 0f;
            renderer.cameraVelocityScale = 0f;
            renderer.maxParticleSize = 0.12f;
            renderer.minParticleSize = 0f;
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            EditorUtility.SetDirty(renderer);
        }

        particleSystem.Play(true);
        EditorUtility.SetDirty(particleSystem);
        EditorUtility.SetDirty(abdomen);
        EditorSceneManager.MarkSceneDirty(abdomen.scene);

        Debug.Log("Configured Firefly_01 abdomen particle glow.");
    }

    private static Gradient BuildGlowGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.78f, 1f, 0.34f), 0f),
                new GradientColorKey(new Color(1f, 0.98f, 0.7f), 0.45f),
                new GradientColorKey(new Color(0.74f, 1f, 0.3f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.12f, 0f),
                new GradientAlphaKey(0.55f, 0.22f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0.45f, 0.8f),
                new GradientAlphaKey(0.1f, 1f),
            });
        return gradient;
    }

    private static AnimationCurve BuildSizeCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.72f),
            new Keyframe(0.32f, 1.02f),
            new Keyframe(0.52f, 1.18f),
            new Keyframe(0.82f, 0.94f),
            new Keyframe(1f, 0.68f));
    }
}
