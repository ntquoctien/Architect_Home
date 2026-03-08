# Removal Plan: Add-on Systems

Based on the architectural analysis, here is the classification of all scripts related to the experimental Blueprint and Furniture systems, their dependencies, and the required cleanup steps.

## 1. Must Refactor First
Before deleting scripts, we must untangle one dependency in the core camera script.

*   **`CameraController.cs`**
    *   **Dependency**: Contains a hard reference to `FurniturePlacer` to block camera input while placing furniture.
    *   **Serialized Fields**: `public FurniturePlacer furniturePlacer;` and `public bool blockWhenPlacing = true;`.
    *   **Action**: Remove these fields and the `furniturePlacer.IsPlacing` check inside `ShouldBlockInput()`.

## 2. Safe to Delete
The following scripts have been verified to have no other hard script dependencies outside of their own isolated modules.

### Blueprint System
*   **`GridManager.cs`** (Singleton)
*   **`GridVisuals.cs`**
*   **`BlueprintInteraction.cs`**
*   **`BlueprintModeController.cs`** 
    *   **UI Callbacks**: Provides `ToggleBlueprintMode()` which is currently hooked up to a UI Button via UnityEvents in the `Room.unity` scene.

### Furniture System
*   **`FurnitureData.cs`** (ScriptableObject)
*   **`ObjectHoverInteractor.cs`** (Add-on feature exclusively used by `FurniturePlacer`)
*   **`FurnitureButton.cs`**
    *   **UI Callbacks**: Usually attached to UI Buttons to trigger `StartPlacement()`.
*   **`FurniturePlacer.cs`**

## 3. Manual Unity Scene/Prefab Cleanup Required
Because deleting scripts will leave "Missing Script" components and broken UnityEvents in your scenes, the following manual cleanup steps are required inside the Unity Editor. There are no prefabs containing these scripts, only scene objects.

### **Scene: `Room.unity`**
*   **Dormant/Active Objects**: Find and delete the GameObjects hosting `GridManager`, `GridVisuals`, `BlueprintInteraction`, and `BlueprintModeController` components.
*   **UI Callbacks**: Inspect your Canvas UI buttons (specifically the one intended for Blueprint mode). Find the `OnClick()` event that references `BlueprintModeController.ToggleBlueprintMode()` and remove the entire listener entry to prevent Editor warnings.


