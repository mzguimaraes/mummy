using UnityEngine;

public class RideControlTest : MonoBehaviour
{
    [Header("Test Setup")]
    public RideControlSystem rideControl;
    public TurntableController[] turntables;
    public SwitchTrackController[] switchTracks;
    
    [Header("Test Controls")]
    public bool autoTest = false;
    public float testInterval = 10f;
    public bool showDebugInfo = true;
    
    [Header("Debug Info")]
    public string currentMode;
    public int activeTrains;
    public float rideProgress;
    public bool isRideActive;
    public string[] blockStatus;
    
    private float lastTestTime = 0f;
    private int testSequence = 0;
    
    void Start()
    {
        if (rideControl == null)
        {
            rideControl = FindObjectOfType<RideControlSystem>();
        }
        
        if (rideControl != null)
        {
            // Subscribe to events
            rideControl.OnRideModeChanged += OnRideModeChanged;
            rideControl.OnTrainDispatched += OnTrainDispatched;
            rideControl.OnTrainCompleted += OnTrainCompleted;
            rideControl.OnRideStarted += OnRideStarted;
            rideControl.OnRideStopped += OnRideStopped;
            rideControl.OnEmergencyStop += OnEmergencyStop;
            rideControl.OnRideProgressChanged += OnRideProgressChanged;
            
            UpdateDebugInfo();
        }
        else
        {
            Debug.LogError("No RideControlSystem found in scene!");
        }
    }
    
    void Update()
    {
        UpdateDebugInfo();
        HandleTestInput();
        
        if (autoTest)
        {
            RunAutoTest();
        }
    }
    
    void UpdateDebugInfo()
    {
        if (rideControl != null)
        {
            currentMode = rideControl.currentMode.ToString();
            activeTrains = rideControl.activeTrains;
            rideProgress = rideControl.rideProgress;
            isRideActive = rideControl.isRideActive;
            
            // Update block status
            blockStatus = new string[rideControl.blockSections.Length];
            for (int i = 0; i < rideControl.blockSections.Length; i++)
            {
                var block = rideControl.blockSections[i];
                blockStatus[i] = $"Block {i}: {(block.isOccupied ? "OCCUPIED" : "CLEAR")}";
            }
        }
    }
    
    void HandleTestInput()
    {
        if (rideControl == null) return;
        
        // Ride control
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleRide();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartReverseSequence();
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            EmergencyStop();
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMaintenance();
        }
        
        // Turntable controls
        if (Input.GetKeyDown(KeyCode.T))
        {
            RotateTurntables();
        }
        
        // Switch track controls
        if (Input.GetKeyDown(KeyCode.S))
        {
            TestSwitchTracks();
        }
        
