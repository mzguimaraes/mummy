using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwitchTrackController : MonoBehaviour
{
    [Header("Switch Track Settings")]
    public enum SwitchType
    {
        Y_Switch,           // Y-shaped track split
        Turntable,          // Rotating platform
        SlidingTrack,       // Track that slides horizontally
        ElevatorTrack,      // Track that moves vertically
        RotatingSection     // Track section that rotates in place
    }
    
    [Header("Switch Configuration")]
    public SwitchType switchType = SwitchType.Y_Switch;
    public Transform[] trackSections;        // Track section prefabs
    public Transform[] connectionPoints;     // Connection points for each track
    public int currentTrackIndex = 0;        // Currently active track
    
    [Header("Movement Settings")]
    public float switchSpeed = 2f;           // Speed of track movement
    public float rotationSpeed = 90f;        // Degrees per second for rotation
    public float movementDistance = 5f;      // Distance for sliding tracks
    public float elevationHeight = 3f;       // Height for elevator tracks
    
    [Header("State Machine")]
    public enum SwitchState
    {
        Idle,           // Waiting for switch command
        Switching,      // Currently switching tracks
        Locked,         // Track is locked in position
        Error,          // Error state
        Maintenance     // Maintenance mode
    }
    
    [Header("Current State")]
    public SwitchState currentState = SwitchState.Idle;
    public bool isLocked = false;
    public bool isMaintenanceMode = false;
    
    [Header("Safety Systems")]
    public bool safetyEnabled = true;
    public float safetyCheckDistance = 10f;
    public LayerMask vehicleLayerMask = -1;
    public bool requireClearance = true;
    
    [Header("Timing")]
    public float switchDelay = 1f;           // Delay before switching
    public float lockDelay = 2f;             // Delay before locking
    public float unlockDelay = 1f;           // Delay before unlocking
    
    [Header("Visual Feedback")]
    public Material[] trackMaterials;        // Materials for different states
    public Light[] statusLights;             // Status indicator lights
    public ParticleSystem[] switchEffects;   // Visual effects during switching
    
    // Private variables
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;
    private Coroutine switchCoroutine;
    private bool isSwitching = false;
    private int targetTrackIndex = 0;
    
    // Events
    public System.Action<int> OnTrackSwitched;
    public System.Action<SwitchState> OnStateChanged;
    public System.Action OnSwitchStarted;
    public System.Action OnSwitchCompleted;
    public System.Action OnSafetyViolation;
    
    void Start()
    {
        InitializeSwitchTrack();
        ChangeState(SwitchState.Idle);
    }
    
    void InitializeSwitchTrack()
    {
        // Store original positions and rotations
        originalPositions = new Vector3[trackSections.Length];
        originalRotations = new Quaternion[trackSections.Length];
        
        for (int i = 0; i < trackSections.Length; i++)
        {
            if (trackSections[i] != null)
            {
                originalPositions[i] = trackSections[i].position;
                originalRotations[i] = trackSections[i].rotation;
            }
        }
        
        // Set initial track position
        SetTrackPosition(currentTrackIndex);
    }
    
    public bool SwitchToTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= trackSections.Length)
        {
            Debug.LogError($"Invalid track index: {trackIndex}");
            return false;
        }
        
        if (currentTrackIndex == trackIndex)
        {
            Debug.Log("Already on target track");
            return true;
        }
        
        if (isSwitching)
        {
            Debug.LogWarning("Switch track is already switching");
            return false;
        }
        
        if (currentState == SwitchState.Locked)
        {
            Debug.LogWarning("Switch track is locked");
            return false;
        }
        
        if (isMaintenanceMode)
        {
            Debug.LogWarning("Switch track is in maintenance mode");
            return false;
        }
        
        // Safety check
        if (safetyEnabled && requireClearance && !IsTrackClear())
        {
            Debug.LogWarning("Track is not clear - cannot switch");
            OnSafetyViolation?.Invoke();
            return false;
        }
        
        targetTrackIndex = trackIndex;
        StartSwitchSequence();
        return true;
    }
    
    void StartSwitchSequence()
    {
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }
        
        switchCoroutine = StartCoroutine(SwitchSequence());
    }
    
    IEnumerator SwitchSequence()
    {
        isSwitching = true;
        ChangeState(SwitchState.Switching);
        OnSwitchStarted?.Invoke();
        
        // Pre-switch delay
        yield return new WaitForSeconds(switchDelay);
        
        // Perform the switch based on type
        switch (switchType)
        {
            case SwitchType.Y_Switch:
                yield return StartCoroutine(SwitchYTrack());
                break;
            case SwitchType.Turntable:
                yield return StartCoroutine(SwitchTurntable());
                break;
            case SwitchType.SlidingTrack:
                yield return StartCoroutine(SwitchSlidingTrack());
                break;
            case SwitchType.ElevatorTrack:
                yield return StartCoroutine(SwitchElevatorTrack());
                break;
            case SwitchType.RotatingSection:
                yield return StartCoroutine(SwitchRotatingSection());
                break;
        }
        
        // Post-switch delay
        yield return new WaitForSeconds(lockDelay);
        
        // Update current track
        currentTrackIndex = targetTrackIndex;
        SetTrackPosition(currentTrackIndex);
        
        // Lock the track
        if (isLocked)
        {
            ChangeState(SwitchState.Locked);
        }
        else
        {
            ChangeState(SwitchState.Idle);
        }
        
        isSwitching = false;
        OnSwitchCompleted?.Invoke();
        OnTrackSwitched?.Invoke(currentTrackIndex);
        
        switchCoroutine = null;
    }
    
    IEnumerator SwitchYTrack()
    {
        // Y-switch: Rotate track sections to align with target
        float elapsedTime = 0f;
        float switchDuration = 1f / switchSpeed;
        
        Quaternion startRotation = trackSections[currentTrackIndex].rotation;
        Quaternion targetRotation = GetTargetRotation(targetTrackIndex);
        
        while (elapsedTime < switchDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / switchDuration;
            
            // Smooth rotation
            trackSections[currentTrackIndex].rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        trackSections[currentTrackIndex].rotation = targetRotation;
    }
    
    IEnumerator SwitchTurntable()
    {
        // Turntable: Rotate the entire platform
        float elapsedTime = 0f;
        float rotationAngle = GetRotationAngle(currentTrackIndex, targetTrackIndex);
        float switchDuration = Mathf.Abs(rotationAngle) / rotationSpeed;
        
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, rotationAngle, 0);
        
        while (elapsedTime < switchDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / switchDuration;
            
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        transform.rotation = targetRotation;
    }
    
    IEnumerator SwitchSlidingTrack()
    {
        // Sliding track: Move track horizontally
        float elapsedTime = 0f;
        float switchDuration = movementDistance / switchSpeed;
        
        Vector3 startPosition = trackSections[currentTrackIndex].position;
        Vector3 targetPosition = GetTargetPosition(targetTrackIndex);
        
        while (elapsedTime < switchDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / switchDuration;
            
            trackSections[currentTrackIndex].position = Vector3.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }
        
        trackSections[currentTrackIndex].position = targetPosition;
    }
    
    IEnumerator SwitchElevatorTrack()
    {
        // Elevator track: Move track vertically
        float elapsedTime = 0f;
        float switchDuration = elevationHeight / switchSpeed;
        
        Vector3 startPosition = trackSections[currentTrackIndex].position;
        Vector3 targetPosition = GetTargetPosition(targetTrackIndex);
        
        while (elapsedTime < switchDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / switchDuration;
            
            trackSections[currentTrackIndex].position = Vector3.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }
        
        trackSections[currentTrackIndex].position = targetPosition;
    }
    
    IEnumerator SwitchRotatingSection()
    {
        // Rotating section: Rotate track section in place
        float elapsedTime = 0f;
        float switchDuration = 1f / switchSpeed;
        
        Quaternion startRotation = trackSections[currentTrackIndex].rotation;
        Quaternion targetRotation = GetTargetRotation(targetTrackIndex);
        
        while (elapsedTime < switchDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / switchDuration;
            
            trackSections[currentTrackIndex].rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        trackSections[currentTrackIndex].rotation = targetRotation;
    }
    
    Quaternion GetTargetRotation(int trackIndex)
    {
        if (trackIndex < connectionPoints.Length && connectionPoints[trackIndex] != null)
        {
            return connectionPoints[trackIndex].rotation;
        }
        return originalRotations[trackIndex];
    }
    
    Vector3 GetTargetPosition(int trackIndex)
    {
        if (trackIndex < connectionPoints.Length && connectionPoints[trackIndex] != null)
        {
            return connectionPoints[trackIndex].position;
        }
        return originalPositions[trackIndex];
    }
    
    float GetRotationAngle(int fromIndex, int toIndex)
    {
        // Calculate rotation angle between track positions
        float anglePerTrack = 360f / trackSections.Length;
        return (toIndex - fromIndex) * anglePerTrack;
    }
    
    void SetTrackPosition(int trackIndex)
    {
        // Set all tracks to their correct positions
        for (int i = 0; i < trackSections.Length; i++)
        {
            if (trackSections[i] != null)
            {
                if (i == trackIndex)
                {
                    // Active track
                    trackSections[i].position = GetTargetPosition(i);
                    trackSections[i].rotation = GetTargetRotation(i);
                    trackSections[i].gameObject.SetActive(true);
                }
                else
                {
                    // Inactive tracks - move to storage position or disable
                    trackSections[i].position = originalPositions[i];
                    trackSections[i].rotation = originalRotations[i];
                    trackSections[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    bool IsTrackClear()
    {
        // Check if there are any vehicles in the safety zone
        Collider[] vehicles = Physics.OverlapSphere(transform.position, safetyCheckDistance, vehicleLayerMask);
        return vehicles.Length == 0;
    }
    
    public void LockTrack()
    {
        isLocked = true;
        if (currentState == SwitchState.Idle)
        {
            ChangeState(SwitchState.Locked);
        }
    }
    
    public void UnlockTrack()
    {
        isLocked = false;
        if (currentState == SwitchState.Locked)
        {
            StartCoroutine(UnlockSequence());
        }
    }
    
    IEnumerator UnlockSequence()
    {
        yield return new WaitForSeconds(unlockDelay);
        ChangeState(SwitchState.Idle);
    }
    
    public void SetMaintenanceMode(bool maintenance)
    {
        isMaintenanceMode = maintenance;
        if (maintenance)
        {
            ChangeState(SwitchState.Maintenance);
        }
        else
        {
            ChangeState(SwitchState.Idle);
        }
    }
    
    public void EmergencyStop()
    {
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }
        
        isSwitching = false;
        ChangeState(SwitchState.Error);
    }
    
    public void ResetError()
    {
        if (currentState == SwitchState.Error)
        {
            ChangeState(SwitchState.Idle);
        }
    }
    
    void ChangeState(SwitchState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
            UpdateVisualFeedback();
            
            Debug.Log($"Switch track state changed to: {newState}");
        }
    }
    
    void UpdateVisualFeedback()
    {
        // Update materials based on state
        if (trackMaterials.Length > 0)
        {
            Material targetMaterial = GetMaterialForState(currentState);
            foreach (Transform track in trackSections)
            {
                if (track != null)
                {
                    Renderer renderer = track.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = targetMaterial;
                    }
                }
            }
        }
        
        // Update status lights
        UpdateStatusLights();
        
        // Play effects
        PlaySwitchEffects();
    }
    
    Material GetMaterialForState(SwitchState state)
    {
        int materialIndex = 0; // Default material
        
        switch (state)
        {
            case SwitchState.Idle:
                materialIndex = 0;
                break;
            case SwitchState.Switching:
                materialIndex = 1;
                break;
            case SwitchState.Locked:
                materialIndex = 2;
                break;
            case SwitchState.Error:
                materialIndex = 3;
                break;
            case SwitchState.Maintenance:
                materialIndex = 4;
                break;
        }
        
        if (materialIndex < trackMaterials.Length)
        {
            return trackMaterials[materialIndex];
        }
        
        return trackMaterials[0]; // Fallback to default
    }
    
    void UpdateStatusLights()
    {
        if (statusLights.Length == 0) return;
        
        Color lightColor = Color.white;
        
        switch (currentState)
        {
            case SwitchState.Idle:
                lightColor = Color.green;
                break;
            case SwitchState.Switching:
                lightColor = Color.yellow;
                break;
            case SwitchState.Locked:
                lightColor = Color.blue;
                break;
            case SwitchState.Error:
                lightColor = Color.red;
                break;
            case SwitchState.Maintenance:
                lightColor = Color.orange;
                break;
        }
        
        foreach (Light light in statusLights)
        {
            if (light != null)
            {
                light.color = lightColor;
                light.intensity = currentState == SwitchState.Switching ? 2f : 1f;
            }
        }
    }
    
    void PlaySwitchEffects()
    {
        if (currentState == SwitchState.Switching)
        {
            foreach (ParticleSystem effect in switchEffects)
            {
                if (effect != null && !effect.isPlaying)
                {
                    effect.Play();
                }
            }
        }
        else
        {
            foreach (ParticleSystem effect in switchEffects)
            {
                if (effect != null && effect.isPlaying)
                {
                    effect.Stop();
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw safety zone
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, safetyCheckDistance);
        
        // Draw track sections
        Gizmos.color = Color.blue;
        for (int i = 0; i < trackSections.Length; i++)
        {
            if (trackSections[i] != null)
            {
                Gizmos.DrawWireCube(trackSections[i].position, Vector3.one);
                
                if (i < connectionPoints.Length && connectionPoints[i] != null)
                {
                    Gizmos.DrawLine(trackSections[i].position, connectionPoints[i].position);
                }
            }
        }
        
        // Draw connection points
        Gizmos.color = Color.green;
        for (int i = 0; i < connectionPoints.Length; i++)
        {
            if (connectionPoints[i] != null)
            {
                Gizmos.DrawWireSphere(connectionPoints[i].position, 0.5f);
            }
        }
    }
} 