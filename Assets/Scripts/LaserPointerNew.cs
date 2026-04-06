using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LaserPointerNew : UdonSharpBehaviour
{
    [Header("References")]
    public LineRenderer laserLine;
    public Transform tipPoint;
    public MeshRenderer visualRenderer;

    [Header("Settings")]
    public float maxDistance = 10f;

    [UdonSynced, FieldChangeCallback(nameof(IsHeld))]
    private bool _isHeld;

    [UdonSynced, FieldChangeCallback(nameof(IsLaserOn))]
    private bool _laserOn;

    public bool IsHeld
    {
        set { _isHeld = value; UpdateVisibility(); }
        get => _isHeld;
    }

    public bool IsLaserOn
    {
        set { _laserOn = value; UpdateVisibility(); }
        get => _laserOn;
    }

    void Start()
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool isOwner = Networking.IsOwner(gameObject);

        // Physical pointer body:
        // - Owner: always visible
        // - Others: only visible when owner is holding it
        if (visualRenderer != null)
            visualRenderer.enabled = isOwner || _isHeld;

        // Laser beam:
        // - Everyone sees it only when held AND toggled on
        if (laserLine != null)
            laserLine.enabled = _isHeld && _laserOn;
    }

    public override void OnPickup()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        IsHeld = true;
        RequestSerialization();
    }

    public override void OnDrop()
    {
        // Always turn off laser on drop, then mark as not held
        _laserOn = false;
        IsHeld = false;
        RequestSerialization();
    }

    public override void OnPickupUseDown()
    {
        // Toggle laser only while holding
        IsLaserOn = !_laserOn;
        RequestSerialization();
    }

    void Update()
    {
        if (laserLine == null || tipPoint == null) return;
        if (!_isHeld || !_laserOn) return;
        if (!Networking.IsOwner(gameObject)) return;

        // Owner does the real raycast
        Ray ray = new Ray(tipPoint.position, tipPoint.forward);
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            endPoint = hit.point;
        else
            endPoint = ray.GetPoint(maxDistance);

        laserLine.SetPosition(0, tipPoint.position);
        laserLine.SetPosition(1, endPoint);
    }

    public override void OnDeserialization()
    {
        // Called on remote clients when synced vars update
        UpdateVisibility();

        if (!_isHeld || !_laserOn) return;
        if (laserLine == null || tipPoint == null) return;

        // Approximate beam direction for other players
        laserLine.SetPosition(0, tipPoint.position);
        laserLine.SetPosition(1, tipPoint.position + tipPoint.forward * maxDistance);
    }
}