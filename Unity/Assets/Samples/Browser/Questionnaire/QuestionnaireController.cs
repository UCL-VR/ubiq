using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

public class QuestionnaireController : MonoBehaviour
{
    private NetworkContext context;

    public NetworkId NetworkId => new NetworkId("4caddaa6-5627a5fa");

    private bool questionnaireCompleted;

    private void Start()
    {
        context = NetworkScene.Register(this);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        switch (m.ToString())
        {
            case "QuestionnaireComplete":
                questionnaireCompleted = true;
                return;
        }
    }

    public IEnumerator DoQuestionnaire()
    {
        questionnaireCompleted = false;
        context.Send(context.Scene.Id.ToString());
        context.Send("DoQuestionnaire");
        while(!questionnaireCompleted)
        {
            yield return 0;
        }
    }
}
