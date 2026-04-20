using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class RailingFirefliesController : UdonSharpBehaviour
{
    [SerializeField] private bool oneShot = true;
    [SerializeField] private float approachDuration = 1.65f;
    [SerializeField] private float staggerDelay = 0.18f;
    [SerializeField] private float perchBobAmplitude = 0.015f;
    [SerializeField] private float perchBobSpeed = 1.6f;
    [SerializeField] private float perchYawAmplitude = 3.5f;
    [SerializeField] private float perchRollAmplitude = 1.5f;
    [SerializeField] private Vector3 baseApproachOffset = new Vector3(1.35f, 0.7f, 0.45f);

    private Transform[] fireflies;
    private Vector3[] perchPositions;
    private Quaternion[] perchRotations;
    private Vector3[] startPositions;
    private float[] phases;
    private bool triggered;
    private float triggerTime;

    private void Start()
    {
        int count = transform.childCount;
        fireflies = new Transform[count];
        perchPositions = new Vector3[count];
        perchRotations = new Quaternion[count];
        startPositions = new Vector3[count];
        phases = new float[count];

        for (int i = 0; i < count; i++)
        {
            Transform firefly = transform.GetChild(i);
            fireflies[i] = firefly;
            perchPositions[i] = firefly.localPosition;
            perchRotations[i] = firefly.localRotation;

            float spread = (i - ((count - 1) * 0.5f)) * 0.18f;
            float vertical = baseApproachOffset.y + ((i % 2 == 0) ? 0.08f : -0.02f);
            float depth = baseApproachOffset.z + (i * 0.08f);
            startPositions[i] = firefly.localPosition + new Vector3(baseApproachOffset.x + spread, vertical, depth);
            phases[i] = i * 0.91f;

            firefly.localPosition = startPositions[i];
            firefly.localRotation = perchRotations[i] * Quaternion.Euler(0f, 18f, 0f);
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player) || !player.isLocal)
        {
            return;
        }

        if (oneShot && triggered)
        {
            return;
        }

        triggered = true;
        triggerTime = Time.time;
    }

    private void Update()
    {
        if (fireflies == null || fireflies.Length == 0 || !triggered)
        {
            return;
        }

        float elapsed = Time.time - triggerTime;

        for (int i = 0; i < fireflies.Length; i++)
        {
            Transform firefly = fireflies[i];
            if (!Utilities.IsValid(firefly) || !firefly.gameObject.activeInHierarchy)
            {
                continue;
            }

            float localElapsed = elapsed - (i * staggerDelay);
            if (localElapsed < 0f)
            {
                firefly.localPosition = startPositions[i];
                firefly.localRotation = perchRotations[i];
                continue;
            }

            float t = Mathf.Clamp01(localElapsed / approachDuration);
            t = t * t * (3f - (2f * t));

            Vector3 targetPosition = perchPositions[i];
            Quaternion targetRotation = perchRotations[i];

            if (t < 1f)
            {
                Vector3 arcOffset = new Vector3(0f, Mathf.Sin(t * Mathf.PI) * 0.18f, 0f);
                firefly.localPosition = Vector3.Lerp(startPositions[i], targetPosition, t) + arcOffset;
                firefly.localRotation = Quaternion.Slerp(targetRotation * Quaternion.Euler(0f, 18f, 0f), targetRotation, t);
                continue;
            }

            float phase = (Time.time * perchBobSpeed) + phases[i];
            Vector3 idleOffset = new Vector3(
                Mathf.Sin(phase * 0.7f) * 0.012f,
                Mathf.Sin(phase) * perchBobAmplitude,
                Mathf.Cos(phase * 0.5f) * 0.01f);

            firefly.localPosition = targetPosition + idleOffset;
            firefly.localRotation = targetRotation * Quaternion.Euler(
                Mathf.Sin(phase * 0.8f) * 2.5f,
                Mathf.Sin(phase * 0.55f) * perchYawAmplitude,
                Mathf.Cos(phase * 0.9f) * perchRollAmplitude);
        }
    }
}
