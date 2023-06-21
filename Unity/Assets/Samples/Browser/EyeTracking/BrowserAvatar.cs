using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

public class BrowserAvatar : MonoBehaviour
{
    public NetworkId NetworkId { get => new NetworkId("b394b5c5-43e66da7"); }
    private NetworkContext context;

    public Transform RemoteCameraTransform;
    public Transform Head;
    public Transform LeftEye;
    public Transform RightEye;

    private Quaternion localHeadRotation;

    private SimpleMovingAverage eyeGazeDirection;
    private Vector3 eyeGazeLocation;

    private void Awake()
    {
        eyeGazeDirection = new SimpleMovingAverage(50);
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // These calls override the rotations from the animator.
        // It would be nice to blend these with IK, but Unity's
        // IK is not working...

        Head.localRotation = localHeadRotation;
    }

    public struct Message
    {
        public Vector3 position;
        public Vector3 head;
        public Vector3 eyes;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        var message = m.FromJson<Message>();

        // Transform into Unity's coordinate system

        // The head bone in the rocketbox avatars is at the root of the neck.
        // The rotations can be applied directly from the threejs camera local
        // rotations.

        var pitch = message.head.x * Mathf.Rad2Deg;
        var yaw = message.head.y * Mathf.Rad2Deg;
        var roll = message.head.z * Mathf.Rad2Deg;
      
        localHeadRotation = Quaternion.Euler(yaw, roll, pitch);

        // The eyes are given in normalised device coordinates, relative to the
        // remote camera (i.e. they need the rotation applied as well).

        var direction = message.eyes;

        // Clamp how far the eyes can move to avoid terrifying the VR user...

        direction.x = Mathf.Clamp(direction.x, -0.20f, 0.20f);
        direction.y = Mathf.Clamp(direction.y, -0.15f, 0.05f);
        direction.z = 1f / Mathf.Tan(direction.z * Mathf.Deg2Rad);
        direction.Normalize();

        var worldDirection  = Quaternion.Euler(-pitch, -yaw, roll) * eyeGazeDirection.Update(direction);

        // The gaze direction is quite noisy, so we should filter it to avoid
        // disturbing the vr users.

        eyeGazeLocation = (RemoteCameraTransform.position + worldDirection);

        LeftEye.LookAt(eyeGazeLocation);
        RightEye.LookAt(eyeGazeLocation);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(LeftEye.transform.position, eyeGazeLocation);
        Gizmos.DrawLine(RightEye.transform.position, eyeGazeLocation);
        Gizmos.DrawWireSphere(LeftEye.transform.position, 0.02f);
        Gizmos.DrawWireSphere(RightEye.transform.position, 0.02f);
    }
}


public class SimpleMovingAverage
{
    // Adapted from https://andrewlock.net/creating-a-simple-moving-average-calculator-in-csharp-1-a-simple-moving-average-calculator/

    private readonly int _k;
    private int _index = 0;

    private readonly Vector3[] _values;
    private Vector3 _sum = Vector3.zero;

    public Vector3 v;

    public SimpleMovingAverage(int k)
    {
        _k = k;
        _values = new Vector3[k];
    }

    public Vector3 Update(Vector3 nextInput)
    {
        _sum = _sum - _values[_index] + nextInput;
        _values[_index] = nextInput;
        _index = (_index + 1) % _k;
        v = _sum / (float)_k;
        return v;
    }
}