using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

public class FireflyCatchBoxController : UdonSharpBehaviour
{
    [SerializeField] private FireflyCatchBoxSpawner spawner;
    [SerializeField] private GameObject boxVisual;
    [SerializeField] private Transform capturePoint;
    [SerializeField] private GameObject[] railTargets;
    [SerializeField] private GameObject[] containedFireflies;
    [SerializeField] private float captureRadius = 0.45f;
    [SerializeField] private float captureCooldown = 0.25f;
    [SerializeField] private float containedBobAmplitude = 0.006f;
    [SerializeField] private float containedBobSpeed = 1.2f;
    [SerializeField] private float containedYawAmplitude = 2f;

    [UdonSynced] private int capturedMask;

    private VRCPickup pickup;
    private Collider hitbox;
    private Rigidbody rb;
    private Transform[] containedTransforms;
    private Vector3[] containedRestPositions;
    private Quaternion[] containedRestRotations;
    private float[] containedPhases;
    private bool cacheReady;
    private bool isHeld;
    private float nextCaptureTime;

    private void Start()
    {
        EnsureCache();
        ApplyLocalState();
    }

    public override void OnPickup()
    {
        isHeld = true;
        Debug.Log("[CatchBox] Picked up CatchBox");
    }

    public override void OnDrop()
    {
        isHeld = false;
        Debug.Log("[CatchBox] Dropped CatchBox");
    }

    public override void OnPickupUseDown()
    {
        if (!isHeld || Time.time < nextCaptureTime)
        {
            return;
        }

        int nearestIndex = FindNearestFireflyNearCapturePoint();
        if (nearestIndex < 0)
        {
            Debug.Log("[CatchBox] Use candidate: none");
            return;
        }

        string targetName = Utilities.IsValid(railTargets[nearestIndex]) ? railTargets[nearestIndex].name : $"firefly#{nearestIndex}";
        Debug.Log($"[CatchBox] Use candidate: {targetName}");

        if (Utilities.IsValid(spawner) && spawner.IsFireflyCaptured(nearestIndex))
        {
            Debug.Log($"[CatchBox] {targetName} already captured");
            return;
        }

        Debug.Log($"[CatchBox] Capturing {targetName}");

        if (Utilities.IsValid(spawner))
        {
            spawner.TryCapture(nearestIndex);
        }

        int bit = 1 << nearestIndex;
        capturedMask |= bit;
        RequestSerialization();
        ApplyLocalState();

        nextCaptureTime = Time.time + captureCooldown;
    }

    public override void OnDeserialization()
    {
        ApplyLocalState();
    }

    private void ApplyLocalState()
    {
        EnsureCache();

        if (containedFireflies == null)
        {
            return;
        }

        for (int i = 0; i < containedFireflies.Length; i++)
        {
            GameObject firefly = containedFireflies[i];
            if (!Utilities.IsValid(firefly))
            {
                continue;
            }

            bool shouldShow = (capturedMask & (1 << i)) != 0;
            if (firefly.activeSelf != shouldShow)
            {
                firefly.SetActive(shouldShow);
            }
        }
    }

    private void Update()
    {
        if (containedTransforms == null)
        {
            return;
        }

        for (int i = 0; i < containedTransforms.Length; i++)
        {
            Transform firefly = containedTransforms[i];
            if (!Utilities.IsValid(firefly) || !firefly.gameObject.activeInHierarchy)
            {
                continue;
            }

            float phase = (Time.time * containedBobSpeed) + containedPhases[i];
            Vector3 idleOffset = new Vector3(
                Mathf.Sin(phase * 0.55f) * 0.0035f,
                Mathf.Sin(phase) * containedBobAmplitude,
                Mathf.Cos(phase * 0.4f) * 0.003f);

            firefly.localPosition = containedRestPositions[i] + idleOffset;
            firefly.localRotation = containedRestRotations[i] * Quaternion.Euler(
                0f,
                Mathf.Sin(phase * 0.7f) * containedYawAmplitude,
                0f);
        }
    }

    private int FindNearestFireflyNearCapturePoint()
    {
        if (railTargets == null || !Utilities.IsValid(capturePoint))
        {
            return -1;
        }

        Vector3 point = capturePoint.position;
        float radiusSqr = captureRadius * captureRadius;
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;

        for (int i = 0; i < railTargets.Length; i++)
        {
            GameObject railTarget = railTargets[i];
            if (!Utilities.IsValid(railTarget) || !railTarget.activeInHierarchy)
            {
                continue;
            }

            Vector3 samplePosition = GetSamplePosition(railTarget.transform);
            float distance = (samplePosition - point).sqrMagnitude;
            if (distance > radiusSqr || distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestIndex = i;
        }

        return nearestIndex;
    }

    private Vector3 GetSamplePosition(Transform railTarget)
    {
        if (!Utilities.IsValid(railTarget))
        {
            return Vector3.zero;
        }

        int childCount = railTarget.childCount;
        Transform glowFallback = null;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = railTarget.GetChild(i);
            if (!Utilities.IsValid(child))
            {
                continue;
            }

            string childName = child.name;
            if (childName != null)
            {
                if (childName.IndexOf("Abdomen") >= 0)
                {
                    return child.position;
                }

                if (glowFallback == null && childName.IndexOf("GlowLight") >= 0)
                {
                    glowFallback = child;
                }
            }
        }

        if (Utilities.IsValid(glowFallback))
        {
            return glowFallback.position;
        }

        if (childCount > 0)
        {
            Transform firstChild = railTarget.GetChild(0);
            if (Utilities.IsValid(firstChild))
            {
                return firstChild.position;
            }
        }

        return railTarget.position;
    }

    private void EnsureCache()
    {
        if (cacheReady)
        {
            return;
        }

        pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
        hitbox = (Collider)GetComponent(typeof(BoxCollider));
        rb = (Rigidbody)GetComponent(typeof(Rigidbody));

        if (containedFireflies == null)
        {
            cacheReady = true;
            return;
        }

        int count = containedFireflies.Length;
        containedTransforms = new Transform[count];
        containedRestPositions = new Vector3[count];
        containedRestRotations = new Quaternion[count];
        containedPhases = new float[count];

        for (int i = 0; i < count; i++)
        {
            GameObject firefly = containedFireflies[i];
            if (!Utilities.IsValid(firefly))
            {
                continue;
            }

            Transform fireflyTransform = firefly.transform;
            containedTransforms[i] = fireflyTransform;
            containedRestPositions[i] = fireflyTransform.localPosition;
            containedRestRotations[i] = fireflyTransform.localRotation;
            containedPhases[i] = i * 0.87f;
        }

        cacheReady = true;
    }
}
