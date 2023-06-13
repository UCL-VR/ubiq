using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.XR;

public class ControlPanelDoorComponent : MonoBehaviour
{
    private static NetworkId experimentNamespace = new NetworkId("9ea1be44-a29787fd");

    private NetworkContext context;

    public AnimationCurve Curve;

    public ControlPanelDoorBuzzer Buzzer;

    private float time;
    private Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this, NetworkId.Create(experimentNamespace, gameObject.name));
        startPosition = transform.localPosition;
        Buzzer.OnBuzz.AddListener(Notify);
    }

    void Notify()
    {
        context.Send("Notify");
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = startPosition + transform.right * Curve.Evaluate(time);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        switch(message.ToString())
        {
            case "Open":
                StartCoroutine(OpenDoor());
                break;
        }
    }

    private IEnumerator OpenDoor()
    {
        context.Send("Opening");
        while (time < 1f)
        {
            time += Time.deltaTime;
            yield return 0;
        }
        time = 1f;
        context.Send("Opened");
    }
}
