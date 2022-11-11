# Creating a 3D Pen for Ubiq

In this short tutorial we'll make a pen that lets us draw shapes in mid-air. We'll add simple networking with Ubiq so you can share your drawings with others!

0) Download and install the Unity editor. We are using Unity 2020.3.40, but later versions should also work.

1) Create a new Unity project with the 3D template.

2) Download the Ubiq package for Unity (v0.3.0): [release page](https://github.com/UCL-VR/ubiq/releases/tag/v0.3.0), [direct file link](https://github.com/UCL-VR/ubiq/releases/download/v0.3.0/ubiq-0.3.0.zip)

3) Extract the Ubiq package into your new project's Packages folder. You should have the file structure: Packages/ubiq-0.3.0/Editor, as in the image below.

![](images/4b2f6bd0-10de-426d-91f5-194aabf68e18.png)

4) Open or return focus to your Unity editor and wait for Ubiq to be imported.

5) Open the Unity package manager with the top menu. The path is Window/Package Manager, highlighted in the image below.

![](images/7bf0b8de-037a-4f6e-a038-60d80577b575.png)

6) In the package manager, select Ubiq from the list on the left. In the pane on the right, click to expand the Samples dropdown, then Import to load the Ubiq samples. Wait for the import to complete.

![](images/bb403b80-4eff-444f-8e51-cc8659bd9803.png)

7) Open the Ubiq intro scene from the Samples: Assets/Samples/Ubiq/0.3.0/Samples/Start Here

8) Now we have everything we need, let's make a simple object for the pen. This can be anything you like! We will make a simple stand-in with one big cylinder for the grip and a tiny one for the nib. Right click in the hierarchy window and select Create Empty. Right click on the object in the hierarchy, select Rename, and give it the name "3DPen". This will be our parent object. We can use this to customize how our object is picked up. Now right click on 3DPen in the Hierarchy window and select 3D Object/Cylinder. Rename this cylinder "Grip". Right click on Grip and again select 3D Object/Cylinder. Rename this new cylinder "Nib". Your hierarchy should now read 3DPen/Grip/Nib, as in the image.

![](images/ea6a9af2-8ec6-43c2-977d-d2cee821cfca.png)

9) Scale, and translate the objects so they (sort of!) resemble a pen. Do not translate the top object (3DPen), as this will get moved when the user grabs it. Focus on just moving and scaling Grip and Nib. Do not worry about rotation now; we'll deal with this later. Tip: the scale of the sample is 1m equals 1 unit. Finally, position the pen somewhere you can easily reach it. You could try next to the menu. Here's what we ended up with:

![](images/d046700a-a462-4ef0-a7df-b85db38dc688.png)

10) Select 3DPen in the hierarchy window. Now, in the Inspector window, select Add Component and add a Rigidbody. In the Rigidbody, enable Is Kinematic.

![](images/099d09fc-c7ed-487e-895e-4c86033c964f.png)

11) Now let's write a script to help us pick up the pen and move it around. Select Add Component again, and type Pen. Unity will prompt you to add a new script with that name - select New Script, then Create and Add.

![](images/eb7cf431-5c49-41cb-b058-61fb2d06162c.png)

12) This will create the Pen script in your Assets folder and attach that script to the object. Open the file (Assets/Pen.cs) and replace its contents with the following:

```
using UnityEngine;
using Ubiq.XR;

// Implement Graspable interface, part of Ubiq XR interaction
// You can use any interaction toolkit you like with Ubiq!
// For the sake of keeping this tutorial simple, we use our simple in-built
// option.
public class Pen : MonoBehaviour, IGraspable
{
    private Hand controller;

    private void LateUpdate()
    {
        if (controller)
        {
            transform.position = controller.transform.position;
            transform.rotation = controller.transform.rotation;
        }
    }

    void IGraspable.Grasp(Hand controller)
    {
        this.controller = controller;
    }

    void IGraspable.Release(Hand controller)
    {
        this.controller = null;
    }
}
```

This implements the Graspable interface provided by Ubiq's XR interaction tools. You can use any interaction toolkit you like with Ubiq, but for the purpose of keeping this tutorial simple, we use our simple in-built option.

12) Enter Play mode by pressing the arrow at the top of the Unity Editor, or with the shortcut Ctrl-P. You can use Ubiq's desktop controls to walk over to the pen (WASD), look at it (hold right-click while moving the mouse), and grasp it (middle-mouse-button while the mouse is over the pen). When you move your view (as before, hold right-click while moving the mouse) again, the object should move with it.

