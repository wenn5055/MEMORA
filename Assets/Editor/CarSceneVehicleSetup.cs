using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using VRC.SDK3.Components;
using VRC.SDKBase;
using SDK3VRCStation = VRC.SDK3.Components.VRCStation;
using VRC.Udon;

public static class CarSceneVehicleSetup
{
    private const string ScenePath = "Assets/Scenes/Experiment Daphne/CarScene.unity";
    private const string VehicleRootName = "CarVehicleRoot";
    private const string CarName = "Car_reduced";
    private const string DriverSeatName = "Driver_Seat";
    private const string CarVehicleControllerScriptPath = "Assets/Scripts/CarVehicleController.cs";
    private const string CarVehicleControllerProgramAssetPath = "Assets/Scripts/CarVehicleController.asset";
    private const string CarSeatStationScriptPath = "Assets/Scripts/CarSeatStation.cs";
    private const string CarSeatStationProgramAssetPath = "Assets/Scripts/CarSeatStation.asset";

    private static readonly string[] PassengerSeatNames =
    {
        "Passenger_Seat_Front",
        "Passenger_Seat_Back_Right",
        "Passenger_Seat_Back_Left",
    };

    [MenuItem("Tools/VRChat/Setup Car Scene Vehicle")]
    public static void SetupCarSceneVehicle()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        EnsureProgramAsset(typeof(CarVehicleController), CarVehicleControllerScriptPath, CarVehicleControllerProgramAssetPath);
        EnsureProgramAsset(typeof(CarSeatStation), CarSeatStationScriptPath, CarSeatStationProgramAssetPath);
        AssetDatabase.SaveAssets();
        UdonSharpCompilerV1.CompileSync();

        GameObject carObject = GameObject.Find(CarName);
        if (carObject == null)
        {
            Debug.LogError("CarSceneVehicleSetup: Could not find Car_reduced in CarScene.");
            return;
        }

        GameObject vehicleRoot = GameObject.Find(VehicleRootName);
        if (vehicleRoot == null)
        {
            vehicleRoot = new GameObject(VehicleRootName);
        }

        Undo.RegisterCreatedObjectUndo(vehicleRoot, "Create car vehicle root");
        vehicleRoot.transform.position = carObject.transform.position;
        vehicleRoot.transform.rotation = Quaternion.identity;
        vehicleRoot.transform.localScale = Vector3.one;

        if (carObject.transform.parent != vehicleRoot.transform)
        {
            Quaternion carWorldRotation = carObject.transform.rotation;
            Vector3 carLocalScale = carObject.transform.localScale;

            Undo.SetTransformParent(carObject.transform, vehicleRoot.transform, "Parent car under vehicle root");
            carObject.transform.localPosition = Vector3.zero;
            carObject.transform.localRotation = carWorldRotation;
            carObject.transform.localScale = carLocalScale;
        }

        VRCObjectSync objectSync = vehicleRoot.GetComponent<VRCObjectSync>();
        if (objectSync == null)
        {
            objectSync = Undo.AddComponent<VRCObjectSync>(vehicleRoot);
        }

        CarVehicleController controller = vehicleRoot.GetComponent<CarVehicleController>();
        if (controller != null && UdonSharpEditorUtility.GetBackingUdonBehaviour(controller) == null)
        {
            UdonSharpUndo.DestroyImmediate(controller);
            controller = null;
        }

        if (controller == null)
        {
            controller = UdonSharpUndo.AddComponent<CarVehicleController>(vehicleRoot);
        }

        UdonBehaviour controllerBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
        if (controllerBehaviour != null)
        {
            controllerBehaviour.SyncMethod = Networking.SyncType.Continuous;
            EditorUtility.SetDirty(controllerBehaviour);
        }

        controller.driveMode = CarDriveMode.Manual;
        controller.maxForwardSpeed = 10f;
        controller.maxReverseSpeed = 4f;
        controller.acceleration = 6f;
        controller.brakeDeceleration = 10f;
        controller.steerRate = 65f;
        controller.drag = 3f;
        controller.engineStartDelay = 0.2f;
        controller.stopWhenDriverExits = true;
        controller.allowMasterFallback = true;
        controller.loopRoute = true;
        controller.waypointReachDistance = 1.5f;
        controller.groundProbeHeight = 3f;
        controller.groundProbeDistance = 10f;
        controller.rideHeight = SampleRideHeight(vehicleRoot.transform.position);