        // Auto test toggle
        if (Input.GetKeyDown(KeyCode.A))
        {
            ToggleAutoTest();
        }
    }
    
    void RunAutoTest()
    {
        if (Time.time - lastTestTime > testInterval)
        {
            switch (testSequence)
            {
                case 0:
                    if (!isRideActive)
                    {
                        StartRide();
                    }
                    break;
                case 1:
                    StartReverseSequence();
                    break;
                case 2:
                    RotateTurntables();
                    break;
                case 3:
                    TestSwitchTracks();
                    break;
                case 4:
                    if (isRideActive)
                    {
                        StopRide();
                    }
                    break;
            }
            
            testSequence = (testSequence + 1) % 5;
            lastTestTime = Time.time;
        }
    }
    
    public void StartRide()
    {
        if (rideControl != null)
        {
            rideControl.StartRide();
            Debug.Log("Starting ride...");
        }
    }
    
    public void StopRide()
    {
        if (rideControl != null)
        {
            rideControl.StopRide();
            Debug.Log("Stopping ride...");
        }
    }
    
    public void ToggleRide()
    {
        if (isRideActive)
        {
            StopRide();
        }
        else
        {
            StartRide();
        }
    }
    
    public void StartReverseSequence()
    {
        if (rideControl != null)
        {
            rideControl.StartReverseSequence();
            Debug.Log("Starting reverse sequence...");
        }
    }
    
    public void EmergencyStop()
    {
        if (rideControl != null)
        {
            rideControl.EmergencyStop();
            Debug.Log("EMERGENCY STOP!");
        }
    }
    
    public void ToggleMaintenance()
    {
        if (rideControl != null)
        {
            bool maintenance = rideControl.currentMode == RideControlSystem.RideMode.Maintenance;
            rideControl.SetMaintenanceMode(!maintenance);
            Debug.Log($"Maintenance mode: {!maintenance}");
        }
    }
    
    public void RotateTurntables()
    {
        foreach (TurntableController turntable in turntables)
        {
            if (turntable != null)
            {
                turntable.RotateClockwise();
                Debug.Log($"Rotating turntable: {turntable.name}");
            }
        }
    }
    
    public void TestSwitchTracks()
    {
        foreach (SwitchTrackController switchTrack in switchTracks)
        {
            if (switchTrack != null)
            {
                int nextTrack = (switchTrack.currentTrackIndex + 1) % switchTrack.trackSections.Length;
                switchTrack.SwitchToTrack(nextTrack);
                Debug.Log($"Switching track: {switchTrack.name} to track {nextTrack}");
            }
        }
    }
    
    public void ToggleAutoTest()
    {
        autoTest = !autoTest;
        testSequence = 0;
        lastTestTime = Time.time;
        Debug.Log($"Auto test: {autoTest}");
    }
    
    void OnRideModeChanged(RideControlSystem.RideMode newMode)
    {
        Debug.Log($"Ride mode changed to: {newMode}");
    }
    
    void OnTrainDispatched(int trainID)
    {
        Debug.Log($"Train {trainID} dispatched");
    }
    
    void OnTrainCompleted(int trainID)
    {
        Debug.Log($"Train {trainID} completed");
    }
    
    void OnRideStarted()
    {
        Debug.Log("Ride started");
    }
    
    void OnRideStopped()
    {
        Debug.Log("Ride stopped");
    }
    
    void OnEmergencyStop()
    {
        Debug.LogError("EMERGENCY STOP ACTIVATED!");
    }
    
    void OnRideProgressChanged(float progress)
    {
        Debug.Log($"Ride progress: {progress:F1}%");
    }
    
    void OnDestroy()
    {
        if (rideControl != null)
        {
            rideControl.OnRideModeChanged -= OnRideModeChanged;
            rideControl.OnTrainDispatched -= OnTrainDispatched;
            rideControl.OnTrainCompleted -= OnTrainCompleted;
            rideControl.OnRideStarted -= OnRideStarted;
            rideControl.OnRideStopped -= OnRideStopped;
            rideControl.OnEmergencyStop -= OnEmergencyStop;
            rideControl.OnRideProgressChanged -= OnRideProgressChanged;
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || rideControl == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 600));
        GUILayout.Label("Ride Control System Test", GUI.skin.box);
        
        // Status info
        GUILayout.Label($"Mode: {currentMode}");
        GUILayout.Label($"Active Trains: {activeTrains}");
        GUILayout.Label($"Ride Progress: {rideProgress:F1}%");
        GUILayout.Label($"Ride Active: {isRideActive}");
        
        GUILayout.Space(10);
        
        // Block status
        GUILayout.Label("Block Status:");
        if (blockStatus != null)
        {
            foreach (string status in blockStatus)
            {
                GUILayout.Label(status);
            }
        }
        
        GUILayout.Space(10);
        
        // Ride controls
        GUILayout.Label("Ride Controls:");
        if (GUILayout.Button($"{(isRideActive ? "Stop" : "Start")} Ride (Space)"))
        {
            ToggleRide();
        }
        
        if (GUILayout.Button("Start Reverse Sequence (R)"))
        {
            StartReverseSequence();
        }
        
        if (GUILayout.Button("Emergency Stop (E)"))
        {
            EmergencyStop();
        }
        
        if (GUILayout.Button($"Toggle Maintenance (M)"))
        {
            ToggleMaintenance();
        }
        
        GUILayout.Space(10);
        
        // Turntable controls
        GUILayout.Label("Turntable Controls:");
        if (GUILayout.Button("Rotate Turntables (T)"))
        {
            RotateTurntables();
        }
        
        // Switch track controls
        GUILayout.Label("Switch Track Controls:");
        if (GUILayout.Button("Test Switch Tracks (S)"))
        {
            TestSwitchTracks();
        }
        
        GUILayout.Space(10);
        
        // Auto test
        if (GUILayout.Button($"Auto Test: {(autoTest ? "ON" : "OFF")} (A)"))
        {
            ToggleAutoTest();
        }
        
        GUILayout.Space(10);
        
        // Key bindings
        GUILayout.Label("Key Bindings:", GUI.skin.box);
        GUILayout.Label("Space - Toggle Ride");
        GUILayout.Label("R - Reverse Sequence");
        GUILayout.Label("E - Emergency Stop");
        GUILayout.Label("M - Toggle Maintenance");
        GUILayout.Label("T - Rotate Turntables");
        GUILayout.Label("S - Test Switch Tracks");
        GUILayout.Label("A - Toggle Auto Test");
        
        GUILayout.EndArea();
    }
    
    void OnDrawGizmos()
    {
        if (rideControl == null) return;
        
        // Draw test sequence path
        Gizmos.color = Color.magenta;
        for (int i = 0; i < testSequence; i++)
        {
            Vector3 position = transform.position + Vector3.up * (i * 2f);
            Gizmos.DrawWireSphere(position, 0.5f);
        }
        
        // Draw block sections
        foreach (var block in rideControl.blockSections)
        {
            if (block.waypoints != null && block.waypoints.Length > 1)
            {
                Gizmos.color = block.isOccupied ? Color.red : Color.green;
                
                for (int i = 0; i < block.waypoints.Length - 1; i++)
                {
                    if (block.waypoints[i] != null && block.waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(block.waypoints[i].position, block.waypoints[i + 1].position);
                    }
                }
            }
        }
    }
} 