13) Let's also build your application to test networking functionality. First, go to the top bar, then Edit/Project Settings. In the Project Settings window, select Player from the list on the left. In the pane on the right, click the dropdown next to Fullscreen Mode and select Windowed, then set the window to something small, like 640 x 480. This helps us test because we can see both the editor and the application running in a small window. Now, again in the top bar, go to File/Build and Run. Select a location for the build, and wait for the build to complete.

14) Now your application should be running in both the editor and as a windowed standalone app. To connect the two, we'll need them to both join the same room. On the editor, use your mouse to click the New button on the Ubiq UI panel in the scene. Leave the name as default, and click the arrow at the top right of the UI panel. Finally, select "No, keep my room private". The panel will change to show you a three letter code. This is the 'joincode' for your room. In your standalone windowed application, click Join on the Ubiq sample UI, enter this code, then click the arrow to submit.

15) Now you have two applications, both connected to the same room. On both, you should now see another avatar in the room with you. Try picking up the object as in Step 12. You'll see that it moves for the user who picked it up, but in the other application it stays still. This means we need to add some networking!

16) Replace your Pen script (Assets/Pen.cs) with the following:

```
using UnityEngine;
using Ubiq.XR;
using Ubiq.Messaging; // new

public class Pen : MonoBehaviour, IGraspable
{
    private NetworkContext context; // new
    private bool owner; // new
    private Hand controller;

    // new
    // 1. Define a message format. Let's us know what to expect on send and recv
    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;

        public Message(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.rotation;
        }
    }

    // new
    private void Start()
    {
        // 2. Register the object with the network scene. This provides a
        // NetworkID for the object and lets it get messages from remote users
        context = NetworkScene.Register(this);
    }

    // new
    public void ProcessMessage (ReferenceCountedSceneGraphMessage msg)
    {
        // 3. Receive and use transform update messages from remote users
        // Here we use them to update our current position
        var data = msg.FromJson<Message>();
        transform.position = data.position;
        transform.rotation = data.rotation;
    }

    // new
    private void FixedUpdate()
    {
        if (owner)
        {
            // 4. Send transform update messages if we are the current 'owner'
            context.SendJson(new Message(transform));
        }
    }

    private void LateUpdate()
    {
        if (controller)
        {
            transform.position = controller.transform.position;
            transform.rotation = controller.transform.rotation;
        }
    }

    void IGraspable.Grasp(Hand controller)
    {
        // 5. Define ownership as 'who holds the item currently'
        owner = true; // new
        this.controller = controller;
    }

    void IGraspable.Release(Hand controller)
    {
        // As 5. above, define ownership as 'who holds the item currently'
        owner = false; // new
        this.controller = null;
    }

     // Note about ownership: 'ownership' is just one way of designing this
     // kind of script. It's sometimes a useful pattern, but has no special
     // significance outside of this file or in Ubiq more generally.
}
```

New lines are functioned are marked with a comment. This script does a number of important things, marked in the code.

17) Test again as described in Steps 12-14. You should now see that when the object is grasped and moved in one application, it also moves in the other!

18) Now let's get the pen to draw in 3D space! Replace Pen.cs with the following:

```
using UnityEngine;
using Ubiq.XR;
using Ubiq.Messaging;

public class Pen : MonoBehaviour, IGraspable, IUseable // new
{
    private NetworkContext context;
    private bool owner;
    private Hand controller;
    private Transform nib; // new
    private Material drawingMaterial; // new
    private GameObject currentDrawing; // new

    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;

        public Message(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.rotation;
        }
    }

    private void Start()
    {
        nib = transform.Find("Grip/Nib"); // new
        context = NetworkScene.Register(this);
        var shader = Shader.Find("Particles/Standard Unlit"); // new
        drawingMaterial = new Material(shader); // new
    }

    public void ProcessMessage (ReferenceCountedSceneGraphMessage msg)
    {
        var data = msg.FromJson<Message>();
        transform.position = data.position;
        transform.rotation = data.rotation;
    }

    private void FixedUpdate()
    {
        if (owner)
        {
            context.SendJson(new Message(transform));
        }
    }

    private void LateUpdate()
    {
        if (controller)
        {
            transform.position = controller.transform.position;
            transform.rotation = controller.transform.rotation;
        }
    }

    void IGraspable.Grasp(Hand controller)
    {
        owner = true;
        this.controller = controller;
    }

    void IGraspable.Release(Hand controller)
    {
        owner = false;
        this.controller = null;
    }

    // new
    void IUseable.Use(Hand controller)
    {
        BeginDrawing();
    }

    // new
    void IUseable.UnUse(Hand controller)
    {
        EndDrawing();
    }

    // new
    private void BeginDrawing()
    {
        currentDrawing = new GameObject("Drawing");
        var trail = currentDrawing.AddComponent<TrailRenderer>();
        trail.time = Mathf.Infinity;
        trail.material = drawingMaterial;
        trail.startWidth = .05f;
        trail.endWidth = .05f;
        trail.minVertexDistance = .02f;

        currentDrawing.transform.parent = nib.transform;
        currentDrawing.transform.localPosition = Vector3.zero;
        currentDrawing.transform.localRotation = Quaternion.identity;
    }

    // new
    private void EndDrawing()
    {
        var trail = currentDrawing.GetComponent<TrailRenderer>();
        currentDrawing.transform.parent = null;
        currentDrawing.GetComponent<TrailRenderer>().emitting = false;
        currentDrawing = null;
    }
}
```

