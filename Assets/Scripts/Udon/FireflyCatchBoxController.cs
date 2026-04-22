using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

public class FireflyCatchBoxController : UdonSharpBehaviour
{
    [SerializeField] private FireflyCatchBoxSpawner spawner;
    [SerializeField] private GameObject boxVisual;
    [SerializeField] private Collider hitboxOverride;
    [SerializeField] private Transform capturePoint;
    [SerializeField] private GameObject[] railTargets;
    [SerializeField] private GameObject[] containedFireflies;
    [SerializeField] private int maxContainedFireflies = 4;
    [SerializeField] private float captureRadius = 0.45f;
    [SerializeField] private float captureCooldown = 0.25f;
    [SerializeField] private float containedBobAmplitude = 0.006f;
    [SerializeField] private float containedBobSpeed = 1.2f;
    [SerializeField] private float containedYawAmplitude = 2f;

    [UdonSynced] private int slot0RailIndex = -1;
    [UdonSynced] private int slot1RailIndex = -1;
    [UdonSynced] private int slot2RailIndex = -1;
    [UdonSynced] private int slot3RailIndex = -1;

    private Transform[] containedTransforms;
    private Vector3[] slotRestPositions;
    private Quaternion[] slotRestRotations;
    private float[] slotPhases;
    private VRCPickup pickup;
    private Collider hitbox;
    private bool cacheReady;
    private bool hasLocalAuthorityConfigured;
    private bool warnedAboutContainedSlots;
    private bool isHeld;
    private float nextCaptureTime;

    private void Start()
    {
        CacheInteractionReferences();
        ApplyOwnerInteractionState();
        EnsureCache();
        ApplyLocalState();
    }

    public override void OnPickup()
    {
        ApplyOwnerInteractionState();
        if (!IsLocalInteractiveOwner())
        {
            return;
        }

        isHeld = true;
        Debug.Log("[CatchBox] Picked up CatchBox");
    }

    public override void OnDrop()
    {
        ApplyOwnerInteractionState();
        isHeld = false;
        Debug.Log("[CatchBox] Dropped CatchBox");
    }

    public override void OnPickupUseDown()
    {
        if (!IsLocalInteractiveOwner() || !isHeld || Time.time < nextCaptureTime)
        {
            return;
        }

        int usableContainedSlots = GetUsableContainedSlotCount();
        if (usableContainedSlots <= 0)
        {
            Debug.LogWarning("[CatchBox] No contained firefly slots are configured");
            return;
        }

        int freeSlot = FindFirstEmptySlot();
        if (freeSlot < 0 || freeSlot >= usableContainedSlots)
        {
            Debug.Log("[CatchBox] Box is full");
            return;
        }

        int nearestIndex = FindNearestFireflyNearCapturePoint();
        if (nearestIndex < 0)
        {
            Debug.Log("[CatchBox] Use candidate: none");
            return;
        }

        GameObject[] activeRailTargets = GetActiveRailTargets();
        string targetName = Utilities.IsValid(activeRailTargets[nearestIndex]) ? activeRailTargets[nearestIndex].name : $"firefly#{nearestIndex}";
        Debug.Log($"[CatchBox] Use candidate: {targetName}");

        if (nearestIndex >= containedFireflies.Length)
        {
            Debug.LogWarning($"[CatchBox] No contained identity is configured for {targetName}");
            return;
        }

        SetSlotRailIndex(freeSlot, nearestIndex);
        RequestSerialization();
        ApplyLocalState();

        Debug.Log($"[CatchBox] Capturing {targetName} into slot {freeSlot}");
        nextCaptureTime = Time.time + captureCooldown;
    }

    public override void OnDeserialization()
    {
        ApplyLocalState();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        ApplyOwnerInteractionState();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        ApplyOwnerInteractionState();
    }

    private void ApplyLocalState()
    {
        EnsureCache();

        if (containedFireflies == null)
        {
            return;
        }

        int identityCount = containedFireflies.Length;
        for (int identityIndex = 0; identityIndex < identityCount; identityIndex++)
        {
            GameObject firefly = containedFireflies[identityIndex];
            if (!Utilities.IsValid(firefly))
            {
                continue;
            }

            int slotIndex = FindSlotForRailIndex(identityIndex);
            bool shouldShow = slotIndex >= 0;
            if (firefly.activeSelf != shouldShow)
            {
                firefly.SetActive(shouldShow);
            }

            if (!shouldShow || slotIndex >= slotRestPositions.Length)
            {
                continue;
            }

            Transform fireflyTransform = firefly.transform;
            fireflyTransform.localPosition = slotRestPositions[slotIndex];
            fireflyTransform.localRotation = slotRestRotations[slotIndex];
        }
    }

