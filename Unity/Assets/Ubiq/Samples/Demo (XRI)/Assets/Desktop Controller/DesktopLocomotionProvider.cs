using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class DesktopLocomotionProvider : ContinuousMoveProviderBase
{
    public InputActionReference Move;

    private void Start()
    {
        Move.action.Enable();

        if(!forwardSource)
        {
            forwardSource = system.xrOrigin.Camera.transform;
        }
    }

    protected override Vector2 ReadInput()
    {
        return Move.action.ReadValue<Vector2>() * moveSpeed;
    }
}
