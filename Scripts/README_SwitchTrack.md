# Switch Track System Documentation

## Overview
The Switch Track System provides comprehensive track switching capabilities for roller coaster simulations, including Y-switches, turntables, sliding tracks, and other track redirection mechanisms. It features state machines, safety systems, and visual feedback.

## Components

### 1. SwitchTrackController.cs
Main controller for track switching operations with state machine management.

**Key Features:**
- Multiple switch types (Y-Switch, Turntable, Sliding Track, Elevator Track, Rotating Section)
- State machine with safety checks
- Visual feedback and status indicators
- Event system for monitoring
- Safety clearance checking

**Switch Types:**
- **Y_Switch**: Traditional Y-shaped track split with rotating sections
- **Turntable**: Rotating platform with multiple connection points
- **SlidingTrack**: Track that slides horizontally between positions
- **ElevatorTrack**: Track that moves vertically between levels
- **RotatingSection**: Track section that rotates in place

**State Machine:**
- **Idle**: Waiting for switch command
- **Switching**: Currently switching tracks
- **Locked**: Track is locked in position
- **Error**: Error state requiring reset
- **Maintenance**: Maintenance mode

### 2. TrackSection.cs
Component for defining track section properties and connection points.

**Key Features:**
- Track type and dimensions
- Connection point management
- Special features (banking, elevation, curves)
- Safety zones (brake zones, speed zones)
- Visual and physics properties

### 3. Track Section Prefabs

#### TrackSection_YSwitch.prefab
Y-shaped track switch with three connection points:
- **ConnectionPoint_Left**: Left branch connection
- **ConnectionPoint_Right**: Right branch connection  
- **ConnectionPoint_Center**: Main track connection

#### TrackSection_Turntable.prefab
Rotating platform with four connection points:
- **ConnectionPoint_North**: North connection
- **ConnectionPoint_East**: East connection
- **ConnectionPoint_South**: South connection
- **ConnectionPoint_West**: West connection

## Setup Instructions

### 1. Basic Switch Track Setup
1. Create an empty GameObject for the switch track
2. Add `SwitchTrackController` component
3. Assign track section prefabs to `trackSections` array
4. Set up connection points for each track section
5. Configure switch type and movement parameters

### 2. Track Section Configuration
```csharp
// Example setup for Y-switch
switchTrack.switchType = SwitchTrackController.SwitchType.Y_Switch;
switchTrack.trackSections = new Transform[] { leftTrack, rightTrack, centerTrack };
switchTrack.connectionPoints = new Transform[] { leftConnector, rightConnector, centerConnector };
```

### 3. Safety Configuration
```csharp
// Enable safety systems
switchTrack.safetyEnabled = true;
switchTrack.safetyCheckDistance = 10f;
switchTrack.requireClearance = true;
switchTrack.vehicleLayerMask = LayerMask.GetMask("Vehicle");
```

## Usage Examples

### Basic Track Switching
```csharp
// Switch to track 1
bool success = switchTrack.SwitchToTrack(1);

// Check if switching was successful
if (success)
{
    Debug.Log("Track switch initiated");
}
else
{
    Debug.Log("Track switch failed - check safety conditions");
}
```

### State Management
```csharp
// Lock the track
switchTrack.LockTrack();

// Unlock the track
switchTrack.UnlockTrack();

// Set maintenance mode
switchTrack.SetMaintenanceMode(true);

// Emergency stop
switchTrack.EmergencyStop();

// Reset error state
switchTrack.ResetError();
```

### Event Handling
```csharp
// Subscribe to events
switchTrack.OnTrackSwitched += (trackIndex) => {
    Debug.Log($"Switched to track {trackIndex}");
};

switchTrack.OnStateChanged += (state) => {
    Debug.Log($"State changed to {state}");
};

switchTrack.OnSafetyViolation += () => {
    Debug.LogWarning("Safety violation detected!");
};
```

## Safety Systems

### Clearance Checking
The system automatically checks for vehicles in the safety zone before allowing switches:
- **Safety Check Distance**: Configurable radius around switch
- **Vehicle Layer Mask**: Specifies which objects to check
- **Require Clearance**: Toggle for clearance requirement

### State-Based Safety
- **Locked State**: Prevents switching when locked
- **Maintenance Mode**: Disables switching during maintenance
- **Error State**: Requires manual reset after errors

## Visual Feedback

### Status Lights
Configure status lights to indicate switch state:
- **Green**: Idle/Ready
- **Yellow**: Switching
- **Blue**: Locked
- **Red**: Error
- **Orange**: Maintenance

### Materials
Assign different materials for each state:
- **Index 0**: Idle state material
- **Index 1**: Switching state material
- **Index 2**: Locked state material
- **Index 3**: Error state material
- **Index 4**: Maintenance state material

### Particle Effects
Add particle systems for visual effects during switching operations.

## Performance Optimization

### Recommended Settings
- **Switch Speed**: 2-5 units/second for smooth movement
- **Rotation Speed**: 90 degrees/second for turntables
- **Safety Check Distance**: 10-15 units based on vehicle size
- **Switch Delay**: 1-2 seconds for safety

### Memory Management
- Pool track section instances for multiple switches
- Use object pooling for effects and particles
- Optimize connection point arrays

## Integration with Ride Vehicle

### Vehicle Detection
The switch track system integrates with the RideVehicle system:
- Detects vehicles using layer masks
- Prevents switching when vehicles are in safety zone
- Provides feedback to vehicle controllers

### Track Connection
Track sections connect to the vehicle's waypoint or spline system:
- Connection points align with track waypoints
- Smooth transitions between track sections
- Automatic path recalculation after switches

## Testing and Debugging

### SwitchTrackTest.cs
Comprehensive test script for switch track operations:
- Manual track switching (1-4 keys)
- State control (L, M, E, R keys)
- Auto test sequence
- Visual debugging with gizmos

### Debug Features
- Gizmos show safety zones and connection points
- Console logging for all state changes
- Visual indicators for track positions
- Error reporting and safety violations

## Advanced Features

### Custom Switch Types
Extend the system with custom switch types:
```csharp
public enum SwitchType
{
    Y_Switch,
    Turntable,
    SlidingTrack,
    ElevatorTrack,
    RotatingSection,
    CustomSwitch  // Add your custom type
}
```

### Multi-Track Operations
Support for complex multi-track scenarios:
- Multiple switches in sequence
- Synchronized switching operations
- Track routing algorithms

### Integration with Ride Control
Connect to ride control systems:
- Automatic switching based on ride state
- Integration with block zoning
- Multi-train coordination

## Troubleshooting

### Common Issues
1. **Switch not working**: Check safety clearance and state
2. **Visual glitches**: Verify material assignments and effects
3. **Performance issues**: Reduce switch speed or safety check frequency
4. **Connection problems**: Ensure connection points are properly aligned

### Debug Steps
1. Check switch track state in inspector
2. Verify safety zone clearance
3. Test with debug script
4. Monitor console for error messages
5. Use gizmos to visualize connections

## Future Enhancements

### Planned Features
1. **Advanced Switch Types**: Scissors switches, diamond crossings
2. **Dynamic Track Generation**: Procedural track switching
3. **Network Synchronization**: Multi-player switch coordination
4. **AI Integration**: Automatic routing and switching
5. **Advanced Safety**: Predictive collision avoidance

### Research Integration
- Real-world switch track engineering principles
- Railway signaling systems
- Roller coaster safety standards
- Track maintenance procedures

## References
- Railway switch track engineering
- Roller coaster track design principles
- Unity state machine patterns
- Safety system design guidelines 