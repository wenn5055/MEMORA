using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

public enum CarDriveMode
{
    Manual,
    AutoRoute,
}

[AddComponentMenu("VRChat/Car/Car Vehicle Controller")]
public class CarVehicleController : UdonSharpBehaviour
{
    public CarDriveMode driveMode = CarDriveMode.Manual;

    public CarSeatStation driverSeat;
    public CarSeatStation[] passengerSeats;

    public float maxForwardSpeed = 10f;
    public float maxReverseSpeed = 4f;
    public float acceleration = 6f;
    public float brakeDeceleration = 10f;
    public float steerRate = 65f;
    public float drag = 3f;
    public float rideHeight = 0.35f;
    public LayerMask groundMask = ~0;
    public Transform[] routePoints;
    public bool loopRoute = true;
    public float waypointReachDistance = 1.5f;
    public float engineStartDelay = 0.2f;
    public bool stopWhenDriverExits = true;
    public bool allowMasterFallback = true;
    public float groundProbeHeight = 3f;
    public float groundProbeDistance = 10f;

    private int _driverPlayerId = -1;
    private bool _engineRunning;

    private float _currentSpeed;
    private float _steerInput;
    private float _throttleInput;
    private float _engineReadyTime;
    private int _routeIndex;

    private void Start()
    {
        SnapToGround();
    }

