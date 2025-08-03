using UnityEngine;
using System.Collections.Generic;

public class CoasterPhysics : MonoBehaviour
{
    [Header("Track Settings")]
    public Transform[] trackPoints;
    public float trackWidth = 1.5f;
    public float bankingAngle = 15f;
    
    [Header("Physics Settings")]
    public float mass = 1000f;
    public float drag = 0.1f;
    public float angularDrag = 0.05f;
    public float maxSpeed = 40f;
    public float initialSpeed = 5f;
    
    [Header("Gravity and Forces")]
    public float gravity = 9.81f;
    public float friction = 0.02f;
    public float airResistance = 0.01f;
    
    [Header("Spline Settings")]
    public int splineResolution = 10;
    public bool useSplineInterpolation = true;
    
    private Rigidbody rb;
    private float currentSpeed;
    private int currentSegment = 0;
    private float segmentProgress = 0f;
    private List<Vector3> splinePoints = new List<Vector3>();
    private List<Quaternion> splineRotations = new List<Quaternion>();
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        SetupRigidbody();
        GenerateSpline();
        currentSpeed = initialSpeed;
    }
    
    void SetupRigidbody()
    {
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        rb.useGravity = false; // We'll handle gravity manually
        rb.constraints = RigidbodyConstraints.FreezeRotationZ; // Prevent rolling
    }
    
    void GenerateSpline()
    {
        if (trackPoints.Length < 2) return;
        
        splinePoints.Clear();
        splineRotations.Clear();
        
        for (int i = 0; i < trackPoints.Length - 1; i++)
        {
            Vector3 start = trackPoints[i].position;
            Vector3 end = trackPoints[i + 1].position;
            
            for (int j = 0; j <= splineResolution; j++)
            {
                float t = (float)j / splineResolution;
                Vector3 point = Vector3.Lerp(start, end, t);
                
                // Add banking based on curve direction
                Vector3 direction = (end - start).normalized;
                float banking = CalculateBanking(direction, t);
                Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 0, banking);
                
                splinePoints.Add(point);
                splineRotations.Add(rotation);
            }
        }
    }
    
    float CalculateBanking(Vector3 direction, float t)
    {
        // Simple banking calculation - can be enhanced with actual track data
        float curveIntensity = Mathf.Sin(t * Mathf.PI * 2) * bankingAngle;
        return curveIntensity;
    }
    
    void FixedUpdate()
    {
        if (splinePoints.Count == 0) return;
        
        UpdatePhysics();
        MoveAlongSpline();
        ApplyForces();
    }
    
    void UpdatePhysics()
    {
        // Calculate forces based on track geometry
        Vector3 trackNormal = GetTrackNormal();
        Vector3 gravityForce = Vector3.down * gravity * mass;
        
        // Project gravity onto track
        Vector3 projectedGravity = Vector3.ProjectOnPlane(gravityForce, trackNormal);
        
        // Apply forces
        rb.AddForce(projectedGravity);
        
        // Apply friction
        Vector3 frictionForce = -rb.velocity.normalized * friction * mass;
        rb.AddForce(frictionForce);
        
        // Apply air resistance
        Vector3 airResistanceForce = -rb.velocity * airResistance * rb.velocity.magnitude;
        rb.AddForce(airResistanceForce);
    }
    
    void MoveAlongSpline()
    {
        if (currentSegment >= splinePoints.Count - 1) return;
        
        Vector3 targetPoint = splinePoints[currentSegment];
        Vector3 nextPoint = splinePoints[currentSegment + 1];
        
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
        
        if (distanceToTarget < 1f)
        {
            currentSegment++;
            segmentProgress = 0f;
            
            if (currentSegment >= splinePoints.Count - 1)
            {
                currentSegment = 0; // Loop back to start
            }
        }
        
        // Interpolate between current and next spline point
        if (useSplineInterpolation && currentSegment < splinePoints.Count - 1)
        {
            Vector3 currentPoint = splinePoints[currentSegment];
            Vector3 nextSplinePoint = splinePoints[currentSegment + 1];
            
            Vector3 interpolatedPosition = Vector3.Lerp(currentPoint, nextSplinePoint, segmentProgress);
            Quaternion interpolatedRotation = Quaternion.Slerp(splineRotations[currentSegment], splineRotations[currentSegment + 1], segmentProgress);
            
            // Move towards interpolated position
            Vector3 direction = (interpolatedPosition - transform.position).normalized;
            rb.velocity = direction * currentSpeed;
            
            // Apply rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, interpolatedRotation, Time.fixedDeltaTime * 5f);
            
            segmentProgress += Time.fixedDeltaTime * currentSpeed / Vector3.Distance(currentPoint, nextSplinePoint);
        }
        else
        {
            // Simple waypoint following
            Vector3 direction = (targetPoint - transform.position).normalized;
            rb.velocity = direction * currentSpeed;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
            }
        }
    }
    
    Vector3 GetTrackNormal()
    {
        if (currentSegment >= splinePoints.Count - 1) return Vector3.up;
        
        Vector3 currentPoint = splinePoints[currentSegment];
        Vector3 nextPoint = splinePoints[currentSegment + 1];
        
        Vector3 forward = (nextPoint - currentPoint).normalized;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;
        
        return up;
    }
    
    void ApplyForces()
    {
        // Clamp speed
        currentSpeed = Mathf.Clamp(rb.velocity.magnitude, 0f, maxSpeed);
    }
    
    public void SetSpeed(float newSpeed)
    {
        currentSpeed = Mathf.Clamp(newSpeed, 0f, maxSpeed);
    }
    
    public void ApplyBrake(float brakeForce)
    {
        currentSpeed = Mathf.Max(0f, currentSpeed - brakeForce * Time.fixedDeltaTime);
    }
    
    void OnDrawGizmosSelected()
    {
        if (trackPoints == null) return;
        
        // Draw track points
        Gizmos.color = Color.blue;
        for (int i = 0; i < trackPoints.Length; i++)
        {
            if (trackPoints[i] != null)
            {
                Gizmos.DrawWireSphere(trackPoints[i].position, 0.3f);
                
                if (i < trackPoints.Length - 1 && trackPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(trackPoints[i].position, trackPoints[i + 1].position);
                }
            }
        }
        
        // Draw spline points
        Gizmos.color = Color.green;
        for (int i = 0; i < splinePoints.Count; i++)
        {
            Gizmos.DrawWireSphere(splinePoints[i], 0.1f);
            
            if (i < splinePoints.Count - 1)
            {
                Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
            }
        }
    }
} 