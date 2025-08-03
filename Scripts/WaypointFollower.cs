using UnityEngine;

public class WaypointFollower : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    public float speed = 10f;
    public float rotationSpeed = 5f;
    public bool loop = true;
    
    [Header("Physics Settings")]
    public float maxSpeed = 30f;
    public float acceleration = 5f;
    public float deceleration = 8f;
    
    private int currentWaypointIndex = 0;
    private Rigidbody rb;
    private float currentSpeed = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on RideVehicle!");
        }
        
        if (waypoints.Length == 0)
        {
            Debug.LogWarning("No waypoints assigned to WaypointFollower!");
        }
    }
    
    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;
        
        FollowWaypoint();
        UpdateSpeed();
    }
    
    void FollowWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Length) return;
        
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        
        // Move towards waypoint
        Vector3 targetVelocity = direction * currentSpeed;
        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 5f);
        
        // Rotate towards waypoint
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        
        // Check if reached waypoint
        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);
        if (distanceToWaypoint < 2f)
        {
            currentWaypointIndex++;
            
            if (loop && currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
        }
    }
    
    void UpdateSpeed()
    {
        // Gradually increase speed towards target
        currentSpeed = Mathf.MoveTowards(currentSpeed, speed, acceleration * Time.fixedDeltaTime);
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
    }
    
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Clamp(newSpeed, 0f, maxSpeed);
    }
    
    public void Stop()
    {
        speed = 0f;
        currentSpeed = 0f;
    }
    
    public void Resume()
    {
        speed = 10f; // Default speed
    }
    
    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
                
                if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
                else if (loop && waypoints[0] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
                }
            }
        }
    }
} 