using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

#if XRI_3_0_7_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Inputs;
#endif

#if WEBXR_0_22_1_OR_NEWER
using WebXR;
#endif

#if WEBXRINTERACTIONS_0_22_0_OR_NEWER
using WebXR.InputSystem;
#endif

#if INPUTSYSTEM_1_7_0_OR_NEWER
using UnityEngine.InputSystem;
#endif

#if XRCOREUTILS_2_2_0_OR_NEWER
using Unity.XR.CoreUtils;
#endif

namespace Ubiq.WebXR
{
    /// <summary>
    /// Attach required WebXR InputActions and managers when part of a WebXR
    /// build. This component will do nothing if the build target is not WebGL.
    /// </summary>
    public class UbiqWebXRInitOnWebGL : MonoBehaviour
    {
        [Tooltip("The InputActionReference to add to the InputActionManager component. If null, WebGL/WebXR builds will not function correctly. Note we use a ScriptableObject reference here rather than a direct reference to avoid serialization issues should the InputSystem package not be present.")]
        [SerializeField] private ScriptableObject inputActionAsset;
        [Tooltip("The GameObject containing the InputActionManager component that this component will add WebXR input actions to. If null, will try to find an InputActionManager in the scene at Start. Note we use a GameObject reference here rather than a direct reference to avoid serialization issues should the XR Interaction Toolkit not be present.")]
        [SerializeField] private GameObject inputActionManagerGameObject;
        [Tooltip("The GameObject containing the XROrigin. If null, will try to find an XROrigin in the scene at Start. Note we use a GameObject reference here rather than a direct reference to avoid serialization issues should XR CoreUtils not be present.")]
        [SerializeField] private GameObject xrOriginGameObject;
        [Tooltip("The GameObject containing the various scripts WebXR needs to run. If null, will try to find a GameObject containing the WebXRManager component in the scene at Start.")]
        [SerializeField] private GameObject webXRGameObject;
        
#if WEBXR_0_22_1_OR_NEWER && XRI_3_0_7_OR_NEWER && INPUTSYSTEM_1_7_0_OR_NEWER && XRCOREUTILS_2_2_0_OR_NEWER && UNITY_WEBGL 
        private InputActionAsset _inputActionAsset;
        private InputActionManager _inputActionManager;
        private XROrigin _xrOrigin;
        private GameObject _webXRGameObject;
        private WebXRManager _webXRManager;
        
        private void Start()
        {
            // Always verify input variables if we're using the WebGL
            // build target. We won't use it in the editor, but it gives the
            // user some prior warning their setup won't work before they do a
            // 5min+ WebGL build.
            VerifyAndGatherInput();
            
#if !UNITY_EDITOR
            if (_inputActionManager && _inputActionAsset)
            {
                _inputActionManager.actionAssets.Add(_inputActionAsset);
            }
            if (_webXRGameObject)
            {
                _webXRGameObject.SetActive(true);
            }
#endif
        }
        
        private void VerifyAndGatherInput()
        {
            VerifyAndGatherInputActionManager();
            VerifyAndGatherXROrigin();
            VerifyAndGatherWebXR();
            VerifyInputActionAsset();
        }
        
        private void VerifyAndGatherInputActionManager()
        {
            if (inputActionManagerGameObject)
            {
                _inputActionManager = inputActionManagerGameObject.GetComponent<InputActionManager>();
                
                if (!_inputActionManager)
                {
                    Debug.LogWarning("InputActionManagerGameObject supplied " +
                                     "but no InputActionManager component " +
                                     "could be found. Will attempt to find" +
                                     " an InputActionManager in scene.");
                }
            }
            
            if (!_inputActionManager)
            {
                _inputActionManager = FindObjectOfType<InputActionManager>();
                
                if (!_inputActionManager)
                {
                    Debug.LogWarning("No InputActionManager could be found. " +
                                     "This player rig will not function " +
                                     "correctly in WebGL/WebXR builds.");
                }
            }
        }
        
