using UnityEngine;

public class FireflySystemBuilder : MonoBehaviour
{
    private static readonly Color FireflyColor = new Color(170f / 255f, 1f, 68f / 255f, 1f);

    [ContextMenu("Rebuild Firefly System")]
    private void RebuildFromContextMenu()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        var lightTransform = transform.Find("FireflyLightTemplate");
        GameObject lightObject;
        if (lightTransform == null)
        {
            lightObject = new GameObject("FireflyLightTemplate");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, -1000f, 0f);
        }
        else
        {
            lightObject = lightTransform.gameObject;
        }

        var templateLight = lightObject.GetComponent<Light>();
        if (templateLight == null)
        {
            templateLight = lightObject.AddComponent<Light>();
        }
        templateLight.type = LightType.Point;
        templateLight.color = FireflyColor;
        templateLight.range = 1.5f;
        templateLight.intensity = 0.6f;
        templateLight.shadows = LightShadows.None;
        templateLight.enabled = false;

        SetupSystem("HillsideFireflies", new Vector3(742f, 17f, 607f), 200, 40f, new Vector3(60f, 30f, 40f), 3f, 6f, 0.1f, 0.3f, 0.08f, 0.15f, 0.35f, 0.9f, templateLight);
        SetupSystem("WaterReflectionFireflies", new Vector3(666f, 7.5f, 600f), 80, 40f, new Vector3(60f, 8f, 40f), 3f, 6f, 0.1f, 0.3f, 0.04f, 0.08f, 0.15f, 0.45f, templateLight);
    }

    private void SetupSystem(string childName, Vector3 worldPosition, int maxParticles, float emissionRate, Vector3 boxSize, float lifetimeMin, float lifetimeMax, float speedMin, float speedMax, float sizeMin, float sizeMax, float alphaMin, float alphaMax, Light templateLight)
    {
        var child = transform.Find(childName);
        GameObject go;
        if (child == null)
        {
            go = new GameObject(childName);
            go.transform.SetParent(transform, false);
        }
        else
        {
            go = child.gameObject;
        }

        go.transform.position = worldPosition;

        var particleSystem = go.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            particleSystem = go.AddComponent<ParticleSystem>();
        }

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.allowRoll = false;

        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(FireflyColor.r, FireflyColor.g, FireflyColor.b, alphaMin),
            new Color(FireflyColor.r, FireflyColor.g, FireflyColor.b, alphaMax));

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;
        shape.position = Vector3.zero;

        var velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.03f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

        var noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.15f;
        noise.scrollSpeed = 0.05f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var lights = particleSystem.lights;
        lights.enabled = true;
        lights.light = templateLight;
        lights.useParticleColor = true;
        lights.alphaAffectsIntensity = true;
        lights.ratio = 1f;
        lights.maxLights = maxParticles;
        lights.useRandomDistribution = true;
        lights.rangeMultiplier = 1f;
        lights.intensityMultiplier = 1f;

        if (!particleSystem.isPlaying)
        {
            particleSystem.Play(true);
        }
    }
}