19) Test as in steps 12-14. You should now be able to draw a line in the air with the pen! This is intuitive in virtual reality - pick up the item and grasp buttons/triggers, and use with main button/trigger. The desktop interface is fiddly for this, but okay for debug: First, click on the pen to 'use' it - you should see a debug message in the Unity editor if successful. Then, while still holding left mouse to use, hold right mouse to move your view around. But you'll notice that the line is only drawn locally so far - the remote user does not yet see it. We'll change that in the next step.

20) Time to add networking to our drawings! Replace Pen.cs with this final version:

```
using UnityEngine;
using Ubiq.XR;
using Ubiq.Messaging;

// Adds simple networking to the 3d pen. The approach used is to draw locally
// when a remote user tells us they are drawing, and stop drawing locally when
// a remote user tells us they are not.
public class Pen : MonoBehaviour, IGraspable, IUseable
{
    private NetworkContext context;
    private bool owner;
    private Hand controller;
    private Transform nib;
    private Material drawingMaterial;
    private GameObject currentDrawing;

    // Amend message to also store current drawing state
    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool isDrawing; // new

        public Message(Transform transform, bool isDrawing)
        {
            this.position = transform.position;
            this.rotation = transform.rotation;
            this.isDrawing = isDrawing; // new
        }
    }

    private void Start()
    {
        nib = transform.Find("Grip/Nib");
        context = NetworkScene.Register(this);
        var shader = Shader.Find("Particles/Standard Unlit");
        drawingMaterial = new Material(shader);
    }

    public void ProcessMessage (ReferenceCountedSceneGraphMessage msg)
    {
        var data = msg.FromJson<Message>();
        transform.position = data.position;
        transform.rotation = data.rotation;

        // new
        // Also start drawing locally when a remote user starts
        if (data.isDrawing && !currentDrawing)
        {
            BeginDrawing();
        }
        if (!data.isDrawing && currentDrawing)
        {
            EndDrawing();
        }
    }

    private void FixedUpdate()
    {
        if (owner)
        {
            // new
            context.SendJson(new Message(transform,isDrawing:currentDrawing));
        }
    }

    private void LateUpdate()
    {
        if (controller)
        {
            transform.position = controller.transform.position;
            transform.rotation = controller.transform.rotation;
        }
    }

    void IGraspable.Grasp(Hand controller)
    {
        owner = true;
        this.controller = controller;
    }

    void IGraspable.Release(Hand controller)
    {
        owner = false;
        this.controller = null;
    }

    void IUseable.Use(Hand controller)
    {
        BeginDrawing();
    }

    void IUseable.UnUse(Hand controller)
    {
        EndDrawing();
    }

    private void BeginDrawing()
    {
        currentDrawing = new GameObject("Drawing");
        var trail = currentDrawing.AddComponent<TrailRenderer>();
        trail.time = Mathf.Infinity;
        trail.material = drawingMaterial;
        trail.startWidth = .05f;
        trail.endWidth = .05f;
        trail.minVertexDistance = .02f;

        currentDrawing.transform.parent = nib.transform;
        currentDrawing.transform.localPosition = Vector3.zero;
        currentDrawing.transform.localRotation = Quaternion.identity;
    }

    private void EndDrawing()
    {
        var trail = currentDrawing.GetComponent<TrailRenderer>();
        currentDrawing.transform.parent = null;
        currentDrawing.GetComponent<TrailRenderer>().emitting = false;
        currentDrawing = null;
    }
}
```

And we're done! Test it again as with steps 12-14, and if you have a headset available, see how it feels in VR!

You might notice that drawings are not visible to new joining users. A more advanced implementation would store the points of the drawings in Peer or Room properties, so new users could see them when they join. If you do try it, let us know how you get on!