using UnityEngine;
using System.Collections;

public class TurntableController : MonoBehaviour
{
    [Header("Turntable Settings")]
    public enum TurntableState
    {
        Idle,
        Rotating,
        Locked,
        Error,
        Maintenance
    }
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f; // Degrees per second
    public float[] rotationPositions = { 0f, 90f, 180f, 270f }; // Available positions
    public int currentPositionIndex = 0;
    public bool autoRotate = false;
    public float autoRotateInterval = 30f;
    
    [Header("Current State")]
    public TurntableState currentState = TurntableState.Idle;
    public float currentRotation = 0f;
    public int targetPositionIndex = 0;
    
    [Header("Safety Systems")]
    public bool safetyEnabled = true;
    public float safetyCheckDistance = 5f;
    public LayerMask vehicleLayerMask = -1;
    public bool requireClearance = true;
    
    [Header("Visual Feedback")]
    public Material[] turntableMaterials;
    public Light[] statusLights;
    public ParticleSystem[] rotationEffects;
    public AudioSource rotationAudio;
    
    [Header("Integration")]
    public SwitchTrackController[] connectedSwitches;
    public Transform[] connectionPoints;
    
    // Private variables
    private Coroutine rotationCoroutine;
    private Coroutine autoRotateCoroutine;
    private bool isRotating = false;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    
    // Events
    public System.Action<TurntableState> OnStateChanged;
    public System.Action<int> OnPositionChanged;
    public System.Action OnRotationStarted;
    public System.Action OnRotationCompleted;
    public System.Action OnSafetyViolation;
    
    void Start()
    {
        InitializeTurntable();
        ChangeState(TurntableState.Idle);
        
        if (autoRotate)
        {
            StartAutoRotate();
        }
    }
    
    void InitializeTurntable()
    {
        // Set initial rotation
        currentRotation = rotationPositions[currentPositionIndex];
        transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        
        // Setup audio
        if (rotationAudio != null)
        {
            rotationAudio.loop = true;
            rotationAudio.volume = 0f;
        }
    }
    
    public bool RotateToPosition(int positionIndex)
    {
        if (positionIndex < 0 || positionIndex >= rotationPositions.Length)
        {
            Debug.LogError($"Invalid position index: {positionIndex}");
            return false;
        }
        
        if (currentPositionIndex == positionIndex)
        {
            Debug.Log("Already at target position");
            return true;
        }
        
        if (isRotating)
        {
            Debug.LogWarning("Turntable is already rotating");
            return false;
        }
        
        if (currentState == TurntableState.Locked)
        {
            Debug.LogWarning("Turntable is locked");
            return false;
        }
        
        if (currentState == TurntableState.Maintenance)
        {
            Debug.LogWarning("Turntable is in maintenance mode");
            return false;
        }
        
        // Safety check
        if (safetyEnabled && requireClearance && !IsTurntableClear())
        {
            Debug.LogWarning("Turntable is not clear - cannot rotate");
            OnSafetyViolation?.Invoke();
            return false;
        }
        
        targetPositionIndex = positionIndex;
        StartRotation();
        return true;
    }
    
    public void RotateClockwise()
    {
        int nextIndex = (currentPositionIndex + 1) % rotationPositions.Length;
        RotateToPosition(nextIndex);
    }
    
    public void RotateCounterClockwise()
    {
        int prevIndex = (currentPositionIndex - 1 + rotationPositions.Length) % rotationPositions.Length;
        RotateToPosition(prevIndex);
    }
    
