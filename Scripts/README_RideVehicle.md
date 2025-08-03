# RideVehicle Prefab Documentation

## Overview
The RideVehicle prefab is a comprehensive coaster vehicle system designed for realistic roller coaster simulation. It includes advanced physics, safety systems, and passenger experience simulation.

## Components

### 1. Rigidbody
- **Mass**: 2000kg (base + passenger weight)
- **Drag**: 0.1 (air resistance)
- **Angular Drag**: 0.05 (rotation damping)
- **Use Gravity**: Enabled
- **Constraints**: None (allows full 3D movement)

### 2. BoxCollider
- **Size**: 2 x 2.5 x 8 meters (width x height x length)
- **Center**: (0, 0, 0)
- **Is Trigger**: False (solid collision)

### 3. Scripts

#### WaypointFollower.cs
Basic waypoint following system for simple track navigation.

**Key Features:**
- Follows array of waypoint transforms
- Configurable speed and rotation
- Smooth acceleration/deceleration
- Loop functionality
- Visual debugging with gizmos

**Usage:**
```csharp
// Assign waypoints in inspector
waypointFollower.waypoints = trackWaypoints;
waypointFollower.speed = 15f;
waypointFollower.StartRide();
```

#### CoasterPhysics.cs
Advanced spline-based physics system for realistic coaster movement.

**Key Features:**
- Spline interpolation between track points
- Banking calculation based on curve direction
- Realistic gravity and friction simulation
- Air resistance modeling
- Configurable physics parameters

**Physics Parameters:**
- **Mass**: Vehicle mass (1000kg default)
- **Gravity**: 9.81 m/s²
- **Friction**: Track friction coefficient (0.02)
- **Air Resistance**: Velocity-dependent drag (0.01)
- **Max Speed**: 40 m/s (144 km/h)
- **Banking Angle**: 15° maximum

**Usage:**
```csharp
// Assign track points in inspector
coasterPhysics.trackPoints = trackPoints;
coasterPhysics.SetSpeed(20f);
coasterPhysics.ApplyBrake(10f);
```

#### RideVehicleController.cs
Main controller managing vehicle behavior and safety systems.

**Key Features:**
- State management (Loading, Ready, Moving, Braking, Stopped, EmergencyStop)
- Safety monitoring (speed, acceleration limits)
- Emergency brake system
- Passenger experience simulation
- Collision detection and response

**Safety Parameters:**
- **Max Safe Speed**: 35 m/s
- **Max Safe Acceleration**: 20 m/s²
- **Max Safe Deceleration**: 25 m/s²
- **Emergency Brake Force**: 15 m/s²
- **G-Force Threshold**: 3G

**Usage:**
```csharp
// Control ride states
rideController.StartRide();
rideController.StopRide();
rideController.EmergencyStop();

// Monitor vehicle state
float progress = rideController.GetProgress();
bool isOccupied = rideController.IsOccupied();
```

## Setup Instructions

### 1. Basic Setup
1. Drag the RideVehicle prefab into your scene
2. Assign track waypoints or track points
3. Configure physics parameters
4. Set up trigger zones for brake/speed control

### 2. Track Setup
For **WaypointFollower**:
- Create empty GameObjects as waypoints
- Position them along your desired track path
- Assign them to the `waypoints` array

For **CoasterPhysics**:
- Create track point GameObjects
- Position them to define the track spline
- Higher resolution = smoother movement
- Enable `useSplineInterpolation` for best results

### 3. Safety Zones
Create trigger zones with appropriate tags:
- **"BrakeZone"**: Automatically applies braking
- **"SpeedZone"**: Controls vehicle speed
- Add `SpeedZone` component for speed control

## Physics Research Integration

### B-Spline Based Track Simulation
The CoasterPhysics script implements concepts from coaster physics research:

1. **Spline Interpolation**: Uses linear interpolation between track points with configurable resolution
2. **Banking Calculation**: Applies banking based on curve direction and intensity
3. **Force Projection**: Projects gravity onto track normal for realistic movement
4. **Energy Conservation**: Models friction and air resistance for realistic speed changes

### Advanced Features to Implement
Based on research papers, consider adding:

1. **Catmull-Rom Splines**: For smoother curve interpolation
2. **Roller Coaster Dynamics**: More accurate force calculations
3. **Multi-Body Physics**: For articulated train cars
4. **Track Deformation**: Dynamic track response to vehicle weight

## Performance Optimization

### Recommended Settings
- **Spline Resolution**: 10-20 points per track segment
- **Fixed Timestep**: 0.02s (50 FPS) for stable physics
- **Collision Detection**: Continuous for high-speed movement
- **Interpolation**: Enable for smooth movement

### Memory Management
- Pool vehicle instances for multiple trains
- Use object pooling for effects and particles
- Optimize track point arrays for large tracks

## Troubleshooting

### Common Issues
1. **Vehicle not moving**: Check waypoints/track points are assigned
2. **Physics instability**: Reduce speed or increase mass
3. **Collision issues**: Adjust collider size or enable continuous collision detection
4. **Performance problems**: Reduce spline resolution or disable interpolation

### Debug Features
- Enable gizmos to visualize track paths
- Monitor console for safety warnings
- Use debug logs for state changes

## Future Enhancements

### Planned Features
1. **Multi-train Operations**: Support for multiple vehicles
2. **Advanced Banking**: Real-time banking calculation
3. **Passenger AI**: Simulated passenger reactions
4. **Sound Integration**: Wheel and track sound effects
5. **Visual Effects**: Sparks, smoke, and lighting

### Research Integration
- Implement B-spline interpolation
- Add roller coaster dynamics equations
- Include track stress analysis
- Support for vertical loops and inversions

## References
- "Roller Coaster Physics" by Tony Wayne
- "B-Spline Based Track Simulation" research papers
- Unity Physics documentation
- Real roller coaster design principles 