    private void Update()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }

        StepVehicle(Time.deltaTime);
    }

    public bool CanLocalPlayerUseDriverSeat()
    {
        return CanPlayerDrive(Networking.LocalPlayer);
    }

    public bool CanPlayerDrive(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player))
        {
            return false;
        }

        if (player.isInstanceOwner)
        {
            return true;
        }

        return allowMasterFallback && player.isMaster;
    }

    public void OnSeatEntered(CarSeatStation seat, VRCPlayerApi player)
    {
        if (seat == null || !Utilities.IsValid(player))
        {
            return;
        }

        if (seat == driverSeat)
        {
            _driverPlayerId = player.playerId;

            if (player.isLocal)
            {
                if (!CanPlayerDrive(player))
                {
                    driverSeat.SendCustomEvent(nameof(CarSeatStation.ForceLocalExit));
                    return;
                }

                Networking.SetOwner(player, gameObject);
                _steerInput = 0f;
                _throttleInput = 0f;
                _currentSpeed = 0f;
                _routeIndex = 0;

                if (driveMode == CarDriveMode.Manual)
                {
                    _engineRunning = true;
                    _engineReadyTime = Time.time + engineStartDelay;
                }
                else
                {
                    _engineRunning = false;
                    _engineReadyTime = 0f;
                }

                RequestSerialization();
            }
        }
    }

    public void OnSeatExited(CarSeatStation seat, VRCPlayerApi player)
    {
        if (seat != driverSeat || !Utilities.IsValid(player))
        {
            return;
        }

        if (player.playerId != _driverPlayerId)
        {
            return;
        }

        _driverPlayerId = -1;
        _steerInput = 0f;
        _throttleInput = 0f;

        if (stopWhenDriverExits)
        {
            _engineRunning = false;
        }

        if (player.isLocal && Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    public void StartVehicle()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }

        _engineRunning = true;
        _engineReadyTime = Time.time + engineStartDelay;
        RequestSerialization();
    }

    public void StopVehicle()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }

        _engineRunning = false;
        _steerInput = 0f;
        _throttleInput = 0f;
        RequestSerialization();
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (!value || !IsLocalDriver())
        {
            return;
        }

        if (_engineRunning)
        {
            StopVehicle();
        }
        else
        {
            StartVehicle();
        }
    }

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        if (!IsLocalDriver() || driveMode != CarDriveMode.Manual)
        {
            return;
        }

        _steerInput = Mathf.Clamp(value, -1f, 1f);
    }

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        if (!IsLocalDriver() || driveMode != CarDriveMode.Manual)
        {
            return;
        }

        _throttleInput = Mathf.Clamp(value, -1f, 1f);
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        return CanPlayerDrive(requestingPlayer);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        _steerInput = 0f;
        _throttleInput = 0f;

        if (!IsLocalDriver())
        {
            _currentSpeed = 0f;
        }
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player) || player.playerId != _driverPlayerId)
        {
            return;
        }

        _driverPlayerId = -1;
        _steerInput = 0f;
        _throttleInput = 0f;
        _engineRunning = false;

        if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    private bool IsLocalDriver()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        return Utilities.IsValid(localPlayer) &&
               localPlayer.playerId == _driverPlayerId &&
               Networking.IsOwner(gameObject);
    }

    private bool HasActiveDriver()
    {
        return _driverPlayerId >= 0;
    }

    private void StepVehicle(float deltaTime)
    {
        bool canMove = _engineRunning && HasActiveDriver() && Time.time >= _engineReadyTime;

        float driveInput = 0f;
        float steerInput = 0f;

        if (canMove)
        {
            if (driveMode == CarDriveMode.Manual)
            {
                driveInput = _throttleInput;
                steerInput = _steerInput;
            }
            else
            {
                driveInput = 1f;
                steerInput = GetAutoRouteSteer();
            }
        }

        UpdateSpeed(driveInput, canMove, deltaTime);
        UpdateRotation(steerInput, deltaTime);
        UpdatePosition(deltaTime);
        SnapToGround();
    }

    private void UpdateSpeed(float driveInput, bool canMove, float deltaTime)
    {
        if (!canMove)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, brakeDeceleration * deltaTime);
            return;
        }

        float targetSpeed = 0f;

        if (driveInput > 0f)
        {
            targetSpeed = driveInput * maxForwardSpeed;
        }
        else if (driveInput < 0f)
        {
            targetSpeed = driveInput * maxReverseSpeed;
        }

        float moveRate = Mathf.Abs(targetSpeed) > Mathf.Abs(_currentSpeed) ? acceleration : brakeDeceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, moveRate * deltaTime);

        if (Mathf.Abs(driveInput) < 0.01f)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, drag * deltaTime);
        }
    }

    private void UpdateRotation(float steerInput, float deltaTime)
    {
        if (Mathf.Abs(_currentSpeed) < 0.01f)
        {
            return;
        }

        float speedFactor = Mathf.Clamp01(Mathf.Abs(_currentSpeed) / Mathf.Max(0.01f, maxForwardSpeed));
        float turnDirection = _currentSpeed < 0f ? -1f : 1f;
        float turnAmount = steerInput * steerRate * speedFactor * turnDirection * deltaTime;

        transform.Rotate(0f, turnAmount, 0f, Space.World);
    }

    private void UpdatePosition(float deltaTime)
    {
        if (Mathf.Abs(_currentSpeed) < 0.001f)
        {
            return;
        }

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        flatForward.Normalize();
        transform.position += flatForward * (_currentSpeed * deltaTime);
    }

    private float GetAutoRouteSteer()
    {
        if (routePoints == null || routePoints.Length == 0)
        {
            _engineRunning = false;
            RequestSerialization();
            return 0f;
        }

        Transform targetPoint = routePoints[_routeIndex];

        if (!Utilities.IsValid(targetPoint))
        {
            AdvanceRouteIndex();
            return 0f;
        }

        Vector3 toTarget = targetPoint.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= waypointReachDistance)
        {
            AdvanceRouteIndex();
            targetPoint = routePoints[_routeIndex];

            if (!Utilities.IsValid(targetPoint))
            {
                return 0f;
            }

            toTarget = targetPoint.position - transform.position;
            toTarget.y = 0f;
        }

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        toTarget.Normalize();
        Vector3 localDirection = transform.InverseTransformDirection(toTarget);
        return Mathf.Clamp(localDirection.x, -1f, 1f);
    }

    private void AdvanceRouteIndex()
    {
        if (routePoints == null || routePoints.Length == 0)
        {
            _engineRunning = false;
            return;
        }

        if (_routeIndex < routePoints.Length - 1)
        {
            _routeIndex++;
            return;
        }

        if (loopRoute)
        {
            _routeIndex = 0;
        }
        else
        {
            _engineRunning = false;
        }
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundProbeHeight;
        RaycastHit hit;

        if (Physics.Raycast(origin, Vector3.down, out hit, groundProbeDistance, groundMask))
        {
            Vector3 position = transform.position;
            position.y = hit.point.y + rideHeight;
            transform.position = position;
        }
    }
}

