using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRChat/Car/Car Seat Handle Interactable")]
public class CarSeatHandleInteractable : UdonSharpBehaviour
{
    public CarSeatStation seatStation;
    public CarVehicleController vehicleController;
    public Collider interactionCollider;
    public Renderer indicatorRenderer;
    public Color enterColor = new Color(0.20f, 0.65f, 0.95f, 1f);
    public Color exitColor = new Color(0.95f, 0.75f, 0.20f, 1f);
    public Color occupiedColor = new Color(0.70f, 0.25f, 0.25f, 1f);
    public Color disabledColor = new Color(0.22f, 0.22f, 0.22f, 1f);

    private bool _lastCanUse;

    private void Start()
    {
        if (interactionCollider == null)
        {
            interactionCollider = (Collider)GetComponent(typeof(Collider));
        }

        if (indicatorRenderer == null)
        {
            indicatorRenderer = (Renderer)GetComponent(typeof(Renderer));
        }

        RefreshState();
    }

    private void Update()
    {
        RefreshState();
    }

    public override void Interact()
    {
        if (seatStation == null || vehicleController == null || seatStation.station == null)
        {
            return;
        }

        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer))
        {
            return;
        }

        if (vehicleController.CanLocalPlayerExitSeat(seatStation))
        {
            seatStation.station.ExitStation(localPlayer);
            return;
        }

        if (vehicleController.CanLocalPlayerEnterSeat(seatStation))
        {
            seatStation.station.UseStation(localPlayer);
        }
    }

    private void RefreshState()
    {
        bool canExit = vehicleController != null && vehicleController.CanLocalPlayerExitSeat(seatStation);
        bool canEnter = vehicleController != null && vehicleController.CanLocalPlayerEnterSeat(seatStation);
        bool canUse = canExit || canEnter;

        if (interactionCollider != null && _lastCanUse != canUse)
        {
            interactionCollider.enabled = canUse;
        }

        _lastCanUse = canUse;
        if (indicatorRenderer != null && indicatorRenderer.material != null)
        {
            Color color = disabledColor;
            if (canExit)
            {
                color = exitColor;
            }
            else if (canEnter)
            {
                color = enterColor;
            }
            else if (vehicleController != null && vehicleController.IsSeatOccupiedByOtherPlayer(seatStation))
            {
                color = occupiedColor;
            }

            indicatorRenderer.material.color = color;
        }
    }
}
