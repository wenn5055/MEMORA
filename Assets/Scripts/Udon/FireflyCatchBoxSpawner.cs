using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FireflyCatchBoxSpawner : UdonSharpBehaviour
{
    [SerializeField] private GameObject[] railTargets;

    [UdonSynced] [SerializeField] private int capturedMask;

    public bool IsFireflyCaptured(int fireflyIndex)
    {
        if (fireflyIndex < 0 || fireflyIndex >= 31)
        {
            return true;
        }

        return (capturedMask & (1 << fireflyIndex)) != 0;
    }

    public void TryCapture(int fireflyIndex)
    {
        if (fireflyIndex < 0 || fireflyIndex >= 31)
        {
            return;
        }

        if (!TakeOwnership())
        {
            return;
        }

        int bit = 1 << fireflyIndex;
        if ((capturedMask & bit) != 0)
        {
            return;
        }

        capturedMask |= bit;
        RequestSerialization();
        ApplyState();
    }

    public override void OnDeserialization()
    {
        ApplyState();
    }

    private bool TakeOwnership()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer))
        {
            return false;
        }

        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(localPlayer, gameObject);
        }

        return true;
    }

    private void ApplyState()
    {
        if (railTargets == null)
        {
            return;
        }

        for (int i = 0; i < railTargets.Length; i++)
        {
            GameObject railTarget = railTargets[i];
            if (!Utilities.IsValid(railTarget))
            {
                continue;
            }

            bool shouldBeVisible = (capturedMask & (1 << i)) == 0;
            if (railTarget.activeSelf != shouldBeVisible)
            {
                railTarget.SetActive(shouldBeVisible);
            }
        }
    }
}
