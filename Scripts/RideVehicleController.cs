using UnityEngine;
using System.Collections;

public class RideVehicleController : MonoBehaviour
{
    [Header("Vehicle Components")]
    public CoasterPhysics coasterPhysics;
    public WaypointFollower waypointFollower;
    
    [Header("Vehicle Settings")]
    public int maxPassengers = 16;
    public float vehicleLength = 8f;
    public float vehicleWidth = 2f;
    public float vehicleHeight = 2.5f;
    
    [Header("Safety Systems")]
    public bool emergencyBrakeEnabled = true;
    public float emergencyBrakeForce = 15f;
    public float maxSafeSpeed = 35f;
    public float maxSafeAcceleration = 20f;
    public float maxSafeDeceleration = 25f;
    
    [Header("Ride States")]
    public enum RideState
    {
        Loading,
        Ready,
        Moving,
        Braking,
        Stopped,
        EmergencyStop
    }
    
    [Header("Current State")]
    public RideState currentState = RideState.Loading;
    public float currentSpeed;
    public float currentAcceleration;
    
    [Header("Passenger Experience")]
    public Transform[] passengerSeats;
    public float gForceThreshold = 3f;
    public bool simulatePassengerReactions = true;
    
    private Rigidbody rb;
    private bool isEmergencyStop = false;
    private float lastSpeed = 0f;
    private float lastAcceleration = 0f;
    
    // Events
    public System.Action<RideState> OnRideStateChanged;
    public System.Action<float> OnSpeedChanged;
    public System.Action<float> OnAccelerationChanged;
    public System.Action OnEmergencyStop;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Get or add required components
        if (coasterPhysics == null)
            coasterPhysics = GetComponent<CoasterPhysics>();
        if (waypointFollower == null)
            waypointFollower = GetComponent<WaypointFollower>();
            
        SetupVehicle();
        ChangeRideState(RideState.Loading);
    }
    
    void SetupVehicle()
    {
        // Setup Rigidbody
        rb.mass = 2000f; // Base mass + passenger mass
        rb.drag = 0.1f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        
        // Add colliders if not present
        if (GetComponent<Collider>() == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(vehicleWidth, vehicleHeight, vehicleLength);
            boxCollider.center = Vector3.zero;
        }
    }
    
    void Update()
    {
        UpdateVehicleMetrics();
        CheckSafetyConditions();
        UpdatePassengerExperience();
    }
    
    void UpdateVehicleMetrics()
    {
        if (rb != null)
        {
            currentSpeed = rb.velocity.magnitude;
            currentAcceleration = (currentSpeed - lastSpeed) / Time.deltaTime;
            
            // Notify listeners
            OnSpeedChanged?.Invoke(currentSpeed);
            OnAccelerationChanged?.Invoke(currentAcceleration);
            
            lastSpeed = currentSpeed;
            lastAcceleration = currentAcceleration;
        }
    }
    
    void CheckSafetyConditions()
    {
        if (!emergencyBrakeEnabled) return;
        
        // Check for emergency conditions
        bool emergencyCondition = false;
        
        if (currentSpeed > maxSafeSpeed)
        {
            emergencyCondition = true;
            Debug.LogWarning($"Emergency: Speed {currentSpeed:F1} exceeds safe limit {maxSafeSpeed}");
        }
        
        if (Mathf.Abs(currentAcceleration) > maxSafeAcceleration)
        {
            emergencyCondition = true;
            Debug.LogWarning($"Emergency: Acceleration {currentAcceleration:F1} exceeds safe limit {maxSafeAcceleration}");
        }
        
        if (emergencyCondition && !isEmergencyStop)
        {
            TriggerEmergencyStop();
        }
    }
    
    void UpdatePassengerExperience()
    {
        if (!simulatePassengerReactions) return;
        
        // Calculate G-forces
        float gForce = currentAcceleration / 9.81f;
        
        if (Mathf.Abs(gForce) > gForceThreshold)
        {
            // Simulate passenger reactions (screaming, etc.)
            Debug.Log($"High G-force detected: {gForce:F1}G");
        }
    }
    
    public void StartRide()
    {
        if (currentState == RideState.Loading || currentState == RideState.Ready)
        {
            ChangeRideState(RideState.Moving);
            
            if (coasterPhysics != null)
            {
                coasterPhysics.SetSpeed(5f); // Initial speed
            }
            else if (waypointFollower != null)
            {
                waypointFollower.Resume();
            }
        }
    }
    
    public void StopRide()
    {
        if (currentState == RideState.Moving)
        {
            ChangeRideState(RideState.Braking);
            
            if (coasterPhysics != null)
            {
                coasterPhysics.ApplyBrake(emergencyBrakeForce);
            }
            else if (waypointFollower != null)
            {
                waypointFollower.Stop();
            }
        }
    }
    
    public void EmergencyStop()
    {
        TriggerEmergencyStop();
    }
    
    void TriggerEmergencyStop()
    {
        if (isEmergencyStop) return;
        
        isEmergencyStop = true;
        ChangeRideState(RideState.EmergencyStop);
        
        // Apply maximum braking
        if (coasterPhysics != null)
        {
            coasterPhysics.ApplyBrake(emergencyBrakeForce * 2f);
        }
        else if (waypointFollower != null)
        {
            waypointFollower.Stop();
        }
        
        OnEmergencyStop?.Invoke();
        Debug.LogError("EMERGENCY STOP ACTIVATED!");
    }
    
    void ChangeRideState(RideState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnRideStateChanged?.Invoke(newState);
            
            Debug.Log($"Ride state changed to: {newState}");
        }
    }
    
    public void SetSpeed(float speed)
    {
        if (isEmergencyStop) return;
        
        if (coasterPhysics != null)
        {
            coasterPhysics.SetSpeed(speed);
        }
        else if (waypointFollower != null)
        {
            waypointFollower.SetSpeed(speed);
        }
    }
    
    public void ResetEmergencyStop()
    {
        isEmergencyStop = false;
        ChangeRideState(RideState.Ready);
    }
    
    public bool IsOccupied()
    {
        // Check if vehicle has passengers
        return currentState == RideState.Loading || currentState == RideState.Moving;
    }
    
    public float GetProgress()
    {
        // Return ride progress as percentage (0-100)
        if (coasterPhysics != null && coasterPhysics.trackPoints.Length > 0)
        {
            return (float)coasterPhysics.currentSegment / coasterPhysics.trackPoints.Length * 100f;
        }
        return 0f;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Handle collisions
        Debug.LogWarning($"Vehicle collision detected with: {collision.gameObject.name}");
        
        // Check if collision is severe enough for emergency stop
        if (collision.relativeVelocity.magnitude > 10f)
        {
            TriggerEmergencyStop();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Handle trigger zones (brake zones, speed zones, etc.)
        if (other.CompareTag("BrakeZone"))
        {
            Debug.Log("Entering brake zone");
            // Apply braking
            if (coasterPhysics != null)
            {
                coasterPhysics.ApplyBrake(5f);
            }
        }
        else if (other.CompareTag("SpeedZone"))
        {
            SpeedZone speedZone = other.GetComponent<SpeedZone>();
            if (speedZone != null)
            {
                SetSpeed(speedZone.targetSpeed);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw vehicle bounds
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(vehicleWidth, vehicleHeight, vehicleLength));
        
        // Draw safety thresholds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}

// Helper class for speed zones
public class SpeedZone : MonoBehaviour
{
    public float targetSpeed = 10f;
    public float transitionTime = 2f;
} 