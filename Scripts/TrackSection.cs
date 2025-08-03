using UnityEngine;

public class TrackSection : MonoBehaviour
{
    [Header("Track Section Properties")]
    public SwitchTrackController.SwitchType trackType;
    public float trackLength = 10f;
    public float trackWidth = 2f;
    public Transform[] connectionPoints;
    
    [Header("Track Features")]
    public bool hasBanking = false;
    public float bankingAngle = 0f;
    public bool hasElevation = false;
    public float elevationHeight = 0f;
    public bool hasCurve = false;
    public float curveRadius = 0f;
    
    [Header("Safety Features")]
    public bool hasBrakeZone = false;
    public float brakeZoneLength = 5f;
    public bool hasSpeedZone = false;
    public float speedZoneLimit = 20f;
    
    [Header("Visual Properties")]
    public Material trackMaterial;
    public Color trackColor = Color.gray;
    public bool hasRails = true;
    public bool hasTies = true;
    
    [Header("Physics Properties")]
    public float friction = 0.02f;
    public float roughness = 0.1f;
    public bool isSlippery = false;
    
    void Start()
    {
        SetupTrackSection();
    }
    
    void SetupTrackSection()
    {
        // Set material color
        if (trackMaterial != null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = trackMaterial;
                renderer.material.color = trackColor;
            }
        }
        
        // Setup collider size
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.size = new Vector3(trackLength, 0.5f, trackWidth);
            collider.center = Vector3.zero;
        }
    }
    
    public Transform GetConnectionPoint(int index)
    {
        if (index >= 0 && index < connectionPoints.Length)
        {
            return connectionPoints[index];
        }
        return null;
    }
    
    public Vector3 GetConnectionPosition(int index)
    {
        Transform point = GetConnectionPoint(index);
        if (point != null)
        {
            return point.position;
        }
        return transform.position;
    }
    
    public Quaternion GetConnectionRotation(int index)
    {
        Transform point = GetConnectionPoint(index);
        if (point != null)
        {
            return point.rotation;
        }
        return transform.rotation;
    }
    
    public bool HasConnectionPoint(int index)
    {
        return index >= 0 && index < connectionPoints.Length && connectionPoints[index] != null;
    }
    
    public int GetConnectionPointCount()
    {
        return connectionPoints.Length;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw track bounds
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(trackLength, 0.5f, trackWidth));
        
        // Draw connection points
        Gizmos.color = Color.green;
        for (int i = 0; i < connectionPoints.Length; i++)
        {
            if (connectionPoints[i] != null)
            {
                Gizmos.DrawWireSphere(connectionPoints[i].position, 0.3f);
                
                // Draw connection direction
                Vector3 direction = connectionPoints[i].forward;
                Gizmos.DrawRay(connectionPoints[i].position, direction * 2f);
            }
        }
        
        // Draw special features
        if (hasBanking)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(trackLength, 0.1f, trackWidth));
        }
        
        if (hasElevation)
        {
            Gizmos.color = Color.cyan;
            Vector3 elevatedPos = transform.position + Vector3.up * elevationHeight;
            Gizmos.DrawWireCube(elevatedPos, new Vector3(trackLength, 0.1f, trackWidth));
        }
        
        if (hasCurve && curveRadius > 0)
        {
            Gizmos.color = Color.magenta;
            Vector3 center = transform.position + transform.right * curveRadius;
            Gizmos.DrawWireSphere(center, curveRadius);
        }
    }
} 