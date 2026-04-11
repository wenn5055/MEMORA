using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRChat/Car/Car Auto Route Starter")]
public class CarAutoRouteStarter : UdonSharpBehaviour
{
    public CarVehicleController vehicleController;
    public Collider interactionCollider;
    public Renderer indicatorRenderer;
    public Color idleColor = new Color(0.20f, 0.80f, 0.35f, 1f);
    public Color runningColor = new Color(1.00f, 0.60f, 0.15f, 1f);
    public Color disabledColor = new Color(0.25f, 0.25f, 0.25f, 1f);

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

        bool canUse = vehicleController != null && vehicleController.CanLocalPlayerToggleVehicle();
        if (interactionCollider != null)
        {
            interactionCollider.enabled = canUse;
        }

        _lastCanUse = canUse;
        if (indicatorRenderer != null && indicatorRenderer.material != null)
        {
            Color color = disabledColor;
            if (canUse)
            {
                color = vehicleController != null && vehicleController.IsRouteRunning() ? runningColor : idleColor;
            }

            indicatorRenderer.material.color = color;
        }
    }

    private void Update()
    {
        bool canUse = vehicleController != null && vehicleController.CanLocalPlayerToggleVehicle();
        if (interactionCollider != null && _lastCanUse != canUse)
        {
            interactionCollider.enabled = canUse;
        }

        _lastCanUse = canUse;
        if (indicatorRenderer != null && indicatorRenderer.material != null)
        {
            Color color = disabledColor;
            if (canUse)
            {
                color = vehicleController != null && vehicleController.IsRouteRunning() ? runningColor : idleColor;
            }

            indicatorRenderer.material.color = color;
        }
    }

public override void Interact()
    {
        bool hasController = vehicleController != null;
        bool canToggle = hasController && vehicleController.CanLocalPlayerToggleVehicle();
        bool routeRunning = hasController && vehicleController.IsRouteRunning();
        bool hasRoute = hasController && vehicleController.HasRouteAvailable();
        Debug.Log("[CarAutoRouteStarter] Interact called. object=" + gameObject.name +
                  ", hasController=" + hasController +
                  ", canToggle=" + canToggle +
                  ", routeRunning=" + routeRunning +
                  ", hasRoute=" + hasRoute);

        if (vehicleController == null || !vehicleController.CanLocalPlayerToggleVehicle())
        {
            return;
        }

        vehicleController.ToggleVehicle();
    }
}
