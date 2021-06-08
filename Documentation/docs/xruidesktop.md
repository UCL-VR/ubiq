The Ubiq sample code includes both desktop and XR interation.

The Desktop and XR Interaction is implemented in separate components. 
The Desktop Components only respond to keyboard and mouse inputs, and the XR Components only to XR controllers.
This allows the Components for both to exist in parallel and the same Player Prefab to function on the Desktop and in XR without any change.

## Desktop Controls

On the Desktop, the Mouse Cursor will be visible by default.

### Navigation

The WSAD keys can be used to move around the environment. Hold the Right Mouse button to turn left and right or look up and down.
Teleport by holding the Alt key. The hand which controls the teleport ray can be rotated by holding the Shift Key and moving the mouse.

### UI

UI components can be interacted with by clicking them as if in a regular desktop GUI application.

### Usable

Usable objects can be Used by holding the Left Mouse Button. They will be Unused when the mouse button is released.

### Graspable

Graspable objects can be grasped by middle-clicking once. They can be ungrasped by clicking again (either in empty space, or on a new Graspable).