        private void VerifyAndGatherXROrigin()
        {
            if (xrOriginGameObject)
            {
                _xrOrigin = xrOriginGameObject.GetComponent<XROrigin>();
                
                if (!_xrOrigin)
                {
                    Debug.LogWarning("XROriginGameObject supplied but no " +
                                     "XROrigin component could be found. Will" +
                                     " attempt to find an XROrigin in scene.");
                }
            }
            
            if (!_xrOrigin)
            {
                _xrOrigin = FindObjectOfType<XROrigin>();
                
                if (!_xrOrigin)
                {
                    Debug.LogWarning("No XROrigin found. This player rig " +
                                     "will not function correctly in " +
                                     "WebGL/WebXR builds.");
                }
            }
        }
        
        private void VerifyAndGatherWebXR()
        {
            _webXRGameObject = webXRGameObject;
            if (!_webXRGameObject)
            {
                var manager = FindObjectOfType<WebXRManager>(includeInactive:true);
                _webXRGameObject = manager ? manager.gameObject : null;
                
                if (!_webXRGameObject)
                {
                    Debug.LogWarning("No WebXR GameObject found. This player rig " +
                                     "will not function correctly in WebGL/WebXR " +
                                     "builds.");
                }
            }
            if (_webXRGameObject)
            {
                _webXRManager = _webXRGameObject.GetComponent<WebXRManager>();
            
                if (!_webXRManager)
                {
                    Debug.LogWarning("No WebXRManager found on the WebXR " +
                                     "GameObject. This player rig " +
                                     "will not function correctly in " +
                                     "WebGL/WebXR builds.");
                }
            }
        }
        
        private void VerifyInputActionAsset()
        {
            if (!inputActionAsset)
            {
                Debug.LogWarning("No InputActionAsset supplied. This player " +
                                 "rig will not function correctly in " +
                                 "WebGL/WebXR builds.");
            }
            if (inputActionAsset)
            {
                _inputActionAsset = inputActionAsset as InputActionAsset;
                if (!_inputActionAsset)
                {
                    Debug.LogWarning("Supplied asset could not be converted " +
                                     "to an InputActionAsset. Is it of the " +
                                     "correct type? This player rig will not " +
                                     "function correctly in" +
                                     " WebGL/WebXR builds");
                }
            }
        }
        
#if !UNITY_EDITOR
        private enum State
        {
            Normal,
            XR
        }
        
        private State state = State.Normal;
        private float cameraYOffset = 0.0f;
        private float cameraFieldOfView = 0.0f; 

        private void Update()
        {
            if (!_xrOrigin || !_webXRManager)
            {
                return;
            }
            
            var isXR = _webXRManager.XRState == WebXRState.AR 
                       || _webXRManager.XRState == WebXRState.VR;
            if (isXR)
            {
                if (state != State.XR)
                {
                    // Entering XR state after being in normal
                    // Store camera fov
                    cameraFieldOfView = _xrOrigin.Camera.fieldOfView;

                    // Adjust camera offset
                    cameraYOffset = _xrOrigin.CameraYOffset;
                    _xrOrigin.CameraYOffset = 0.0f;
                    _xrOrigin.CameraFloorOffsetObject.transform.localPosition = 
                        Vector3.zero;

                    // Fire the event
                    Ubiq.XR.Notifications.XRNotifications.HmdMounted();
                }
                
                state = State.XR;
            }
            else
            {
                if (state == State.XR)
                {
                    // Entering normal state after being in XR
                    // Restore field of view
                    _xrOrigin.Camera.fieldOfView = cameraFieldOfView;

                    // Return camera offset to initial
                    _xrOrigin.CameraYOffset = cameraYOffset;
                    _xrOrigin.CameraFloorOffsetObject.transform.localPosition = 
                        Vector3.up * cameraYOffset;

                    // Fire the event
                    Ubiq.XR.Notifications.XRNotifications.HmdUnmounted();
                }

                _xrOrigin.Camera.transform.SetLocalPositionAndRotation(
                    Vector3.zero,Quaternion.identity);
                state = State.Normal;
            }
        }
#endif
#endif
    }
}

