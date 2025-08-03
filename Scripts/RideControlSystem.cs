using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class RideControlSystem : MonoBehaviour
{
    [Header("Ride Configuration")]
    public enum RideMode
    {
        Normal,         // Standard forward operation
        Reverse,        // Reverse sequence
        Maintenance,    // Maintenance mode
        Emergency,      // Emergency stop
        Testing         // Test mode
    }
    
    [Header("Block Section Management")]
    public BlockSection[] blockSections;
    public int maxTrains = 3;
    public float minimumBlockDistance = 20f;
    public float blockClearanceTime = 5f;
    
    [Header("Timing and Dispatch")]
    public float dispatchInterval = 120f; // 2 minutes between trains
    public float loadingTime = 60f;
    public float unloadingTime = 45f;
    public float reverseSequenceTime = 180f;
    public float forwardReturnTime = 120f;
    
    [Header("Speed Control")]
    public float normalSpeed = 25f;
    public float reverseSpeed = 15f;
    public float brakeZoneSpeed = 8f;
    public float turntableSpeed = 5f;
    
    [Header("Current State")]
    public RideMode currentMode = RideMode.Normal;
    public int activeTrains = 0;
    public float rideProgress = 0f;
    public bool isRideActive = false;
    
    [Header("Timeline Integration")]
    public PlayableDirector rideTimeline;
    public TimelineAsset[] rideSequences;
    public bool useTimelineControl = true;
    
    [Header("Safety Systems")]
    public bool safetyEnabled = true;
    public float emergencyStopTime = 3f;
    public bool requireBlockClearance = true;
    public bool enableCollisionDetection = true;
    
    [Header("Train Management")]
    public RideVehicleController[] trainPool;
    public Transform[] spawnPoints;
    public Transform[] dispatchPoints;
    public Transform[] loadingStations;
    public Transform[] unloadingStations;
    
    [Header("Switch Track Integration")]
    public SwitchTrackController[] switchTracks;
    public TurntableController[] turntables;
    
    [Header("Event Triggers")]
    public TriggerZone[] eventZones;
    public AudioSource[] rideAudio;
    public ParticleSystem[] rideEffects;
    
    // Private variables
    private List<TrainData> activeTrainData = new List<TrainData>();
    private Queue<RideVehicleController> availableTrains = new Queue<RideVehicleController>();
    private Dictionary<int, BlockSection> blockSectionMap = new Dictionary<int, BlockSection>();
    private Coroutine dispatchCoroutine;
    private Coroutine reverseSequenceCoroutine;
    private float lastDispatchTime = 0f;
    private int currentSequenceIndex = 0;
    
    // Events
    public System.Action<RideMode> OnRideModeChanged;
    public System.Action<int> OnTrainDispatched;
    public System.Action<int> OnTrainCompleted;
    public System.Action OnRideStarted;
    public System.Action OnRideStopped;
    public System.Action OnEmergencyStop;
    public System.Action<float> OnRideProgressChanged;
    
    [System.Serializable]
    public class BlockSection
    {
        public int blockID;
        public string blockName;
        public Transform[] waypoints;
        public Transform[] trackPoints;
        public float blockLength;
        public float maxSpeed;
        public bool isOccupied = false;
        public RideVehicleController currentTrain;
        public float entryTime;
        public float exitTime;
        public bool isReversed = false;
        
        [Header("Safety")]
        public bool hasBrakeZone = false;
        public float brakeZoneStart;
        public float brakeZoneEnd;
        public bool hasSpeedZone = false;
        public float speedZoneLimit;
        
        [Header("Events")]
        public AudioClip[] blockAudio;
        public ParticleSystem[] blockEffects;
        public Light[] blockLights;
    }
    
    [System.Serializable]
    public class TrainData
    {
        public RideVehicleController train;
        public int trainID;
        public int currentBlock;
        public int targetBlock;
        public float dispatchTime;
        public float estimatedCompletionTime;
        public bool isReversed;
        public bool isCompleted;
        public float currentSpeed;
        public Vector3 currentPosition;
    }
    
    [System.Serializable]
    public class TriggerZone
    {
        public string zoneName;
        public Transform zoneTransform;
        public float triggerRadius = 5f;
        public bool isActive = true;
        public System.Action<RideVehicleController> onTriggerEnter;
        public System.Action<RideVehicleController> onTriggerExit;
    }
    
    void Start()
    {
        InitializeRideControl();
        SetupBlockSections();
        SetupTrainPool();
        SetupEventZones();
    }
    
    void InitializeRideControl()
    {
        // Initialize block section map
        foreach (BlockSection block in blockSections)
        {
            blockSectionMap[block.blockID] = block;
        }
        
        // Setup timeline if available
        if (useTimelineControl && rideTimeline != null)
        {
            rideTimeline.stopped += OnTimelineStopped;
            rideTimeline.played += OnTimelinePlayed;
        }
        
        ChangeRideMode(RideMode.Normal);
    }
    
    void SetupBlockSections()
    {
        for (int i = 0; i < blockSections.Length; i++)
        {
            BlockSection block = blockSections[i];
            block.blockID = i;
            
            // Calculate block length if not set
            if (block.blockLength <= 0 && block.waypoints.Length > 1)
            {
                block.blockLength = CalculateBlockLength(block.waypoints);
            }
        }
    }
    
    void SetupTrainPool()
    {
        // Initialize train pool
        foreach (RideVehicleController train in trainPool)
        {
            if (train != null)
            {
                availableTrains.Enqueue(train);
                train.gameObject.SetActive(false);
            }
        }
    }
    
    void SetupEventZones()
    {
        foreach (TriggerZone zone in eventZones)
        {
            if (zone.zoneTransform != null)
            {
                // Create trigger collider
                SphereCollider trigger = zone.zoneTransform.gameObject.AddComponent<SphereCollider>();
                trigger.radius = zone.triggerRadius;
                trigger.isTrigger = true;
                
                // Add trigger component
                RideTriggerZone triggerComponent = zone.zoneTransform.gameObject.AddComponent<RideTriggerZone>();
                triggerComponent.zoneName = zone.zoneName;
                triggerComponent.onTriggerEnter = zone.onTriggerEnter;
                triggerComponent.onTriggerExit = zone.onTriggerExit;
            }
        }
    }
    
    public void StartRide()
    {
        if (isRideActive) return;
        
        isRideActive = true;
        OnRideStarted?.Invoke();
        
        // Start dispatch coroutine
        if (dispatchCoroutine != null)
        {
            StopCoroutine(dispatchCoroutine);
        }
        dispatchCoroutine = StartCoroutine(DispatchTrains());
        
        // Start timeline if available
        if (useTimelineControl && rideTimeline != null && rideSequences.Length > 0)
        {
            rideTimeline.playableAsset = rideSequences[0];
            rideTimeline.Play();
        }
        
        Debug.Log("Ride started");
    }
    
    public void StopRide()
    {
        if (!isRideActive) return;
        
        isRideActive = false;
        OnRideStopped?.Invoke();
        
        // Stop dispatch coroutine
        if (dispatchCoroutine != null)
        {
            StopCoroutine(dispatchCoroutine);
            dispatchCoroutine = null;
        }
        
        // Stop timeline
        if (useTimelineControl && rideTimeline != null)
        {
            rideTimeline.Stop();
        }
        
        // Stop all trains
        foreach (TrainData trainData in activeTrainData)
        {
            if (trainData.train != null)
            {
                trainData.train.StopRide();
            }
        }
        
        Debug.Log("Ride stopped");
    }
    
    public void StartReverseSequence()
    {
        if (reverseSequenceCoroutine != null)
        {
            StopCoroutine(reverseSequenceCoroutine);
        }
        
        reverseSequenceCoroutine = StartCoroutine(ReverseSequence());
    }
    
    IEnumerator ReverseSequence()
    {
        Debug.Log("Starting reverse sequence");
        ChangeRideMode(RideMode.Reverse);
        
        // Stop normal dispatch
        if (dispatchCoroutine != null)
        {
            StopCoroutine(dispatchCoroutine);
        }
        
        // Wait for all trains to complete current block
        yield return StartCoroutine(WaitForBlockClearance());
        
        // Reverse all active trains
        foreach (TrainData trainData in activeTrainData)
        {
            if (trainData.train != null && !trainData.isCompleted)
            {
                ReverseTrain(trainData);
            }
        }
        
        // Play reverse timeline
        if (useTimelineControl && rideTimeline != null && rideSequences.Length > 1)
        {
            rideTimeline.playableAsset = rideSequences[1];
            rideTimeline.Play();
        }
        
        // Wait for reverse sequence to complete
        yield return new WaitForSeconds(reverseSequenceTime);
        
        // Return to forward operation
        yield return StartCoroutine(ForwardReturnSequence());
    }
    
    IEnumerator ForwardReturnSequence()
    {
        Debug.Log("Starting forward return sequence");
        
        // Return all trains to forward direction
        foreach (TrainData trainData in activeTrainData)
        {
            if (trainData.train != null && !trainData.isCompleted)
            {
                ReturnTrainToForward(trainData);
            }
        }
        
        // Play forward return timeline
        if (useTimelineControl && rideTimeline != null && rideSequences.Length > 2)
        {
            rideTimeline.playableAsset = rideSequences[2];
            rideTimeline.Play();
        }
        
        // Wait for return sequence to complete
        yield return new WaitForSeconds(forwardReturnTime);
        
        // Resume normal operation
        ChangeRideMode(RideMode.Normal);
        if (isRideActive)
        {
            dispatchCoroutine = StartCoroutine(DispatchTrains());
        }
        
        Debug.Log("Forward return sequence completed");
    }
    
    IEnumerator DispatchTrains()
    {
        while (isRideActive && currentMode == RideMode.Normal)
        {
            // Check if we can dispatch a new train
            if (activeTrains < maxTrains && availableTrains.Count > 0)
            {
                // Wait for dispatch interval
                float timeSinceLastDispatch = Time.time - lastDispatchTime;
                if (timeSinceLastDispatch < dispatchInterval)
                {
                    yield return new WaitForSeconds(dispatchInterval - timeSinceLastDispatch);
                }
                
                // Dispatch train
                DispatchTrain();
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
    
    void DispatchTrain()
    {
        if (availableTrains.Count == 0) return;
        
        RideVehicleController train = availableTrains.Dequeue();
        if (train == null) return;
        
        // Setup train
        int spawnIndex = activeTrains % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];
        
        train.transform.position = spawnPoint.position;
        train.transform.rotation = spawnPoint.rotation;
        train.gameObject.SetActive(true);
        
        // Create train data
        TrainData trainData = new TrainData
        {
            train = train,
            trainID = activeTrains,
            currentBlock = 0,
            targetBlock = 1,
            dispatchTime = Time.time,
            estimatedCompletionTime = Time.time + CalculateRideDuration(),
            isReversed = false,
            isCompleted = false
        };
        
        activeTrainData.Add(trainData);
        activeTrains++;
        
        // Start train
        train.StartRide();
        train.SetSpeed(normalSpeed);
        
        // Subscribe to train events
        train.OnRideStateChanged += (state) => OnTrainStateChanged(trainData, state);
        train.OnSpeedChanged += (speed) => OnTrainSpeedChanged(trainData, speed);
        
        lastDispatchTime = Time.time;
        OnTrainDispatched?.Invoke(trainData.trainID);
        
        Debug.Log($"Train {trainData.trainID} dispatched");
    }
    
    void ReverseTrain(TrainData trainData)
    {
        if (trainData.train == null) return;
        
        trainData.isReversed = true;
        trainData.train.SetSpeed(reverseSpeed);
        
        // Update block sections for reverse
        foreach (BlockSection block in blockSections)
        {
            block.isReversed = true;
        }
        
        Debug.Log($"Train {trainData.trainID} reversed");
    }
    
    void ReturnTrainToForward(TrainData trainData)
    {
        if (trainData.train == null) return;
        
        trainData.isReversed = false;
        trainData.train.SetSpeed(normalSpeed);
        
        // Update block sections for forward
        foreach (BlockSection block in blockSections)
        {
            block.isReversed = false;
        }
        
        Debug.Log($"Train {trainData.trainID} returned to forward");
    }
    
    void OnTrainStateChanged(TrainData trainData, RideVehicleController.RideState state)
    {
        switch (state)
        {
            case RideVehicleController.RideState.Loading:
                HandleTrainLoading(trainData);
                break;
            case RideVehicleController.RideState.Moving:
                HandleTrainMoving(trainData);
                break;
            case RideVehicleController.RideState.Braking:
                HandleTrainBraking(trainData);
                break;
            case RideVehicleController.RideState.Stopped:
                HandleTrainStopped(trainData);
                break;
            case RideVehicleController.RideState.EmergencyStop:
                HandleTrainEmergencyStop(trainData);
                break;
        }
    }
    
    void OnTrainSpeedChanged(TrainData trainData, float speed)
    {
        trainData.currentSpeed = speed;
    }
    
    void HandleTrainLoading(TrainData trainData)
    {
        // Start loading timer
        StartCoroutine(LoadingSequence(trainData));
    }
    
    void HandleTrainMoving(TrainData trainData)
    {
        // Update block occupancy
        UpdateBlockOccupancy(trainData);
        
        // Check for block transitions
        CheckBlockTransitions(trainData);
    }
    
    void HandleTrainBraking(TrainData trainData)
    {
        // Apply brake zone speed
        trainData.train.SetSpeed(brakeZoneSpeed);
    }
    
    void HandleTrainStopped(TrainData trainData)
    {
        // Check if train completed the ride
        if (IsTrainAtUnloadStation(trainData))
        {
            StartCoroutine(UnloadingSequence(trainData));
        }
    }
    
    void HandleTrainEmergencyStop(TrainData trainData)
    {
        Debug.LogWarning($"Train {trainData.trainID} emergency stop");
        OnEmergencyStop?.Invoke();
        
        // Stop the entire ride
        StopRide();
        ChangeRideMode(RideMode.Emergency);
    }
    
    IEnumerator LoadingSequence(TrainData trainData)
    {
        yield return new WaitForSeconds(loadingTime);
        
        // Train is loaded, start moving
        if (trainData.train != null)
        {
            trainData.train.StartRide();
        }
    }
    
    IEnumerator UnloadingSequence(TrainData trainData)
    {
        yield return new WaitForSeconds(unloadingTime);
        
        // Train is unloaded, return to pool
        ReturnTrainToPool(trainData);
    }
    
    void ReturnTrainToPool(TrainData trainData)
    {
        if (trainData.train != null)
        {
            trainData.train.gameObject.SetActive(false);
            availableTrains.Enqueue(trainData.train);
        }
        
        activeTrainData.Remove(trainData);
        activeTrains--;
        
        trainData.isCompleted = true;
        OnTrainCompleted?.Invoke(trainData.trainID);
        
        Debug.Log($"Train {trainData.trainID} returned to pool");
    }
    
    void UpdateBlockOccupancy(TrainData trainData)
    {
        // Clear previous block
        if (trainData.currentBlock < blockSections.Length)
        {
            blockSections[trainData.currentBlock].isOccupied = false;
            blockSections[trainData.currentBlock].currentTrain = null;
        }
        
        // Set current block
        if (trainData.targetBlock < blockSections.Length)
        {
            blockSections[trainData.targetBlock].isOccupied = true;
            blockSections[trainData.targetBlock].currentTrain = trainData.train;
            blockSections[trainData.targetBlock].entryTime = Time.time;
            
            trainData.currentBlock = trainData.targetBlock;
        }
    }
    
    void CheckBlockTransitions(TrainData trainData)
    {
        // Check if train is near next block
        if (trainData.currentBlock < blockSections.Length - 1)
        {
            BlockSection currentBlock = blockSections[trainData.currentBlock];
            BlockSection nextBlock = blockSections[trainData.currentBlock + 1];
            
            float distanceToNext = Vector3.Distance(trainData.train.transform.position, 
                nextBlock.waypoints[0].position);
            
            if (distanceToNext < minimumBlockDistance && !nextBlock.isOccupied)
            {
                trainData.targetBlock = trainData.currentBlock + 1;
                
                // Apply block-specific speed
                if (nextBlock.hasSpeedZone)
                {
                    trainData.train.SetSpeed(nextBlock.speedZoneLimit);
                }
                else
                {
                    trainData.train.SetSpeed(trainData.isReversed ? reverseSpeed : normalSpeed);
                }
            }
        }
    }
    
    bool IsTrainAtUnloadStation(TrainData trainData)
    {
        if (trainData.train == null) return false;
        
        foreach (Transform station in unloadingStations)
        {
            float distance = Vector3.Distance(trainData.train.transform.position, station.position);
            if (distance < 5f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    IEnumerator WaitForBlockClearance()
    {
        bool blocksClear = false;
        while (!blocksClear)
        {
            blocksClear = true;
            foreach (BlockSection block in blockSections)
            {
                if (block.isOccupied)
                {
                    blocksClear = false;
                    break;
                }
            }
            
            if (!blocksClear)
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    float CalculateBlockLength(Transform[] waypoints)
    {
        float length = 0f;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            length += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        }
        return length;
    }
    
    float CalculateRideDuration()
    {
        float totalLength = 0f;
        foreach (BlockSection block in blockSections)
        {
            totalLength += block.blockLength;
        }
        
        return totalLength / normalSpeed + loadingTime + unloadingTime;
    }
    
    void ChangeRideMode(RideMode newMode)
    {
        if (currentMode != newMode)
        {
            currentMode = newMode;
            OnRideModeChanged?.Invoke(newMode);
            
            Debug.Log($"Ride mode changed to: {newMode}");
        }
    }
    
    void OnTimelinePlayed(PlayableDirector director)
    {
        Debug.Log("Ride timeline started");
    }
    
    void OnTimelineStopped(PlayableDirector director)
    {
        Debug.Log("Ride timeline stopped");
    }
    
    void Update()
    {
        UpdateRideProgress();
        UpdateTrainPositions();
    }
    
    void UpdateRideProgress()
    {
        if (activeTrainData.Count > 0)
        {
            float totalProgress = 0f;
            foreach (TrainData trainData in activeTrainData)
            {
                if (trainData.train != null)
                {
                    totalProgress += trainData.train.GetProgress();
                }
            }
            
            rideProgress = totalProgress / activeTrainData.Count;
            OnRideProgressChanged?.Invoke(rideProgress);
        }
    }
    
    void UpdateTrainPositions()
    {
        foreach (TrainData trainData in activeTrainData)
        {
            if (trainData.train != null)
            {
                trainData.currentPosition = trainData.train.transform.position;
            }
        }
    }
    
    public void EmergencyStop()
    {
        Debug.LogError("EMERGENCY STOP ACTIVATED!");
        
        // Stop all trains immediately
        foreach (TrainData trainData in activeTrainData)
        {
            if (trainData.train != null)
            {
                trainData.train.EmergencyStop();
            }
        }
        
        // Stop the ride
        StopRide();
        ChangeRideMode(RideMode.Emergency);
        
        OnEmergencyStop?.Invoke();
    }
    
    public void SetMaintenanceMode(bool maintenance)
    {
        if (maintenance)
        {
            ChangeRideMode(RideMode.Maintenance);
            StopRide();
        }
        else
        {
            ChangeRideMode(RideMode.Normal);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw block sections
        foreach (BlockSection block in blockSections)
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
        
        // Draw spawn points
        Gizmos.color = Color.blue;
        foreach (Transform spawn in spawnPoints)
        {
            if (spawn != null)
            {
                Gizmos.DrawWireSphere(spawn.position, 2f);
            }
        }
        
        // Draw loading/unloading stations
        Gizmos.color = Color.yellow;
        foreach (Transform station in loadingStations)
        {
            if (station != null)
            {
                Gizmos.DrawWireCube(station.position, Vector3.one * 3f);
            }
        }
        
        Gizmos.color = Color.cyan;
        foreach (Transform station in unloadingStations)
        {
            if (station != null)
            {
                Gizmos.DrawWireCube(station.position, Vector3.one * 3f);
            }
        }
    }
}

// Helper component for trigger zones
public class RideTriggerZone : MonoBehaviour
{
    public string zoneName;
    public System.Action<RideVehicleController> onTriggerEnter;
    public System.Action<RideVehicleController> onTriggerExit;
    
    void OnTriggerEnter(Collider other)
    {
        RideVehicleController train = other.GetComponent<RideVehicleController>();
        if (train != null)
        {
            onTriggerEnter?.Invoke(train);
            Debug.Log($"Train entered zone: {zoneName}");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        RideVehicleController train = other.GetComponent<RideVehicleController>();
        if (train != null)
        {
            onTriggerExit?.Invoke(train);
            Debug.Log($"Train exited zone: {zoneName}");
        }
    }
} 