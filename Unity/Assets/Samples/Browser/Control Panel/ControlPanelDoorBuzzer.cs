using System.Collections;
using System.Collections.Generic;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.Events;

public class ControlPanelDoorBuzzer : MonoBehaviour, IUseable
{
    private void Awake()
    {
        OnBuzz = new UnityEvent();
    }

    public void UnUse(Hand controller)
    {
    }

    public void Use(Hand controller)
    {
        OnBuzz.Invoke();
    }

    public UnityEvent OnBuzz;
}
