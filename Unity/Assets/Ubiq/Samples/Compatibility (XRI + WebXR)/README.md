This prefab can be used to attach required WebXR managers when part part of a WebGL build. It will also fix various small issues when going into and out of XR mode.  

## To make Ubiq's 'Demo (XRI)' sample WebXR compatible ##

1. Open the Demo scene from the Demo (XRI) sample.
2. Drag in the 'Ubiq Player Bridge (WebXR)' prefab from this sample.

You're done! The WebXR plugin is compatible with XRI interactions, so nothing else needs changing. This works thanks to the (excellent!) WebXR-Interactions plugin by De-Panther (https://github.com/De-Panther/unity-webxr-export), which builds on the original WebVR plugin by Mozilla.

## (Optional) Switching to the Universal Render Pipeline ##

The Universal Render Pipeline (URP) does not seem to be strictly required to build for WebXR, but is recommended by WebXR-Export. It also lets us use a pre-defined Render Pipeline Asset to help performance on lower end devices.

1. Import the URP package (com.unity.render-pipelines.universal)
2. In Edit > Project Settings > Graphics, set the Scriptable Render Pipeline Settings asset to WebXR_PipelineAsset

## (If using a Unity version prior to Unity 6) Switch to Gamma color space ##

As with URP, gamma space shading is not a strict requirement, but is recommended by WebXR-Export. This is likely because the WebGL DXT-compression extension Unity uses to make linear space shading possible does not appear to be supported on mobile. 

NOTE: Specifically for Unity 6, linear color space is the default for new Unity projects, and is also compatible with WebXR-Export. This means you can likely skip this step. If you are on Unity 6 and using a Gamma color space, follow the steps below, but instead change the color space to Linear.

A linear color space is more true to life, but shading in gamma space can look fine too - what's important is just that if you do plan to switch for your project, do it early! After switching, your existing scenes will look different in the other color space due to changes in how lighting is calculated.

1. Go to Edit > Project Settings > Player, unfold the Other Settings group and change Color Space to Gamma. 
2. Repeat for all your platforms (may update automatically).

## Setting up your Unity project to build for WebXR ##

1. In File > Build Settings..., select the WebGL platform and click Switch Platform (Unity may provide a button to install the required editor build plugin first)
2. In Project Settings > XR Plug-in Management, select the WebGL tab and ensure the tick box next to WebXR Export is ticked
3. Import web templates: Window > WebXR > Copy WebGLTemplates
4. Select a web template: Go to Edit > Project Settings > Player, click the WebGL tab, and select one of the WebXR templates

You should now be ready to build. Both Ubiq and WebXR require a secure context. The Build and Run option in File > Build Settings... will host the application on a simple local web server and open a page in your default browser. This is a good way to test as your browser will likely permit insecure local contexts to behave as secure. Later on when you host your application on the wider web for others to use, you will need appropriate X.509 certificates (e.g., through Let's Encrypt).
