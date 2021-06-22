# XR Interaction

Ubiq includes a straightforward XR interaction framework. This supports high level actions such as *Using* and *Grasping* 2D and 3D objects, as well as interacting with the [Unity UI system](https://docs.unity3d.com/Manual/com.unity.ugui.html).

Ubiq is not dependent on its own interaction system, and it is expected users may utilise the [Unity XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@1.0/manual/index.html), [MRTK](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/mrtk-getting-started), [VRTK](https://vrtoolkit.readme.io/) or another system for advanced functionality.

The Ubiq system is intended to support common XR requirements however, while being very simple to use, and transparently cross-platform. All the Ubiq Samples are created with the Ubiq interaction system.

### Cross Platform Support

The Ubiq interaction system is designed to work on both the desktop and in XR. This is achieved by maintaining two sets of input processing Components, that only respond to keyboard & mouse interactions, and XR controller interactions, respectively.

These Components are designed to co-exist on one set of GameObjects, allowing the same Player Prefab to be used for desktop and XR applications with no change.

For the interactables - 3D objects and 2D controls - identical events are recieved regardless of the source, so user code will work both for XR and the desktop transparently.


## 3D Interaction

Interacting with 3D objects is *action based*. Users can *Use* or *Grasp* objects. What these actions do is entirely up to user code.

For example, users could Use a button which spawns an object or turns on a light. They could Grasp a box which attaches to their hand, or a door which swings around an axis.

To implement these behaviours, Components implement `IUsable` or `IGraspable`. They will then recieve callbacks to `Use()`/`UnUse()` and `Grasp()`/`Release()`, respectively.

In XR, Players Use or Grasp objects by putting their controllers on an object and using the Trigger and Grip buttons. On the Desktop, users can use the cursor to click on objects with the Left or Middle mouse buttons.

Components implementing `IUsable` and `IGraspable` must be attached to objects with Colliders, though they do not need a RigidBody.

### Hands

The methods of `IUsable` and `IGraspable` are passed `Hand` instances. `Hand` represents an entity in 3D space (a `MonoBehaviour`) that can interact with other 3D objects. 

Other than existing in 3D, the `Hand` type is very abstract. It mainly exists to provide a 3D anchor, and to allow Components to distinguish between different controllers. A `Hand` does not have to have any physical presence (a RigidBody, or Collider). 

Implementations of `IUsable` and `IGraspable` should be prepared to be used or grasped by multiple `Hand` instances simultaneously.

### Graspers and Users

Calls to the `IUsable` and `IGraspable` implementations are made by the `UsableObjectUser` and `GraspableObjectGrasper` Components. These rely on the `HandController` Component, which implements the XR controller tracking and input processing based on the Unity XR namespace. There are desktop equivalents of `UsableObjectUser` and `GraspableObjectGrasper`. 

These references are to the concrete types and so it is not currently possible to use `UsableObjectUser` or `GraspableObjectGrasper` with custom hand implementations. Though it is possible to re-implement them and pass a custom `Hand` subclass to the `IUsable` and `IGraspable` implementations.

## 2D Interaction

The Ubiq XR interaction integrates with Unity's UI system. Players can raycast from their hands to interact with Unity Canvases and controls.

To enable Ubiq XR interaction with a Canvas, add the `XRUICanvas` component to it. Once this Component is added users can interact with the Unity controls using raycasts from the controllers, or the mouse cursor on the desktop.

When using the `XRUICanvas` an `EventSystem` is no longer required. Cameras are not required either on World Space Canvases, allowing them to be declared in Prefabs and instantiated dynamically.

### Raycasters

The 2D and 3D interaction mechanisms are separate. UI interaction is performed through UIRaycasters. There are XR and Desktop raycasters (`XRUIRaycaster` and `DesktopUIRaycaster`). Instances of both are attached to the sample Player Prefab.


## Player Controller

Users move through the world using Player Controllers. The Samples contain a Player Prefab with a camera and two hands. Player Controllers can move linearly or teleport. Interaction always occurs through a `Hand` instance, so a Player Controller is not technically necessary.
