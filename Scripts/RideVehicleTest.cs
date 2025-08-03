using UnityEngine;

public class RideVehicleTest : MonoBehaviour
{
    [Header("Test Setup")]
    public RideVehicleController rideVehicle;
    public Transform[] testWaypoints;
    public Transform[] testTrackPoints;
    
    [Header("Test Controls")]
    public bool autoStart = false;
    public float testSpeed = 15f;
    public bool enableEmergencyTest = false;
    
    [Header("Debug Info")]
    public float currentSpeed;
    public float currentProgress;
    public string currentState;
    
    void Start()
    {
        if (rideVehicle == null)
        {
            rideVehicle = FindObjectOfType<RideVehicleController>();
        }
        
        if (rideVehicle != null)
        {
            // Set up waypoints if available
            if (testWaypoints.Length > 0)
            {
                WaypointFollower waypointFollower = rideVehicle.GetComponent<WaypointFollower>();
                if (waypointFollower != null)
                {
                    waypointFollower.waypoints = testWaypoints;
                }
            }
            
            // Set up track points if available
            if (testTrackPoints.Length > 0)
            {
                CoasterPhysics coasterPhysics = rideVehicle.GetComponent<CoasterPhysics>();
                if (coasterPhysics != null)
                {
                    coasterPhysics.trackPoints = testTrackPoints;
                }
            }
            
            // Subscribe to events
            rideVehicle.OnRideStateChanged += OnRideStateChanged;
            rideVehicle.OnSpeedChanged += OnSpeedChanged;
            rideVehicle.OnEmergencyStop += OnEmergencyStop;
            
            if (autoStart)
            {
                StartRide();
            }
        }
        else
        {
            Debug.LogError("No RideVehicle found in scene!");
        }
    }
    
    void Update()
    {
        UpdateDebugInfo();
        HandleTestInput();
    }
    
    void UpdateDebugInfo()
    {
        if (rideVehicle != null)
        {
            currentSpeed = rideVehicle.currentSpeed;
            currentProgress = rideVehicle.GetProgress();
            currentState = rideVehicle.currentState.ToString();
        }
    }
    
    void HandleTestInput()
    {
        if (rideVehicle == null) return;
        
        // Test controls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartRide();
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopRide();
        }
        
        if (Input.GetKeyDown(KeyCode.E) && enableEmergencyTest)
        {
            EmergencyStop();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetRide();
        }
        
        // Speed control
        if (Input.GetKey(KeyCode.UpArrow))
        {
            rideVehicle.SetSpeed(testSpeed + 5f);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            rideVehicle.SetSpeed(testSpeed - 5f);
        }
    }
    
    public void StartRide()
    {
        if (rideVehicle != null)
        {
            Debug.Log("Starting ride...");
            rideVehicle.StartRide();
        }
    }
    
    public void StopRide()
    {
        if (rideVehicle != null)
        {
            Debug.Log("Stopping ride...");
            rideVehicle.StopRide();
        }
    }
    
    public void EmergencyStop()
    {
        if (rideVehicle != null)
        {
            Debug.Log("EMERGENCY STOP!");
            rideVehicle.EmergencyStop();
        }
    }
    
    public void ResetRide()
    {
        if (rideVehicle != null)
        {
            Debug.Log("Resetting ride...");
            rideVehicle.ResetEmergencyStop();
        }
    }
    
    void OnRideStateChanged(RideVehicleController.RideState newState)
    {
        Debug.Log($"Ride state changed to: {newState}");
    }
    
    void OnSpeedChanged(float newSpeed)
    {
        Debug.Log($"Speed changed to: {newSpeed:F1} m/s");
    }
    
    void OnEmergencyStop()
    {
        Debug.LogError("EMERGENCY STOP ACTIVATED!");
    }
    
    void OnDestroy()
    {
        if (rideVehicle != null)
        {
            rideVehicle.OnRideStateChanged -= OnRideStateChanged;
            rideVehicle.OnSpeedChanged -= OnSpeedChanged;
            rideVehicle.OnEmergencyStop -= OnEmergencyStop;
        }
    }
    
    void OnGUI()
    {
        if (rideVehicle == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Ride Vehicle Test Controls", GUI.skin.box);
        
        GUILayout.Label($"State: {currentState}");
        GUILayout.Label($"Speed: {currentSpeed:F1} m/s");
        GUILayout.Label($"Progress: {currentProgress:F1}%");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Start Ride (Space)"))
        {
            StartRide();
        }
        
        if (GUILayout.Button("Stop Ride (S)"))
        {
            StopRide();
        }
        
        if (enableEmergencyTest && GUILayout.Button("Emergency Stop (E)"))
        {
            EmergencyStop();
        }
        
        if (GUILayout.Button("Reset (R)"))
        {
            ResetRide();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Arrow Keys: Adjust Speed");
        
        GUILayout.EndArea();
    }
    
    void OnDrawGizmos()
    {
        // Draw test waypoints
        if (testWaypoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < testWaypoints.Length; i++)
            {
                if (testWaypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(testWaypoints[i].position, 0.5f);
                    
                    if (i < testWaypoints.Length - 1 && testWaypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(testWaypoints[i].position, testWaypoints[i + 1].position);
                    }
                }
            }
        }
        
        // Draw test track points
        if (testTrackPoints != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < testTrackPoints.Length; i++)
            {
                if (testTrackPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(testTrackPoints[i].position, 0.3f);
                    
                    if (i < testTrackPoints.Length - 1 && testTrackPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(testTrackPoints[i].position, testTrackPoints[i + 1].position);
                    }
                }
            }
        }
    }
} 