    private void Update()
    {
        if (!hasLocalAuthorityConfigured && Utilities.IsValid(Networking.LocalPlayer))
        {
            ApplyOwnerInteractionState();
        }

        if (containedTransforms == null || slotRestPositions == null)
        {
            return;
        }

        int slotCount = Mathf.Min(GetUsableContainedSlotCount(), slotRestPositions.Length);
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            int railIndex = GetSlotRailIndex(slotIndex);
            if (railIndex < 0 || railIndex >= containedTransforms.Length)
            {
                continue;
            }

            Transform firefly = containedTransforms[railIndex];
            if (!Utilities.IsValid(firefly) || !firefly.gameObject.activeInHierarchy)
            {
                continue;
            }

            float phase = (Time.time * containedBobSpeed) + slotPhases[slotIndex];
            Vector3 idleOffset = new Vector3(
                Mathf.Sin(phase * 0.55f) * 0.0035f,
                Mathf.Sin(phase) * containedBobAmplitude,
                Mathf.Cos(phase * 0.4f) * 0.003f);

            firefly.localPosition = slotRestPositions[slotIndex] + idleOffset;
            firefly.localRotation = slotRestRotations[slotIndex] * Quaternion.Euler(
                0f,
                Mathf.Sin(phase * 0.7f) * containedYawAmplitude,
                0f);
        }
    }

    private int FindNearestFireflyNearCapturePoint()
    {
        GameObject[] activeRailTargets = GetActiveRailTargets();
        if (activeRailTargets == null || !Utilities.IsValid(capturePoint))
        {
            return -1;
        }

        Vector3 point = capturePoint.position;
        float radiusSqr = captureRadius * captureRadius;
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;

        for (int i = 0; i < activeRailTargets.Length; i++)
        {
            GameObject railTarget = activeRailTargets[i];
            if (!Utilities.IsValid(railTarget) || !railTarget.activeInHierarchy)
            {
                continue;
            }

            if (HasCapturedRailIndex(i))
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

    private void CacheInteractionReferences()
    {
        if (!Utilities.IsValid(pickup))
        {
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
        }

        if (!Utilities.IsValid(hitbox))
        {
            hitbox = Utilities.IsValid(hitboxOverride) ? hitboxOverride : (Collider)GetComponent(typeof(Collider));
        }
    }

    private void ApplyOwnerInteractionState()
    {
        CacheInteractionReferences();

        bool isLocalInteractive = IsLocalInteractiveOwner();
        if (!isLocalInteractive)
        {
            isHeld = false;
        }

        if (Utilities.IsValid(pickup))
        {
            pickup.enabled = isLocalInteractive;
        }

        if (Utilities.IsValid(hitbox))
        {
            hitbox.enabled = isLocalInteractive;
        }

        hasLocalAuthorityConfigured = Utilities.IsValid(Networking.LocalPlayer);
    }

    private bool IsLocalInteractiveOwner()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        return Utilities.IsValid(localPlayer) && Networking.IsOwner(gameObject);
    }

    private GameObject[] GetActiveRailTargets()
    {
        if (Utilities.IsValid(spawner))
        {
            GameObject[] sharedRailTargets = spawner.GetRailTargets();
            if (sharedRailTargets != null && sharedRailTargets.Length > 0)
            {
                return sharedRailTargets;
            }
        }

        return railTargets;
    }

    private int GetUsableContainedSlotCount()
    {
        int configuredSlots = containedFireflies == null ? 0 : containedFireflies.Length;
        int usableSlots = Mathf.Min(maxContainedFireflies, configuredSlots);

        if (!warnedAboutContainedSlots && configuredSlots < maxContainedFireflies)
        {
            Debug.LogWarning($"[CatchBox] Configured containedFireflies slots ({configuredSlots}) are fewer than maxContainedFireflies ({maxContainedFireflies})");
            warnedAboutContainedSlots = true;
        }

        return usableSlots;
    }

    private int FindFirstEmptySlot()
    {
        int slotCount = GetUsableContainedSlotCount();
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            if (GetSlotRailIndex(slotIndex) < 0)
            {
                return slotIndex;
            }
        }

        return -1;
    }

    private bool HasCapturedRailIndex(int railIndex)
    {
        return FindSlotForRailIndex(railIndex) >= 0;
    }

    private int FindSlotForRailIndex(int railIndex)
    {
        int slotCount = GetUsableContainedSlotCount();
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            if (GetSlotRailIndex(slotIndex) == railIndex)
            {
                return slotIndex;
            }
        }

        return -1;
    }

    private int GetSlotRailIndex(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0:
                return slot0RailIndex;
            case 1:
                return slot1RailIndex;
            case 2:
                return slot2RailIndex;
            case 3:
                return slot3RailIndex;
            default:
                return -1;
        }
    }

    private void SetSlotRailIndex(int slotIndex, int railIndex)
    {
        switch (slotIndex)
        {
            case 0:
                slot0RailIndex = railIndex;
                return;
            case 1:
                slot1RailIndex = railIndex;
                return;
            case 2:
                slot2RailIndex = railIndex;
                return;
            case 3:
                slot3RailIndex = railIndex;
                return;
        }
    }

    private void EnsureCache()
    {
        if (cacheReady)
        {
            return;
        }

        if (containedFireflies == null)
        {
            cacheReady = true;
            return;
        }

        int identityCount = containedFireflies.Length;
        containedTransforms = new Transform[identityCount];

        for (int i = 0; i < identityCount; i++)
        {
            GameObject firefly = containedFireflies[i];
            if (!Utilities.IsValid(firefly))
            {
                continue;
            }

            containedTransforms[i] = firefly.transform;
        }

        int slotCount = GetUsableContainedSlotCount();
        slotRestPositions = new Vector3[slotCount];
        slotRestRotations = new Quaternion[slotCount];
        slotPhases = new float[slotCount];

        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            Transform slotTransform = containedTransforms[slotIndex];
            if (!Utilities.IsValid(slotTransform))
            {
                continue;
            }

            slotRestPositions[slotIndex] = slotTransform.localPosition;
            slotRestRotations[slotIndex] = slotTransform.localRotation;
            slotPhases[slotIndex] = slotIndex * 0.87f;
        }

        cacheReady = true;
    }
}
