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
    private const int StartupGroundingFrames = 120;
    private const float StartupGroundProbeHeight = 1000f;
    private const float StartupGroundProbeDistance = 5000f;
    private const int NoTerminalIndex = -1;
    private const float SeatUseDebounceSeconds = 0.35f;

    public CarDriveMode driveMode = CarDriveMode.Manual;

    public CarSeatStation driverSeat;
    public CarSeatStation[] passengerSeats;

    public float maxForwardSpeed = 5.5f;
    public float maxReverseSpeed = 4f;
    public float acceleration = 2.5f;
    public float brakeDeceleration = 5f;
    public float steerRate = 32f;
    public float drag = 3f;
    public float rideHeight = 0.05f;
    public LayerMask groundMask = ~0;
    public Transform[] routePoints;
    public Light[] headlights;
    public bool loopRoute = true;
    public float waypointReachDistance = 4f;
    public float engineStartDelay = 0.2f;
    public bool stopWhenDriverExits = true;
    public bool allowMasterFallback = true;
    public float groundProbeHeight = 3f;
    public float groundProbeDistance = 10f;
    public float groundSmoothTime = 0.22f;
    public float groundDeadZone = 0.015f;
    public float steerSmoothTime = 0.35f;
    public float autoRouteSlowdownDistance = 8f;
    public float autoRouteMinSpeedFactor = 0.35f;
    public float autoRouteTurnMinSpeedFactor = 0.55f;

    private int _driverPlayerId = -1;
    private int[] _seatOccupants;
    private bool _engineRunning;
    private bool _routeCompleted;
    private float _currentSpeed;
    private float _steerInput;
    private float _throttleInput;
    private float _engineReadyTime;
    private int _routeIndex;
    private int _startupGroundingFramesRemaining;
    private int _dockedTerminalIndex = NoTerminalIndex;
    private int _routeDirection = 1;
    private float _ignoreUseUntilTime;
    private bool _groundYInitialized;
    private float _smoothedSteerInput;
    private float _steerVelocity;
    private float _groundYVelocity;

    private void Start()
    {
        EnsureSeatState();
        InitializeRouteState();
        _startupGroundingFramesRemaining = StartupGroundingFrames;
        SnapToGround(StartupGroundProbeHeight, StartupGroundProbeDistance);
        SetHeadlightsEnabled(false);
        SendCustomEventDelayedFrames(nameof(EnsureGroundedDuringStartup), 1);
    }

    private void Update()
    {
        if (Networking.IsOwner(gameObject))
        {
            StepVehicle(Time.deltaTime);
        }
    }

    public bool CanLocalPlayerUseDriverSeat()
    {
        return CanLocalPlayerEnterSeat(driverSeat);
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
        return routePoints != null && routePoints.Length > 1;
    }

    public bool IsRouteRunning()
    {
        return _engineRunning;
    }

    public bool IsRouteCompleted()
    {
        return _routeCompleted;
    }

    public bool CanLocalPlayerStartRoute()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer))
        {
            return false;
        }

        return IsSeatOccupiedByLocalPlayer(driverSeat) &&
               CanPlayerDrive(localPlayer) &&
               !_engineRunning &&
               IsDockedAtTerminal() &&
               HasRouteTravelFromDockedTerminal() &&
               Networking.IsOwner(gameObject);
    }

    public bool CanLocalPlayerToggleVehicle()
    {
        return CanLocalPlayerStartRoute();
    }

    public CarSeatStation GetLocalOccupiedSeat()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer))
        {
            return null;
        }

        if (IsSeatOccupiedByLocalPlayer(driverSeat))
        {
            return driverSeat;
        }

        if (passengerSeats == null)
        {
            return null;
        }

        for (int i = 0; i < passengerSeats.Length; i++)
        {
            CarSeatStation seat = passengerSeats[i];
            if (IsSeatOccupiedByLocalPlayer(seat))
            {
                return seat;
            }
        }

        return null;
    }

    public bool CanLocalPlayerEnterSeat(CarSeatStation seat)
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (seat == null || !Utilities.IsValid(localPlayer))
        {
            return false;
        }

        if (_engineRunning)
        {
            return false;
        }

        if (IsLocalPlayerSeatedInAnySeat())
        {
            return false;
        }

        if (IsSeatOccupied(seat))
        {
            return false;
        }

        if (seat.IsDriverSeat() && !CanPlayerDrive(localPlayer))
        {
            return false;
        }

        return seat.station != null;
    }

    public bool CanLocalPlayerExitSeat(CarSeatStation seat)
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (seat == null || !Utilities.IsValid(localPlayer))
        {
            return false;
        }

        if (_engineRunning)
        {
            return false;
        }

        return IsSeatOccupiedByLocalPlayer(seat) && seat.station != null;
    }

    public bool CanLocalPlayerExitCurrentSeat()
    {
        return CanLocalPlayerExitSeat(GetLocalOccupiedSeat());
    }

    public bool IsSeatOccupied(CarSeatStation seat)
    {
        return GetSeatOccupantId(seat) >= 0;
    }

    public bool IsSeatOccupiedByLocalPlayer(CarSeatStation seat)
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        return Utilities.IsValid(localPlayer) && GetSeatOccupantId(seat) == localPlayer.playerId;
    }

    public bool IsSeatOccupiedByOtherPlayer(CarSeatStation seat)
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        int occupantId = GetSeatOccupantId(seat);
        return occupantId >= 0 && (!Utilities.IsValid(localPlayer) || occupantId != localPlayer.playerId);
    }

    public bool IsLocalPlayerSeatedInAnySeat()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer))
        {
            return false;
        }

        EnsureSeatState();
        for (int i = 0; i < _seatOccupants.Length; i++)
        {
            if (_seatOccupants[i] == localPlayer.playerId)
            {
                return true;
            }
        }

        return false;
    }

    public void OnSeatEntered(CarSeatStation seat, VRCPlayerApi player)
    {
        if (seat == null || !Utilities.IsValid(player))
        {
            return;
        }

        SetSeatOccupant(seat, player.playerId);

        if (player.isLocal)
        {
            _ignoreUseUntilTime = Time.time + SeatUseDebounceSeconds;
        }

        if (seat != driverSeat)
        {
            return;
        }

        _driverPlayerId = player.playerId;
        SetHeadlightsEnabled(true);

        if (!player.isLocal)
        {
            return;
        }

        if (!CanPlayerDrive(player))
        {
            driverSeat.SendCustomEvent(nameof(CarSeatStation.ForceLocalExit));
            return;
        }

        Networking.SetOwner(player, gameObject);
        _steerInput = 0f;
        _throttleInput = 0f;
        _currentSpeed = 0f;

        if (driveMode == CarDriveMode.Manual)
        {
            _engineRunning = true;
            _engineReadyTime = Time.time + engineStartDelay;
        }
        else
        {
            _engineRunning = false;
            _engineReadyTime = 0f;

            if (!HasRouteAvailable())
            {
                _dockedTerminalIndex = NoTerminalIndex;
                _routeIndex = 0;
                _routeDirection = 1;
            }
            else if (!IsDockedAtTerminal())
            {
                InitializeRouteState();
            }
        }

        RequestSerialization();
    }

    public void OnSeatExited(CarSeatStation seat, VRCPlayerApi player)
    {
        if (seat == null || !Utilities.IsValid(player))
        {
            return;
        }

        ClearSeatOccupant(seat);

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
            _engineRunning = false;
            _currentSpeed = 0f;
        }

        if (player.isLocal && Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    public void StartVehicle()
    {
        if (!CanLocalPlayerStartRoute())
        {
            return;
        }

        int nextRouteIndex = GetNextRouteIndexFromDockedTerminal();
        if (nextRouteIndex < 0)
        {
            return;
        }

        _routeDirection = GetDirectionForTerminal(_dockedTerminalIndex);
        _routeIndex = nextRouteIndex;
        _dockedTerminalIndex = NoTerminalIndex;
        _routeCompleted = false;
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

    public void ToggleVehicle()
    {
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
        if (!value || Time.time < _ignoreUseUntilTime)
        {
            return;
        }

        CarSeatStation localSeat = GetLocalOccupiedSeat();
        if (localSeat == null)
        {
            return;
        }

        if (driveMode == CarDriveMode.Manual)
        {
            if (localSeat == driverSeat && IsLocalDriver())
            {
                ToggleVehicle();
            }

            return;
        }

        if (localSeat == driverSeat && CanLocalPlayerStartRoute())
        {
            StartVehicle();
        }
    }

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        TryExitCurrentSeatFromSecondaryInput(value);
    }

    public override void InputDrop(bool value, UdonInputEventArgs args)
    {
        TryExitCurrentSeatFromSecondaryInput(value);
    }

    private void TryExitCurrentSeatFromSecondaryInput(bool value)
    {
        if (!value)
        {
            return;
        }

        CarSeatStation localSeat = GetLocalOccupiedSeat();
        if (localSeat == null || !CanLocalPlayerExitSeat(localSeat))
        {
            return;
        }

        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer) || localSeat.station == null)
        {
            return;
        }

        localSeat.station.ExitStation(localPlayer);
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
        if (!Utilities.IsValid(player))
        {
            return;
        }

        ClearSeatOccupantByPlayerId(player.playerId);

        if (player.playerId != _driverPlayerId)
        {
            return;
        }

        _driverPlayerId = -1;
        SetHeadlightsEnabled(false);
        _steerInput = 0f;
        _throttleInput = 0f;
        _engineRunning = false;
        _currentSpeed = 0f;

        if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    public void EnsureGroundedDuringStartup()
    {
        if (_startupGroundingFramesRemaining <= 0)
        {
            return;
        }

        _startupGroundingFramesRemaining--;
        SnapToGround(StartupGroundProbeHeight, StartupGroundProbeDistance);

        if (_startupGroundingFramesRemaining > 0)
        {
            SendCustomEventDelayedFrames(nameof(EnsureGroundedDuringStartup), 1);
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
        return _driverPlayerId >= 0 && IsSeatOccupied(driverSeat);
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
            if (headlight != null)
            {
                headlight.enabled = enabled;
            }
        }
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
                float rawSteer = GetAutoRouteSteer();
                _smoothedSteerInput = Mathf.SmoothDamp(_smoothedSteerInput, rawSteer, ref _steerVelocity, steerSmoothTime);
                steerInput = _smoothedSteerInput;
                driveInput = GetAutoRouteDriveInput(steerInput);
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
        Vector3 toTarget;
        if (!TryGetAutoRouteTargetOffset(out toTarget))
        {
            return 0f;
        }

        toTarget.Normalize();
        Vector3 localDirection = transform.InverseTransformDirection(toTarget);
        return Mathf.Clamp(localDirection.x, -1f, 1f);
    }

    private float GetAutoRouteDriveInput(float steerInput)
    {
        Vector3 toTarget;
        if (!TryGetAutoRouteTargetOffset(out toTarget))
        {
            return 0f;
        }

        float slowdownDistance = Mathf.Max(waypointReachDistance + 0.01f, autoRouteSlowdownDistance);
        float distanceFactor = Mathf.Clamp01(toTarget.magnitude / slowdownDistance);
        float approachSpeedFactor = Mathf.Lerp(autoRouteMinSpeedFactor, 1f, distanceFactor);
        float turnSpeedFactor = Mathf.Lerp(1f, autoRouteTurnMinSpeedFactor, Mathf.Clamp01(Mathf.Abs(steerInput)));
        return Mathf.Clamp01(Mathf.Min(approachSpeedFactor, turnSpeedFactor));
    }

    private bool TryGetAutoRouteTargetOffset(out Vector3 toTarget)
    {
        toTarget = Vector3.zero;

        if (!HasRouteAvailable())
        {
            DockAtTerminal(NoTerminalIndex);
            return false;
        }

        while (true)
        {
            Transform targetPoint = routePoints[_routeIndex];
            if (!Utilities.IsValid(targetPoint))
            {
                AdvanceRouteIndex();
                if (!_engineRunning || !HasRouteAvailable())
                {
                    return false;
                }

                continue;
            }

            toTarget = targetPoint.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f || toTarget.magnitude <= waypointReachDistance)
            {
                AdvanceRouteIndex();
                if (!_engineRunning || !HasRouteAvailable())
                {
                    return false;
                }

                continue;
            }

            return true;
        }
    }

    private void AdvanceRouteIndex()
    {
        if (!HasRouteAvailable())
        {
            DockAtTerminal(NoTerminalIndex);
            return;
        }

        int lastRouteIndex = GetLastRouteIndex();
        if (_routeDirection > 0)
        {
            if (_routeIndex < lastRouteIndex)
            {
                _routeIndex++;
                return;
            }

            DockAtTerminal(lastRouteIndex);
            return;
        }

        if (_routeIndex > 0)
        {
            _routeIndex--;
            return;
        }

        DockAtTerminal(0);
    }

    private void DockAtTerminal(int terminalIndex)
    {
        _engineRunning = false;
        _routeCompleted = terminalIndex != NoTerminalIndex;
        _currentSpeed = 0f;
        _steerInput = 0f;
        _throttleInput = 0f;
        _dockedTerminalIndex = terminalIndex;

        if (terminalIndex != NoTerminalIndex)
        {
            _routeIndex = terminalIndex;
            _routeDirection = GetDirectionForTerminal(terminalIndex);
        }

        RequestSerialization();
    }

    private void InitializeRouteState()
    {
        if (!HasRouteAvailable())
        {
            _dockedTerminalIndex = NoTerminalIndex;
            _routeIndex = 0;
            _routeDirection = 1;
            _routeCompleted = false;
            return;
        }

        _dockedTerminalIndex = DetectNearestTerminalIndex();
        _routeDirection = GetDirectionForTerminal(_dockedTerminalIndex);
        _routeIndex = _dockedTerminalIndex;
        _routeCompleted = false;
    }

    private bool IsDockedAtTerminal()
    {
        if (_dockedTerminalIndex == NoTerminalIndex || _engineRunning)
        {
            return false;
        }

        int lastRouteIndex = GetLastRouteIndex();
        return _dockedTerminalIndex == 0 || _dockedTerminalIndex == lastRouteIndex;
    }

    private bool HasRouteTravelFromDockedTerminal()
    {
        int lastRouteIndex = GetLastRouteIndex();
        if (lastRouteIndex < 1)
        {
            return false;
        }

        return _dockedTerminalIndex == 0 || _dockedTerminalIndex == lastRouteIndex;
    }

    private int GetNextRouteIndexFromDockedTerminal()
    {
        int lastRouteIndex = GetLastRouteIndex();
        if (lastRouteIndex < 1)
        {
            return -1;
        }

        if (_dockedTerminalIndex == 0)
        {
            return 1;
        }

        if (_dockedTerminalIndex == lastRouteIndex)
        {
            return lastRouteIndex - 1;
        }

        return -1;
    }

    private int GetDirectionForTerminal(int terminalIndex)
    {
        return terminalIndex == GetLastRouteIndex() ? -1 : 1;
    }

    private int GetLastRouteIndex()
    {
        return routePoints != null ? routePoints.Length - 1 : -1;
    }

    private int DetectNearestTerminalIndex()
    {
        int lastRouteIndex = GetLastRouteIndex();
        if (lastRouteIndex <= 0)
        {
            return 0;
        }

        Transform startPoint = routePoints[0];
        Transform endPoint = routePoints[lastRouteIndex];
        if (!Utilities.IsValid(startPoint))
        {
            return lastRouteIndex;
        }

        if (!Utilities.IsValid(endPoint))
        {
            return 0;
        }

        Vector3 startOffset = startPoint.position - transform.position;
        startOffset.y = 0f;
        Vector3 endOffset = endPoint.position - transform.position;
        endOffset.y = 0f;
        return endOffset.sqrMagnitude < startOffset.sqrMagnitude ? lastRouteIndex : 0;
    }

    private void SnapToGround()
    {
        SnapToGround(groundProbeHeight, groundProbeDistance);
    }

    private bool SnapToGround(float probeHeight, float probeDistance)
    {
        Vector3 origin = transform.position + Vector3.up * probeHeight;
        RaycastHit hit;

        if (Physics.Raycast(origin, Vector3.down, out hit, probeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y + rideHeight;
            Vector3 position = transform.position;

            if (_groundYInitialized && _startupGroundingFramesRemaining <= 0)
            {
                float diff = targetY - position.y;
                if (Mathf.Abs(diff) <= groundDeadZone)
                {
                    _groundYVelocity = 0f;
                }
                else
                {
                    position.y = Mathf.SmoothDamp(position.y, targetY, ref _groundYVelocity, Mathf.Max(0.01f, groundSmoothTime), Mathf.Infinity, Time.deltaTime);
                }
            }
            else
            {
                position.y = targetY;
                _groundYVelocity = 0f;
                _groundYInitialized = true;
            }

            transform.position = position;
            return true;
        }

        return false;
    }

    private void EnsureSeatState()
    {
        int totalSeats = 1 + (passengerSeats != null ? passengerSeats.Length : 0);
        if (_seatOccupants != null && _seatOccupants.Length == totalSeats)
        {
            return;
        }

        _seatOccupants = new int[totalSeats];
        for (int i = 0; i < _seatOccupants.Length; i++)
        {
            _seatOccupants[i] = -1;
        }
    }

    private int GetSeatIndex(CarSeatStation seat)
    {
        if (seat == null)
        {
            return -1;
        }

        if (seat == driverSeat)
        {
            return 0;
        }

        if (passengerSeats == null)
        {
            return -1;
        }

        for (int i = 0; i < passengerSeats.Length; i++)
        {
            if (passengerSeats[i] == seat)
            {
                return i + 1;
            }
        }

        return -1;
    }

    private int GetSeatOccupantId(CarSeatStation seat)
    {
        EnsureSeatState();
        int seatIndex = GetSeatIndex(seat);
        if (seatIndex < 0 || seatIndex >= _seatOccupants.Length)
        {
            return -1;
        }

        return _seatOccupants[seatIndex];
    }

    private void SetSeatOccupant(CarSeatStation seat, int playerId)
    {
        EnsureSeatState();
        int seatIndex = GetSeatIndex(seat);
        if (seatIndex >= 0 && seatIndex < _seatOccupants.Length)
        {
            _seatOccupants[seatIndex] = playerId;
        }
    }

    private void ClearSeatOccupant(CarSeatStation seat)
    {
        EnsureSeatState();
        int seatIndex = GetSeatIndex(seat);
        if (seatIndex >= 0 && seatIndex < _seatOccupants.Length)
        {
            _seatOccupants[seatIndex] = -1;
        }
    }

    private void ClearSeatOccupantByPlayerId(int playerId)
    {
        EnsureSeatState();
        for (int i = 0; i < _seatOccupants.Length; i++)
        {
            if (_seatOccupants[i] == playerId)
            {
                _seatOccupants[i] = -1;
            }
        }
    }
}






