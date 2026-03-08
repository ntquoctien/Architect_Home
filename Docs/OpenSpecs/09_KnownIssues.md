# 09 Known Issues

## Current Tracking

### Missing Scripts Overload (Unity Editor Only)
Because the `Blueprint` and `Furniture` scripts were aggressively deleted from disk, any scenes that had them attached (`Room.unity` and `SampleScene.unity`) will now throw warnings in the Unity console upon loading.
*   **Workaround**: None required for runtime builds. In Editor, developers must manually locate the warning origin and right-click "Remove Component" on the missing script slots.
*   **Priority**: Low (Housekeeping).

### Wall Thickness at Extreme Angles
Ear clipping algorithm (`RoomMeshGenerator`) handles extreme internal angles correctly for the floor, but the calculation for exterior wall offset (`wallThickness`) may occasionally create intersecting corner posts if an angle is exceedingly sharp (e.g., < 15 degrees).
*   **Workaround**: Currently limited by user UI dragging rules. 
*   **Priority**: Medium.

## Future Epics
*   **No Multi-Room Collision**: Currently, if the procedural generator is instanced twice, the logic does not prevent two discrete room polygon meshes from overlapping perfectly.
*   **Door/Window Holes**: Non-convex walls generated dynamically do not currently support boolean operations (CSG) to punch holes for doors and windows.
