using UnityEngine;

public class SwitchTrackTest : MonoBehaviour
{
    [Header("Test Setup")]
    public SwitchTrackController switchTrack;
    public RideVehicleController testVehicle;
    
    [Header("Test Controls")]
    public bool autoTest = false;
    public float testInterval = 5f;
    public int[] testSequence = { 0, 1, 2, 0 };
    
    [Header("Debug Info")]
    public string currentState;
    public int currentTrack;
    public bool isSwitching;
    public bool isLocked;
    
    private int testSequenceIndex = 0;
    private float lastTestTime = 0f;
    
    void Start()
    {
        if (switchTrack == null)
        {
            switchTrack = FindObjectOfType<SwitchTrackController>();
        }
        
        if (switchTrack != null)
        {
            // Subscribe to events
            switchTrack.OnStateChanged += OnSwitchStateChanged;
            switchTrack.OnTrackSwitched += OnTrackSwitched;
            switchTrack.OnSwitchStarted += OnSwitchStarted;
            switchTrack.OnSwitchCompleted += OnSwitchCompleted;
            switchTrack.OnSafetyViolation += OnSafetyViolation;
            
            UpdateDebugInfo();
        }
        else
        {
            Debug.LogError("No SwitchTrackController found in scene!");
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
        if (switchTrack != null)
        {
            currentState = switchTrack.currentState.ToString();
            currentTrack = switchTrack.currentTrackIndex;
            isSwitching = switchTrack.currentState == SwitchTrackController.SwitchState.Switching;
            isLocked = switchTrack.currentState == SwitchTrackController.SwitchState.Locked;
        }
    }
    
    void HandleTestInput()
    {
        if (switchTrack == null) return;
        
        // Track switching controls
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchToTrack(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchToTrack(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchToTrack(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SwitchToTrack(3);
        }
        
        // State controls
        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleLock();
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMaintenance();
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            EmergencyStop();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetError();
        }
        
        // Auto test toggle
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleAutoTest();
        }
    }
    
    void RunAutoTest()
    {
        if (Time.time - lastTestTime > testInterval)
        {
            if (testSequenceIndex < testSequence.Length)
            {
                int targetTrack = testSequence[testSequenceIndex];
                SwitchToTrack(targetTrack);
                testSequenceIndex++;
            }
            else
            {
                testSequenceIndex = 0;
            }
            
            lastTestTime = Time.time;
        }
    }
    
    public void SwitchToTrack(int trackIndex)
    {
        if (switchTrack != null)
        {
            Debug.Log($"Switching to track {trackIndex}");
            bool success = switchTrack.SwitchToTrack(trackIndex);
            
            if (!success)
            {
                Debug.LogWarning($"Failed to switch to track {trackIndex}");
            }
        }
    }
    
    public void ToggleLock()
    {
        if (switchTrack != null)
        {
            if (switchTrack.currentState == SwitchTrackController.SwitchState.Locked)
            {
                switchTrack.UnlockTrack();
                Debug.Log("Unlocking track");
            }
            else
            {
                switchTrack.LockTrack();
                Debug.Log("Locking track");
            }
        }
    }
    
    public void ToggleMaintenance()
    {
        if (switchTrack != null)
        {
            bool maintenance = switchTrack.currentState == SwitchTrackController.SwitchState.Maintenance;
            switchTrack.SetMaintenanceMode(!maintenance);
            Debug.Log($"Maintenance mode: {!maintenance}");
        }
    }
    
    public void EmergencyStop()
    {
        if (switchTrack != null)
        {
            switchTrack.EmergencyStop();
            Debug.Log("Emergency stop activated");
        }
    }
    
    public void ResetError()
    {
        if (switchTrack != null)
        {
            switchTrack.ResetError();
            Debug.Log("Error reset");
        }
    }
    
    public void ToggleAutoTest()
    {
        autoTest = !autoTest;
        testSequenceIndex = 0;
        lastTestTime = Time.time;
        Debug.Log($"Auto test: {autoTest}");
    }
    
    void OnSwitchStateChanged(SwitchTrackController.SwitchState newState)
    {
        Debug.Log($"Switch track state changed to: {newState}");
    }
    
    void OnTrackSwitched(int trackIndex)
    {
        Debug.Log($"Track switched to: {trackIndex}");
    }
    
    void OnSwitchStarted()
    {
        Debug.Log("Switch operation started");
    }
    
    void OnSwitchCompleted()
    {
        Debug.Log("Switch operation completed");
    }
    
    void OnSafetyViolation()
    {
        Debug.LogWarning("Safety violation detected!");
    }
    
    void OnDestroy()
    {
        if (switchTrack != null)
        {
            switchTrack.OnStateChanged -= OnSwitchStateChanged;
            switchTrack.OnTrackSwitched -= OnTrackSwitched;
            switchTrack.OnSwitchStarted -= OnSwitchStarted;
            switchTrack.OnSwitchCompleted -= OnSwitchCompleted;
            switchTrack.OnSafetyViolation -= OnSafetyViolation;
        }
    }
    
    void OnGUI()
    {
        if (switchTrack == null) return;
        
        GUILayout.BeginArea(new Rect(10, 220, 300, 300));
        GUILayout.Label("Switch Track Test Controls", GUI.skin.box);
        
        GUILayout.Label($"State: {currentState}");
        GUILayout.Label($"Current Track: {currentTrack}");
        GUILayout.Label($"Is Switching: {isSwitching}");
        GUILayout.Label($"Is Locked: {isLocked}");
        
        GUILayout.Space(10);
        
        GUILayout.Label("Track Switching:");
        if (GUILayout.Button("Track 0 (1)"))
        {
            SwitchToTrack(0);
        }
        if (GUILayout.Button("Track 1 (2)"))
        {
            SwitchToTrack(1);
        }
        if (GUILayout.Button("Track 2 (3)"))
        {
            SwitchToTrack(2);
        }
        if (GUILayout.Button("Track 3 (4)"))
        {
            SwitchToTrack(3);
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("State Controls:");
        if (GUILayout.Button("Toggle Lock (L)"))
        {
            ToggleLock();
        }
        if (GUILayout.Button("Toggle Maintenance (M)"))
        {
            ToggleMaintenance();
        }
        if (GUILayout.Button("Emergency Stop (E)"))
        {
            EmergencyStop();
        }
        if (GUILayout.Button("Reset Error (R)"))
        {
            ResetError();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button($"Auto Test: {(autoTest ? "ON" : "OFF")} (T)"))
        {
            ToggleAutoTest();
        }
        
        GUILayout.EndArea();
    }
    
    void OnDrawGizmos()
    {
        if (switchTrack != null)
        {
            // Draw test sequence path
            Gizmos.color = Color.yellow;
            for (int i = 0; i < testSequence.Length - 1; i++)
            {
                int current = testSequence[i];
                int next = testSequence[i + 1];
                
                if (current < switchTrack.trackSections.Length && next < switchTrack.trackSections.Length)
                {
                    Vector3 start = switchTrack.trackSections[current].position;
                    Vector3 end = switchTrack.trackSections[next].position;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
} 