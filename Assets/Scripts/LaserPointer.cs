using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

//[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LaserPointer : UdonSharpBehaviour
{
    public LineRenderer laserLine;
    public float maxDistance = 10f;
    public Transform tipPoint;

    [UdonSynced, FieldChangeCallback(nameof(IsHeld))]
    private bool _isHeld;

    [UdonSynced, FieldChangeCallback(nameof(IsLaserOn))]
    private bool _laserOn;

    public bool IsHeld
    {
        set
        {
            _isHeld = value;
            UpdateVisibility();
        }
        get => _isHeld;
    }

    public bool IsLaserOn
    {
        set
        {
            _laserOn = value;
            UpdateVisibility();
        }
        get => _laserOn;
    }

    void Start()
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (laserLine == null) return;

        if (Networking.IsOwner(gameObject))
        {
            // Owner sees beam ONLY when ON
            laserLine.enabled = _laserOn;
        }
        else
        {
            // Others see beam ONLY when held AND ON
            laserLine.enabled = _isHeld && _laserOn;
        }
    }

    public override void OnPickup()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        IsHeld = true;
        RequestSerialization();
    }

    public override void OnDrop()
    {
        IsHeld = false;
        RequestSerialization();
    }

    public override void OnPickupUseDown()
    {
        IsLaserOn = !_laserOn;
        RequestSerialization();
    }

    void Update()
    {
        if (laserLine == null || tipPoint == null) return;
        if (!_laserOn) return;

        // Only owner calculates the real raycast
        if (!Networking.IsOwner(gameObject)) return;

        Ray ray = new Ray(tipPoint.position, tipPoint.forward);
        RaycastHit hit;

        Vector3 endPoint;

        if (Physics.Raycast(ray, out hit, maxDistance))
            endPoint = hit.point;
        else
            endPoint = ray.GetPoint(maxDistance);

        laserLine.SetPosition(0, tipPoint.position);
        laserLine.SetPosition(1, endPoint);
    }

    public override void OnDeserialization()
    {
        UpdateVisibility();

        if (!_isHeld || !_laserOn) return;
        if (laserLine == null || tipPoint == null) return;

        // Approximate beam for others
        laserLine.SetPosition(0, tipPoint.position);
        laserLine.SetPosition(1, tipPoint.position + tipPoint.forward * maxDistance);
    }
}
