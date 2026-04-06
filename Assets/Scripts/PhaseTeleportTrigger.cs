using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

public class PhaseTeleportTrigger : UdonSharpBehaviour
{
    [SerializeField] private Transform destination;
    [FormerlySerializedAs("phase2Skybox")]
    [SerializeField] private Material targetSkybox;
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

        if (targetSkybox != null)
            RenderSettings.skybox = targetSkybox;
    }
}
