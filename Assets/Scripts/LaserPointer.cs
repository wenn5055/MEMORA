using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class LaserPointer : UdonSharpBehaviour
{
    public LineRenderer laserLine;
    public float maxDistance = 10f;
    public Transform tipPoint; // tip of the pointer

    [UdonSynced] private bool laserOn = false;
    [UdonSynced] private Vector3 laserEndPoint;

    // Called by VRC when player presses Use button while holding
    public override void OnPickupUseDown()
    {
        laserOn = !laserOn;
        RequestSerialization();
    }

    public override void OnDrop()
    {
        // Turn off laser when dropped
        laserOn = false;
        RequestSerialization();
        if (laserLine) laserLine.enabled = false;
    }

    void Update()
    {
        if (laserLine == null || tipPoint == null) return;

        laserLine.enabled = laserOn;

        if (!laserOn) return;

        // Raycast from tip forward
        Ray ray = new Ray(tipPoint.position, tipPoint.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
            laserEndPoint = hit.point;
        else
            laserEndPoint = ray.GetPoint(maxDistance);

        // Update line positions
        laserLine.SetPosition(0, tipPoint.position);
        laserLine.SetPosition(1, laserEndPoint);

        // Only sync if we own it
        if (Networking.IsOwner(gameObject))
            RequestSerialization();
    }

    // Remote players see the laser via synced data
    public override void OnDeserialization()
    {
        if (laserLine == null) return;
        laserLine.enabled = laserOn;
        if (laserOn && tipPoint != null)
        {
            laserLine.SetPosition(0, tipPoint.position);
            laserLine.SetPosition(1, laserEndPoint);
        }
    }
}