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
    private const int MaxAutoSeatRetries = 20;

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
    public Light[] headlights;
    public bool loopRoute = true;
    public float waypointReachDistance = 1.5f;
    public float engineStartDelay = 0.2f;
    public bool stopWhenDriverExits = true;
    public bool allowMasterFallback = true;
    public float groundProbeHeight = 3f;
    public float groundProbeDistance = 10f;

    private int _driverPlayerId = -1;
    private bool _engineRunning;
    private bool _routeCompleted;
    private bool _localAutoSeated;
    private int _autoSeatRetryCount;

    private float _currentSpeed;
    private float _steerInput;
    private float _throttleInput;
    private float _engineReadyTime;
    private int _routeIndex;
    private bool _debugHasStepState;
    private bool _debugLastCanMove;
    private bool _debugLoggedLowSpeed;
    private bool _debugLoggedMovement;
    private bool _debugLoggedPendingEngine;
    private bool _debugLoggedOwnershipLoss;

    private void Start()
    {
        SnapToGround();
        SetHeadlightsEnabled(false);
        _localAutoSeated = false;
        _autoSeatRetryCount = 0;
        SendCustomEventDelayedFrames(nameof(TryAutoSeatLocalPlayer), 2);
    }

    private void Update()
    {
        bool isOwner = Networking.IsOwner(gameObject);
        if (_engineRunning && isOwner && Time.time < _engineReadyTime && !_debugLoggedPendingEngine)
        {
            Debug.Log("[CarVehicleController] Update waiting for engine ready. time=" + Time.time +
                      ", engineReadyTime=" + _engineReadyTime +
                      ", driverPlayerId=" + _driverPlayerId);
            _debugLoggedPendingEngine = true;
        }

        if (_engineRunning && !isOwner && !_debugLoggedOwnershipLoss)
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            int localPlayerId = Utilities.IsValid(localPlayer) ? localPlayer.playerId : -1;
            Debug.Log("[CarVehicleController] Update skipped: local is not owner while engineRunning. localPlayerId=" + localPlayerId +
                      ", driverPlayerId=" + _driverPlayerId);
            _debugLoggedOwnershipLoss = true;
        }

        if (isOwner)
        {
            StepVehicle(Time.deltaTime);
        }
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

    public bool HasRouteAvailable()
    {
        return routePoints != null && routePoints.Length > 0;
    }

    public bool IsRouteRunning()
    {
        return _engineRunning;
    }

    public bool IsLocalDriverInControl()
    {
        return IsLocalDriver();
    }

    public bool CanLocalPlayerStartRoute()
    {
        return IsLocalDriver() && !_routeCompleted && HasRouteAvailable();
    }

    public bool CanLocalPlayerToggleVehicle()
    {
        if (!IsLocalDriver())
        {
            return false;
        }

        if (_engineRunning)
        {
            return true;
        }

        if (driveMode == CarDriveMode.AutoRoute)
        {
            return !_routeCompleted && HasRouteAvailable();
        }

        return true;
    }

    public void TryAutoSeatLocalPlayer()
    {
        if (_localAutoSeated)
        {
            return;
        }

        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer))
        {
            ScheduleAutoSeatRetry();
            return;
        }

        CarSeatStation assignedSeat = GetAssignedSeat(localPlayer);
        if (assignedSeat == null || assignedSeat.station == null)
        {
            return;
        }

        assignedSeat.station.UseStation(localPlayer);
        ScheduleAutoSeatRetry();
    }

    public void OnSeatEntered(CarSeatStation seat, VRCPlayerApi player)
    {
        if (seat == null || !Utilities.IsValid(player))
        {
            return;
        }

        if (player.isLocal)
        {
            _localAutoSeated = true;
        }

        if (seat != driverSeat)
        {
            return;
        }

        _driverPlayerId = player.playerId;

        if (player.isLocal && !CanPlayerDrive(player))
        {
            driverSeat.SendCustomEvent(nameof(CarSeatStation.ForceLocalExit));
            return;
        }

        SetHeadlightsEnabled(true);

        if (!player.isLocal)
        {
            return;
        }

        Networking.SetOwner(player, gameObject);
        _steerInput = 0f;
        _throttleInput = 0f;
        _currentSpeed = 0f;
        _routeIndex = 0;
        _routeCompleted = false;

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

    public void OnSeatExited(CarSeatStation seat, VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player))
        {
            return;
        }

        if (player.isLocal)
        {
            _localAutoSeated = false;
            _autoSeatRetryCount = 0;
            SendCustomEventDelayedFrames(nameof(TryAutoSeatLocalPlayer), 5);
        }

        if (seat != driverSeat || player.playerId != _driverPlayerId)
        {
            return;
        }

        _driverPlayerId = -1;
        SetHeadlightsEnabled(false);
        _steerInput = 0f;
        _throttleInput = 0f;

        if (stopWhenDriverExits)
        {
            Debug.Log("[CarVehicleController] OnSeatExited stopping engine. playerId=" + player.playerId +
                      ", isLocal=" + player.isLocal +
                      ", currentSpeed=" + _currentSpeed);
            _engineRunning = false;
        }

        if (player.isLocal && Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    public void StartVehicle()
    {
        int routeCount = routePoints != null ? routePoints.Length : 0;
        bool isOwner = Networking.IsOwner(gameObject);
        bool hasActiveDriver = HasActiveDriver();
        bool hasRoute = HasRouteAvailable();
        Debug.Log("[CarVehicleController] StartVehicle called. isOwner=" + isOwner +
                  ", hasActiveDriver=" + hasActiveDriver +
                  ", driveMode=" + driveMode +
                  ", hasRoute=" + hasRoute +
                  ", routeCompleted=" + _routeCompleted +
                  ", routeIndex=" + _routeIndex +
                  ", routeCount=" + routeCount);

        if (!isOwner || !hasActiveDriver)
        {
            Debug.Log("[CarVehicleController] StartVehicle rejected: missing ownership or active driver.");
            return;
        }

        if (driveMode == CarDriveMode.AutoRoute)
        {
            if (!hasRoute || _routeCompleted)
            {
                Debug.Log("[CarVehicleController] StartVehicle rejected: autoroute unavailable or already completed.");
                return;
            }

            _routeIndex = Mathf.Clamp(_routeIndex, 0, routePoints.Length - 1);
        }

        _engineRunning = true;
        _engineReadyTime = Time.time + engineStartDelay;
        _debugHasStepState = false;
        _debugLoggedLowSpeed = false;
        _debugLoggedMovement = false;
        _debugLoggedPendingEngine = false;
        _debugLoggedOwnershipLoss = false;
        RequestSerialization();
    }

    public void StopVehicle()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Debug.Log("[CarVehicleController] StopVehicle ignored: local is not owner.");
            return;
        }

        Debug.Log("[CarVehicleController] StopVehicle called. currentSpeed=" + _currentSpeed +
                  ", routeIndex=" + _routeIndex +
                  ", routeCompleted=" + _routeCompleted +
                  ", driverPlayerId=" + _driverPlayerId);
        _engineRunning = false;
        _steerInput = 0f;
        _throttleInput = 0f;
        RequestSerialization();
    }

    public void ToggleVehicle()
    {
        Debug.Log("[CarVehicleController] ToggleVehicle called. engineRunning=" + _engineRunning +
                  ", isLocalDriver=" + IsLocalDriver());
        if (_engineRunning)
        {
            StopVehicle();
        }
        else
        {
            StartVehicle();
        }
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        Debug.Log("[CarVehicleController] InputUse received. value=" + value +
                  ", isLocalDriver=" + IsLocalDriver() +
                  ", engineRunning=" + _engineRunning);
        if (!value || !IsLocalDriver())
        {
            return;
        }

        if (driveMode == CarDriveMode.AutoRoute)
        {
            return;
        }

        ToggleVehicle();
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
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        int newOwnerId = Utilities.IsValid(player) ? player.playerId : -1;
        int localPlayerId = Utilities.IsValid(localPlayer) ? localPlayer.playerId : -1;
        Debug.Log("[CarVehicleController] OnOwnershipTransferred. newOwnerId=" + newOwnerId +
                  ", localPlayerId=" + localPlayerId +
                  ", localIsOwner=" + Networking.IsOwner(gameObject) +
                  ", driverPlayerId=" + _driverPlayerId);

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
        SetHeadlightsEnabled(false);
        _steerInput = 0f;
        _throttleInput = 0f;
        Debug.Log("[CarVehicleController] OnPlayerLeft stopping engine. playerId=" + player.playerId);
        _engineRunning = false;

        if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    private CarSeatStation GetAssignedSeat(VRCPlayerApi player)
    {
        if (CanPlayerDrive(player) && driverSeat != null)
        {
            return driverSeat;
        }

        if (passengerSeats == null || passengerSeats.Length == 0)
        {
            return null;
        }

        int passengerIndex = player.playerId - 2;
        if (passengerIndex < 0)
        {
            passengerIndex = 0;
        }
        else if (passengerIndex >= passengerSeats.Length)
        {
            passengerIndex = passengerSeats.Length - 1;
        }

        return passengerSeats[passengerIndex];
    }

    private void ScheduleAutoSeatRetry()
    {
        if (_localAutoSeated || _autoSeatRetryCount >= MaxAutoSeatRetries)
        {
            return;
        }

        _autoSeatRetryCount++;
        SendCustomEventDelayedFrames(nameof(TryAutoSeatLocalPlayer), 10);
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

    private void SetHeadlightsEnabled(bool enabled)
    {
        if (headlights == null)
        {
            return;
        }

        for (int i = 0; i < headlights.Length; i++)
        {
            Light headlight = headlights[i];
            if (headlight == null)
            {
                continue;
            }

            headlight.enabled = enabled;
        }
    }

    private void StepVehicle(float deltaTime)
    {
        bool canMove = _engineRunning && HasActiveDriver() && Time.time >= _engineReadyTime;

        if (!_debugHasStepState || canMove != _debugLastCanMove)
        {
            Debug.Log("[CarVehicleController] StepVehicle state. engineRunning=" + _engineRunning +
                      ", hasActiveDriver=" + HasActiveDriver() +
                      ", engineReady=" + (Time.time >= _engineReadyTime) +
                      ", canMove=" + canMove +
                      ", currentSpeed=" + _currentSpeed);
            _debugHasStepState = true;
            _debugLastCanMove = canMove;
        }

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
            if (_engineRunning && !_debugLoggedLowSpeed)
            {
                Debug.Log("[CarVehicleController] UpdatePosition skipped: currentSpeed too low (" + _currentSpeed + ").");
                _debugLoggedLowSpeed = true;
            }
            return;
        }

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        flatForward.Normalize();
        Vector3 delta = flatForward * (_currentSpeed * deltaTime);
        if (!_debugLoggedMovement)
        {
            Debug.Log("[CarVehicleController] UpdatePosition moving. delta=" + delta +
                      ", currentSpeed=" + _currentSpeed +
                      ", positionBefore=" + transform.position);
            _debugLoggedMovement = true;
        }

        transform.position += delta;
    }

    private float GetAutoRouteSteer()
    {
        if (!HasRouteAvailable())
        {
            Debug.Log("[CarVehicleController] GetAutoRouteSteer stopping engine: route unavailable.");
            _engineRunning = false;
            _routeCompleted = true;
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

            if (!_engineRunning || !HasRouteAvailable())
            {
                return 0f;
            }

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
        if (!HasRouteAvailable())
        {
            Debug.Log("[CarVehicleController] AdvanceRouteIndex stopping engine: route unavailable.");
            _engineRunning = false;
            _routeCompleted = true;
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
            return;
        }

        Debug.Log("[CarVehicleController] AdvanceRouteIndex completed final waypoint. routeIndex=" + _routeIndex);
        _engineRunning = false;
        _routeCompleted = true;
        RequestSerialization();
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







