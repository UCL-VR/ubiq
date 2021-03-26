# Building a Basic Networked Object

1. Create a new Unity Script and add it to your GameObject that you want to have networked. You can do this via the inspector by clicking on &quot;Add Component&quot; and typing the new name.
 
 ![](images/bd892032-38e4-4ddd-bc38-0d10437cdcb6.png)


2. Include Ubik.Messaging

 ![](images/e80eacb1-1f1d-4ea9-a155-13664560eb88.png)


3. Inherit from INetworkObject and INetworkComponent
 
 ![](images/ca92236f-ed39-40fc-9a2f-0c0d223b14c6.png)


4. Implement their interfaces
 In Visual Studio this can be done through the context menu.
 Right Click -\&gt; Quick Actions and Refactoring -\&gt; Implement interface
**Note:** This will only give you the stubs. You will need to fill them in yourself in the next steps
 
 ![](images/b067cfe4-d3ee-4a50-911f-6a510f643822.png)


5. Implement Network ID creation
 
 ![](images/9237eeb3-44ef-493a-84a8-e633c23c9233.png)


6. Register your networked object with the network Scene
 This should be done at the start of the objects life i.e. in the Start() function.
 If you want to send messages as well, you also need to save the context object that is returned.
 
 ![](images/dd24ba78-6e46-472e-bd68-fda10fe36d97.png)


7. Define how your message will look like.
 This is best done as a struct in the class. It being defined in the class prevents naming conflict.
 In the message, write the variables that you want to send. A good start is TransformMessage that is built to store the transform and is useful if you want your object&#39;s location and orientation to be synchronised.
 Do not forget the constructor! It allows to create the message in one line.
 
 ![](images/641029e0-cb0c-4188-879c-66d2a0b35961.png)


8. Receiving Messages
 Messages are received automatically. However, you will have to define how they are processed.
 For that, fill in ProcessMessage(…) The first step is usually &quot;decoding&quot; the message. Usually it will be sent as a JSON, but if you send it in a different format, you need to decode it differently as well.

 ![](images/c4628137-6ac2-4ea5-8122-55a00742680e.png)


9. Sending Messages
 You can send messages at any time and anywhere in the code through using your context object. However, most of the time you will probably want that the objects move in sync, so it makes sense to send an update each frame. For this, put your sending in Update(…).
 
 ![](images/1dbfd8f4-7d2b-4651-8a28-0a5231603682.png)