using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FireflyCatchBoxSpawner : UdonSharpBehaviour
{
    [SerializeField] private GameObject[] railTargets;

    public bool IsFireflyCaptured(int fireflyIndex)
    {
        return false;
    }

    public void TryCapture(int fireflyIndex)
    {
    }

    public GameObject[] GetRailTargets()
    {
        return railTargets;
    }

    public override void OnDeserialization()
    {
    }
}
