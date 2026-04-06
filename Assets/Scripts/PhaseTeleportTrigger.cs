using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PhaseTeleportTrigger : UdonSharpBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private Material phase2Skybox;
    [SerializeField] private bool oneShot = true;

    private bool triggered;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player) || !player.isLocal || !Utilities.IsValid(destination))
            return;

        if (oneShot && triggered)
            return;

        triggered = true;
        player.TeleportTo(destination.position, destination.rotation);

        if (phase2Skybox != null)
            RenderSettings.skybox = phase2Skybox;
    }
}
