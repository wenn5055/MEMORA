using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRCStation = VRC.SDKBase.VRCStation;

public enum CarSeatRole
{
    Driver,
    Passenger,
}

[AddComponentMenu("VRChat/Car/Car Seat Station")]
public class CarSeatStation : UdonSharpBehaviour
{
    public CarSeatRole seatRole = CarSeatRole.Passenger;
    public CarVehicleController vehicleController;
    public Collider seatCollider;
    public VRCStation station;

    private bool _lastLocalAccess = true;
    private bool _localSeatOccupied;

    private void Start()
    {
        if (station == null)
        {
            station = (VRCStation)GetComponent(typeof(VRCStation));
        }

        if (seatCollider == null)
        {
            seatCollider = (Collider)GetComponent(typeof(Collider));
        }

        RefreshLocalAccess();
    }

    private void Update()
    {
        RefreshLocalAccess();
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        HandleSeatEntered(player);
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        HandleSeatExited(player);
    }

    public void LegacyStationEntered()
    {
        HandleSeatEntered(Networking.LocalPlayer);
    }

    public void LegacyStationExited()
    {
        HandleSeatExited(Networking.LocalPlayer);
    }

    public void ForceLocalExit()
    {
        if (station == null || Networking.LocalPlayer == null)
        {
            return;
        }

        station.ExitStation(Networking.LocalPlayer);
    }

    public bool IsDriverSeat()
    {
        return seatRole == CarSeatRole.Driver;
    }

    private void HandleSeatEntered(VRCPlayerApi player)
    {
        if (vehicleController == null || !Utilities.IsValid(player))
        {
            return;
        }

        if (player.isLocal)
        {
            if (_localSeatOccupied)
            {
                return;
            }

            _localSeatOccupied = true;
        }

        if (seatRole == CarSeatRole.Driver && player.isLocal && !vehicleController.CanPlayerDrive(player))
        {
            SendCustomEventDelayedFrames(nameof(ForceLocalExit), 1);
            return;
        }

        vehicleController.OnSeatEntered(this, player);
        RefreshLocalAccess();
    }

    private void HandleSeatExited(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player))
        {
            return;
        }

        if (player.isLocal)
        {
            if (!_localSeatOccupied)
            {
                return;
            }

            _localSeatOccupied = false;
        }

        if (vehicleController != null)
        {
            vehicleController.OnSeatExited(this, player);
        }

        RefreshLocalAccess();
    }

    private void RefreshLocalAccess()
    {
        bool allowed = true;

        if (seatRole == CarSeatRole.Driver && vehicleController != null)
        {
            allowed = vehicleController.CanLocalPlayerUseDriverSeat();
        }

        if (seatCollider != null && allowed != _lastLocalAccess)
        {
            seatCollider.enabled = allowed;
        }

        _lastLocalAccess = allowed;
    }
}