    void StartRotation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        
        rotationCoroutine = StartCoroutine(RotationSequence());
    }
    
    IEnumerator RotationSequence()
    {
        isRotating = true;
        ChangeState(TurntableState.Rotating);
        OnRotationStarted?.Invoke();
        
        // Calculate rotation
        float targetRotationAngle = rotationPositions[targetPositionIndex];
        float startRotationAngle = currentRotation;
        
        // Determine shortest rotation path
        float rotationDelta = targetRotationAngle - startRotationAngle;
        if (rotationDelta > 180f) rotationDelta -= 360f;
        if (rotationDelta < -180f) rotationDelta += 360f;
        
        float rotationDuration = Mathf.Abs(rotationDelta) / rotationSpeed;
        
        // Start rotation effects
        StartRotationEffects();
        
        // Perform rotation
        float elapsedTime = 0f;
        startRotation = transform.rotation;
        targetRotation = Quaternion.Euler(0, targetRotationAngle, 0);
        
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationDuration;
            
            // Smooth rotation
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            currentRotation = transform.eulerAngles.y;
            
            yield return null;
        }
        
        // Ensure exact final position
        transform.rotation = targetRotation;
        currentRotation = targetRotationAngle;
        
        // Update position index
        currentPositionIndex = targetPositionIndex;
        
        // Stop rotation effects
        StopRotationEffects();
        
        isRotating = false;
        ChangeState(TurntableState.Idle);
        OnRotationCompleted?.Invoke();
        OnPositionChanged?.Invoke(currentPositionIndex);
        
        rotationCoroutine = null;
        
        Debug.Log($"Turntable rotated to position {currentPositionIndex} ({currentRotation}°)");
    }
    
    void StartRotationEffects()
    {
        // Start particle effects
        foreach (ParticleSystem effect in rotationEffects)
        {
            if (effect != null && !effect.isPlaying)
            {
                effect.Play();
            }
        }
        
        // Start audio
        if (rotationAudio != null)
        {
            rotationAudio.volume = 0.5f;
            rotationAudio.Play();
        }
    }
    
    void StopRotationEffects()
    {
        // Stop particle effects
        foreach (ParticleSystem effect in rotationEffects)
        {
            if (effect != null && effect.isPlaying)
            {
                effect.Stop();
            }
        }
        
        // Stop audio
        if (rotationAudio != null)
        {
            rotationAudio.volume = 0f;
            rotationAudio.Stop();
        }
    }
    
    public void StartAutoRotate()
    {
        if (autoRotateCoroutine != null)
        {
            StopCoroutine(autoRotateCoroutine);
        }
        
        autoRotateCoroutine = StartCoroutine(AutoRotateSequence());
    }
    
    public void StopAutoRotate()
    {
        if (autoRotateCoroutine != null)
        {
            StopCoroutine(autoRotateCoroutine);
            autoRotateCoroutine = null;
        }
    }
    
    IEnumerator AutoRotateSequence()
    {
        while (autoRotate && currentState != TurntableState.Maintenance)
        {
            yield return new WaitForSeconds(autoRotateInterval);
            
            if (currentState == TurntableState.Idle)
            {
                RotateClockwise();
            }
        }
    }
    
    bool IsTurntableClear()
    {
        // Check if there are any vehicles in the safety zone
        Collider[] vehicles = Physics.OverlapSphere(transform.position, safetyCheckDistance, vehicleLayerMask);
        return vehicles.Length == 0;
    }
    
    public void LockTurntable()
    {
        ChangeState(TurntableState.Locked);
        StopAutoRotate();
    }
    
    public void UnlockTurntable()
    {
        if (currentState == TurntableState.Locked)
        {
            ChangeState(TurntableState.Idle);
            
            if (autoRotate)
            {
                StartAutoRotate();
            }
        }
    }
    
    public void SetMaintenanceMode(bool maintenance)
    {
        if (maintenance)
        {
            ChangeState(TurntableState.Maintenance);
            StopAutoRotate();
            
            if (isRotating)
            {
                StopRotation();
            }
        }
        else
        {
            ChangeState(TurntableState.Idle);
            
            if (autoRotate)
            {
                StartAutoRotate();
            }
        }
    }
    
    public void EmergencyStop()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
        
        StopAutoRotate();
        StopRotationEffects();
        
        isRotating = false;
        ChangeState(TurntableState.Error);
    }
    
    public void ResetError()
    {
        if (currentState == TurntableState.Error)
        {
            ChangeState(TurntableState.Idle);
        }
    }
    
    void StopRotation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
        
        StopRotationEffects();
        isRotating = false;
    }
    
    void ChangeState(TurntableState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
            UpdateVisualFeedback();
            
            Debug.Log($"Turntable state changed to: {newState}");
        }
    }
    
    void UpdateVisualFeedback()
    {
        // Update materials based on state
        if (turntableMaterials.Length > 0)
        {
            Material targetMaterial = GetMaterialForState(currentState);
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = targetMaterial;
            }
        }
        
        // Update status lights
        UpdateStatusLights();
    }
    
    Material GetMaterialForState(TurntableState state)
    {
        int materialIndex = 0; // Default material
        
        switch (state)
        {
            case TurntableState.Idle:
                materialIndex = 0;
                break;
            case TurntableState.Rotating:
                materialIndex = 1;
                break;
            case TurntableState.Locked:
                materialIndex = 2;
                break;
            case TurntableState.Error:
                materialIndex = 3;
                break;
            case TurntableState.Maintenance:
                materialIndex = 4;
                break;
        }
        
        if (materialIndex < turntableMaterials.Length)
        {
            return turntableMaterials[materialIndex];
        }
        
        return turntableMaterials[0]; // Fallback to default
    }
    
    void UpdateStatusLights()
    {
        if (statusLights.Length == 0) return;
        
        Color lightColor = Color.white;
        
        switch (currentState)
        {
            case TurntableState.Idle:
                lightColor = Color.green;
                break;
            case TurntableState.Rotating:
                lightColor = Color.yellow;
                break;
            case TurntableState.Locked:
                lightColor = Color.blue;
                break;
            case TurntableState.Error:
                lightColor = Color.red;
                break;
            case TurntableState.Maintenance:
                lightColor = Color.orange;
                break;
        }
        
        foreach (Light light in statusLights)
        {
            if (light != null)
            {
                light.color = lightColor;
                light.intensity = currentState == TurntableState.Rotating ? 2f : 1f;
            }
        }
    }
    
    public float GetCurrentRotation()
    {
        return currentRotation;
    }
    
    public int GetCurrentPositionIndex()
    {
        return currentPositionIndex;
    }
    
    public bool IsRotating()
    {
        return isRotating;
    }
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(1f, speed);
    }
    
    public void SetAutoRotateInterval(float interval)
    {
        autoRotateInterval = Mathf.Max(1f, interval);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw safety zone
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, safetyCheckDistance);
        
        // Draw rotation positions
        Gizmos.color = Color.blue;
        for (int i = 0; i < rotationPositions.Length; i++)
        {
            Vector3 position = transform.position;
            Quaternion rotation = Quaternion.Euler(0, rotationPositions[i], 0);
            Vector3 direction = rotation * Vector3.forward;
            
            Gizmos.DrawRay(position, direction * 3f);
            Gizmos.DrawWireSphere(position + direction * 3f, 0.5f);
        }
        
        // Draw connection points
        Gizmos.color = Color.green;
        foreach (Transform point in connectionPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.3f);
                Gizmos.DrawLine(transform.position, point.position);
            }
        }
    }
} 