        CarSeatStation driverSeat = SetupSeat(DriverSeatName, controller, CarSeatRole.Driver);
        if (driverSeat == null)
        {
            Debug.LogError("CarSceneVehicleSetup: Could not configure Driver_Seat.");
            return;
        }

        CarSeatStation[] passengerSeats = new CarSeatStation[PassengerSeatNames.Length];
        for (int i = 0; i < PassengerSeatNames.Length; i++)
        {
            passengerSeats[i] = SetupSeat(PassengerSeatNames[i], controller, CarSeatRole.Passenger);
        }

        controller.driverSeat = driverSeat;
        controller.passengerSeats = passengerSeats;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(vehicleRoot);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("CarSceneVehicleSetup: Car vehicle system has been configured.");
    }

    private static CarSeatStation SetupSeat(string seatName, CarVehicleController controller, CarSeatRole seatRole)
    {
        GameObject seatObject = GameObject.Find(seatName);
        if (seatObject == null)
        {
            Debug.LogError($"CarSceneVehicleSetup: Could not find seat '{seatName}'.");
            return null;
        }

        UdonBehaviour[] udonBehaviours = seatObject.GetComponents<UdonBehaviour>();
        for (int i = 0; i < udonBehaviours.Length; i++)
        {
            if (UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviours[i]) == null)
            {
                Undo.DestroyObjectImmediate(udonBehaviours[i]);
            }
        }

        CarSeatStation seatStation = seatObject.GetComponent<CarSeatStation>();
        if (seatStation != null && UdonSharpEditorUtility.GetBackingUdonBehaviour(seatStation) == null)
        {
            UdonSharpUndo.DestroyImmediate(seatStation);
            seatStation = null;
        }

        if (seatStation == null)
        {
            seatStation = UdonSharpUndo.AddComponent<CarSeatStation>(seatObject);
        }

        SDK3VRCStation station = seatObject.GetComponent<SDK3VRCStation>();
        Collider collider = seatObject.GetComponent<Collider>();

        seatStation.seatRole = seatRole;
        seatStation.vehicleController = controller;
        seatStation.seatCollider = collider;
        seatStation.station = station;

        ConfigureStationCallbacks(station);
        EditorUtility.SetDirty(seatStation);
        return seatStation;
    }

    private static void ConfigureStationCallbacks(SDK3VRCStation station)
    {
        if (station == null)
        {
            return;
        }

        SetStationCallbackField(station, "OnLocalPlayerEnterStation", nameof(CarSeatStation.LegacyStationEntered));
        SetStationCallbackField(station, "OnLocalPlayerExitStation", nameof(CarSeatStation.LegacyStationExited));
        SetStationCallbackField(station, "OnRemotePlayerEnterStation", string.Empty);
        SetStationCallbackField(station, "OnRemotePlayerExitStation", string.Empty);
        EditorUtility.SetDirty(station);
    }

    private static void SetStationCallbackField(SDK3VRCStation station, string fieldName, string value)
    {
        FieldInfo field = typeof(SDK3VRCStation).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (field != null)
        {
            field.SetValue(station, value);
        }
    }

    private static void EnsureProgramAsset(System.Type behaviourType, string scriptPath, string programAssetPath)
    {
        if (UdonSharpProgramAsset.GetProgramAssetForClass(behaviourType) != null)
        {
            return;
        }

        AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);

        MonoScript sourceScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (sourceScript == null)
        {
            Debug.LogError($"CarSceneVehicleSetup: Could not load script at '{scriptPath}'.");
            return;
        }

        UdonSharpProgramAsset programAsset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(programAssetPath);
        if (programAsset == null)
        {
            programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            programAsset.name = System.IO.Path.GetFileNameWithoutExtension(programAssetPath);
            programAsset.sourceCsScript = sourceScript;
            AssetDatabase.CreateAsset(programAsset, programAssetPath);
        }
        else if (programAsset.sourceCsScript != sourceScript)
        {
            programAsset.sourceCsScript = sourceScript;
        }

        EditorUtility.SetDirty(programAsset);
    }

    private static float SampleRideHeight(Vector3 position)
    {
        RaycastHit hit;
        Vector3 origin = position + Vector3.up * 3f;

        if (Physics.Raycast(origin, Vector3.down, out hit, 15f, ~0))
        {
            return Mathf.Max(0.1f, position.y - hit.point.y);
        }

        return 0.35f;
    }
}


