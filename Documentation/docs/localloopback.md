# Testing Networked Objects

To enable you to test your networked objects without the help of a friend, the &quot;Hello World – Local Loopback&quot; scene is included in the Ubik samples. It has the scenery of the &quot;Hello World&quot; scene, but twice. Another addition is a local server that is automatically started when you play the scene and the scene is configured to connect both instances of the environment to it. This way, you create you have two connected clients in one active Unity scene and can test your networked objects without involving the UCL server or any other person.

![](images/6d75e6c4-a29b-458e-909c-847b7da0796d.png)

To use it, you have to import the Ubik Samples and open the scene in the samples\introduction folder. To start the loopback, simply click the play button. The local server is now started and you will see an avatar in the left forest, but not the right, because they have not joined the same room yet. For that, open the nodes for both forests and find the nodes with the name &quot;NetworkScene&quot; within (see left image below). In the inspector window you will find a button with the title &quot;join&quot; (see right image below). If you click it for both nodes, you will notice that the forest on the right now also has an avatar in it that moves in sync with the left one. The two clients are now connected and you can test your networked objects.

| ![](images/d9186eab-5ec2-4251-9078-25ee8af2b4de.png) | ![](images/13b07596-a1cf-4117-97b6-120d9cb803e1.png) |
| --- | --- |

