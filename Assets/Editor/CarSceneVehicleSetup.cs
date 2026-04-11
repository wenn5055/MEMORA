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
    private const string WorldName = "VRCWorld";
    private const string DefaultSpawnName = "DefaultSpawn";
    private const string DriverSeatName = "Driver_Seat";
    private const string RouteRootName = "CarRoute_Main";
    private const string RouteStarterName = "DashboardRouteStarter";
    private const string CarVehicleControllerScriptPath = "Assets/Scripts/CarVehicleController.cs";
    private const string CarVehicleControllerProgramAssetPath = "Assets/Scripts/CarVehicleController.asset";
    private const string CarSeatStationScriptPath = "Assets/Scripts/CarSeatStation.cs";
    private const string CarSeatStationProgramAssetPath = "Assets/Scripts/CarSeatStation.asset";
    private const string CarAutoRouteStarterScriptPath = "Assets/Scripts/CarAutoRouteStarter.cs";
    private const string CarAutoRouteStarterProgramAssetPath = "Assets/Scripts/CarAutoRouteStarter.asset";

    private static readonly string[] PassengerSeatNames =
    {
        "Passenger_Seat_Front",
        "Passenger_Seat_Back_Right",
        "Passenger_Seat_Back_Left",
    };

    private static readonly string[] RouteWaypointNames =
    {
        "WP0_Depart",
        "WP1_AisleExit",
        "WP2_Connector",
        "WP3_Merge",
        "WP4_Cruise",
        "WP5_Stop",
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
        EnsureProgramAsset(typeof(CarAutoRouteStarter), CarAutoRouteStarterScriptPath, CarAutoRouteStarterProgramAssetPath);
        AssetDatabase.SaveAssets();
        UdonSharpCompilerV1.CompileSync();

        GameObject worldObject = GameObject.Find(WorldName);
        if (worldObject == null)
        {
            Debug.LogError("CarSceneVehicleSetup: Could not find VRCWorld in CarScene.");
            return;
        }

        VRCSceneDescriptor sceneDescriptor = worldObject.GetComponent<VRCSceneDescriptor>();
        if (sceneDescriptor == null)
        {
            Debug.LogError("CarSceneVehicleSetup: VRCWorld is missing VRCSceneDescriptor.");
            return;
        }

        EnsureSceneSpawn(sceneDescriptor);
        EnsureReferenceCamera(sceneDescriptor);

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
            Undo.RegisterCreatedObjectUndo(vehicleRoot, "Create car vehicle root");
        }

        vehicleRoot.transform.position = carObject.transform.position;
        vehicleRoot.transform.localScale = Vector3.one;

        if (carObject.transform.parent != vehicleRoot.transform)
        {
            Quaternion carLocalRotation = carObject.transform.localRotation;
            Vector3 carLocalScale = carObject.transform.localScale;

            Undo.SetTransformParent(carObject.transform, vehicleRoot.transform, "Parent car under vehicle root");
            carObject.transform.localPosition = Vector3.zero;
            carObject.transform.localRotation = carLocalRotation;
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

        controller.driveMode = CarDriveMode.AutoRoute;
        controller.maxForwardSpeed = 6.25f;
        controller.maxReverseSpeed = 4f;
        controller.acceleration = 4.5f;
        controller.brakeDeceleration = 7.5f;
        controller.steerRate = 60f;
        controller.drag = 2.5f;
        controller.engineStartDelay = 0.15f;
        controller.stopWhenDriverExits = true;
        controller.allowMasterFallback = true;
        controller.loopRoute = false;
        controller.waypointReachDistance = 2.5f;
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
        controller.routePoints = EnsureAutoRoute(controller, vehicleRoot.transform);

        if (controller.routePoints != null && controller.routePoints.Length > 0)
        {
            AlignVehicleToFirstWaypoint(vehicleRoot.transform, controller.routePoints[0]);
        }

        SetupRouteStarter(driverSeat, controller);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(vehicleRoot);
        EditorUtility.SetDirty(sceneDescriptor);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("CarSceneVehicleSetup: Car vehicle system has been configured.");
    }

    private static void EnsureSceneSpawn(VRCSceneDescriptor sceneDescriptor)
    {
        Transform spawn = null;

        if (sceneDescriptor.spawns != null)
        {
            for (int i = 0; i < sceneDescriptor.spawns.Length; i++)
            {
                if (sceneDescriptor.spawns[i] != null)
                {
                    spawn = sceneDescriptor.spawns[i];
                    break;
                }
            }
        }

        if (spawn == null)
        {
            Transform existingSpawn = sceneDescriptor.transform.Find(DefaultSpawnName);
            if (existingSpawn != null)
            {
                spawn = existingSpawn;
            }
        }

        if (spawn == null)
        {
            GameObject spawnObject = new GameObject(DefaultSpawnName);
            Undo.RegisterCreatedObjectUndo(spawnObject, "Create default VRChat spawn");
            Transform spawnTransform = spawnObject.transform;
            spawnTransform.SetParent(sceneDescriptor.transform, false);
            spawnTransform.position = sceneDescriptor.transform.position + Vector3.up * 0.1f;
            spawnTransform.rotation = sceneDescriptor.transform.rotation;
            spawn = spawnTransform;
        }

        sceneDescriptor.spawns = new[] { spawn };
        sceneDescriptor.SpawnLocation = spawn;
        EditorUtility.SetDirty(sceneDescriptor);
    }

    private static void EnsureReferenceCamera(VRCSceneDescriptor sceneDescriptor)
    {
        GameObject referenceCamera = sceneDescriptor.ReferenceCamera;
        if (referenceCamera == null)
        {
            referenceCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        if (referenceCamera == null)
        {
            Camera fallbackCamera = Object.FindObjectOfType<Camera>();
            if (fallbackCamera != null)
            {
                referenceCamera = fallbackCamera.gameObject;
            }
        }

        if (referenceCamera == null)
        {
            Debug.LogWarning("CarSceneVehicleSetup: Could not find a camera to assign as VRC Scene Descriptor reference camera.");
            return;
        }

        sceneDescriptor.ReferenceCamera = referenceCamera;
        EditorUtility.SetDirty(sceneDescriptor);
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

        if (station != null)
        {
            station.PlayerMobility = VRC.SDKBase.VRCStation.Mobility.ImmobilizeForVehicle;
            station.disableStationExit = true;
            station.canUseStationFromStation = false;
        }

        ConfigureStationCallbacks(station);
        ConfigureStationCallbacks(station);
        EditorUtility.SetDirty(seatStation);
        return seatStation;
    }

private static void SetupRouteStarter(CarSeatStation driverSeat, CarVehicleController controller)
    {
        Transform existing = driverSeat.transform.Find(RouteStarterName);
        GameObject starterObject = existing != null ? existing.gameObject : null;
        Transform seatAnchor = driverSeat.transform.Find("Seat");
        Transform routeParent = seatAnchor != null ? seatAnchor : driverSeat.transform;

        if (starterObject == null || starterObject.GetComponent<BoxCollider>() == null || starterObject.GetComponent<MeshRenderer>() == null)
        {
            if (starterObject != null)
            {
                Undo.DestroyObjectImmediate(starterObject);
            }

            starterObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            starterObject.name = RouteStarterName;
            Undo.RegisterCreatedObjectUndo(starterObject, "Create route starter");
        }

        Undo.SetTransformParent(starterObject.transform, routeParent, "Parent route starter");
        starterObject.layer = driverSeat.gameObject.layer;
        starterObject.transform.localPosition = new Vector3(0f, 0.06f, 0.32f);
        starterObject.transform.localRotation = Quaternion.identity;
        starterObject.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);

        BoxCollider collider = starterObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = Undo.AddComponent<BoxCollider>(starterObject);
        }

        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = new Vector3(4f, 4f, 4f);

        CarAutoRouteStarter starter = starterObject.GetComponent<CarAutoRouteStarter>();
        if (starter != null && UdonSharpEditorUtility.GetBackingUdonBehaviour(starter) == null)
        {
            UdonSharpUndo.DestroyImmediate(starter);
            starter = null;
        }

        if (starter == null)
        {
            starter = UdonSharpUndo.AddComponent<CarAutoRouteStarter>(starterObject);
        }

        starter.vehicleController = controller;
        starter.interactionCollider = collider;
        starter.indicatorRenderer = starterObject.GetComponent<Renderer>();
        if (starter.indicatorRenderer != null)
        {
            starter.indicatorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            starter.indicatorRenderer.receiveShadows = false;
        }

        UdonBehaviour starterBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(starter);
        if (starterBehaviour != null)
        {
            starterBehaviour.SyncMethod = Networking.SyncType.None;
            SetObjectField(starterBehaviour, "interactionText", "Start Route");
            SetObjectField(starterBehaviour, "interactText", "Start Route");
            SetObjectField(starterBehaviour, "proximity", 4f);
            EditorUtility.SetDirty(starterBehaviour);
        }

        EditorUtility.SetDirty(starter);
        EditorUtility.SetDirty(starterObject);
    }

    private static Transform[] EnsureAutoRoute(CarVehicleController controller, Transform vehicleRoot)
    {
        GameObject routeRoot = GameObject.Find(RouteRootName);
        if (routeRoot == null)
        {
            routeRoot = new GameObject(RouteRootName);
            Undo.RegisterCreatedObjectUndo(routeRoot, "Create car route root");
        }

        routeRoot.transform.position = Vector3.zero;
        routeRoot.transform.rotation = Quaternion.identity;
        routeRoot.transform.localScale = Vector3.one;

        Vector3[] positions = BuildRoutePositions(vehicleRoot.position, controller.rideHeight);
        Transform[] waypoints = new Transform[RouteWaypointNames.Length];

        for (int i = 0; i < RouteWaypointNames.Length; i++)
        {
            Transform waypoint = routeRoot.transform.Find(RouteWaypointNames[i]);
            if (waypoint == null)
            {
                GameObject waypointObject = new GameObject(RouteWaypointNames[i]);
                Undo.RegisterCreatedObjectUndo(waypointObject, $"Create {RouteWaypointNames[i]}");
                waypoint = waypointObject.transform;
                waypoint.SetParent(routeRoot.transform, false);
            }

            waypoint.position = positions[i];
            waypoint.rotation = Quaternion.identity;
            waypoints[i] = waypoint;
            EditorUtility.SetDirty(waypoint.gameObject);
        }

        EditorUtility.SetDirty(routeRoot);
        return waypoints;
    }

    private static Vector3[] BuildRoutePositions(Vector3 vehiclePosition, float rideHeight)
    {
        Vector3[] positions = new Vector3[RouteWaypointNames.Length];
        positions[0] = AdjustWaypointHeight(new Vector3(954.4f, vehiclePosition.y, 410.2f), rideHeight);
        positions[1] = AdjustWaypointHeight(new Vector3(961.8f, vehiclePosition.y, 408.4f), rideHeight);
        positions[2] = AdjustWaypointHeight(new Vector3(970.9f, vehiclePosition.y, 406.7f), rideHeight);
        positions[3] = AdjustWaypointHeight(new Vector3(1007.2f, vehiclePosition.y, 406.7f), rideHeight);
        positions[4] = AdjustWaypointHeight(new Vector3(1008.4f, vehiclePosition.y, 334.0f), rideHeight);
        positions[5] = AdjustWaypointHeight(new Vector3(1011.6f, vehiclePosition.y, 286.0f), rideHeight);
        return positions;
    }

    private static Vector3 AdjustWaypointHeight(Vector3 position, float rideHeight)
    {
        position.y = SampleGroundHeight(position) + rideHeight;
        return position;
    }

    private static void AlignVehicleToFirstWaypoint(Transform vehicleRoot, Transform firstWaypoint)
    {
        if (firstWaypoint == null)
        {
            return;
        }

        Vector3 direction = firstWaypoint.position - vehicleRoot.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        vehicleRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        EditorUtility.SetDirty(vehicleRoot.gameObject);
    }

    private static void ConfigureStationCallbacks(SDK3VRCStation station)
    {
        if (station == null)
        {
            return;
        }

        SetObjectField(station, "OnLocalPlayerEnterStation", nameof(CarSeatStation.LegacyStationEntered));
        SetObjectField(station, "OnLocalPlayerExitStation", nameof(CarSeatStation.LegacyStationExited));
        SetObjectField(station, "OnRemotePlayerEnterStation", string.Empty);
        SetObjectField(station, "OnRemotePlayerExitStation", string.Empty);
        station.PlayerMobility = VRC.SDKBase.VRCStation.Mobility.ImmobilizeForVehicle;
        station.disableStationExit = true;
        station.canUseStationFromStation = false;
        EditorUtility.SetDirty(station);
        EditorUtility.SetDirty(station);
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

    private static void SetObjectField(Object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (field == null)
        {
            return;
        }

        field.SetValue(target, value);
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

    private static float SampleGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        Vector3 origin = position + Vector3.up * 12f;

        if (Physics.Raycast(origin, Vector3.down, out hit, 40f, ~0))
        {
            return hit.point.y;
        }

        return position.y;
    }
}

