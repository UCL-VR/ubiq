# Asyncrhonous Design Patterns in Unity

The Unity process manages the main thread, which begins before any user code is executed. Most Unity resources can only be accessed from the main thread; an exception will be thrown otherwise.
There are still many possibilities for writing aysnchronous code however.

## Design Pattern

Delayed initialisation with callbacks. Mimics the do-then pattern in JS. Methods are called which take Actions. Those Actions are initialised by lambdas. The lambas execution thread depends on the called function.

```
void Start()
{
	factory.GetRtcConfiguration(config =>
	{
		pc = factory.CreatePeerConnection(config, this);
	});           
}
```

## Design Pattern

Message pumps with Update. Commonly used in the mid-level networking code, this pattern uses a list of actions to execute methods on the main thread.

```
class RoomClient
{
	private List<Action> actions = new List<Action>();

	public void SendToServer(Message message)
	{
		actions.Add(() =>
		{
			SendToServerSync(message);
		});
	}
	
	private void Update()
	{
		foreach (var action in actions)
		{
			action();
		}
		actions.Clear();
	}
}
```


## Design Pattern

Commonly used in webrtc code for objects that take time to initialise because they are waiting on external resources. This pattern uses coroutines to effectively poll a resource, conditionally executing operations on the main thread.

```
	void Start()
	{
		factory.GetRtcConfiguration(config =>
		{
			pc = factory.CreatePeerConnection(config, this);
		});           
	}

	private IEnumerator WaitForPeerConnection(Action OnPcCreated)
	{
		while (pc == null)
		{
			yield return null;
		}
		OnPcCreated();
	}

	public void AddLocalAudioSource()
	{
		StartCoroutine(WaitForPeerConnection(() =>
		{
			var audiosource = factory.CreateAudioSource();
			var audiotrack = factory.CreateAudioTrack("localAudioSource", audiosource);
			pc.AddTrack(audiotrack, new[] { "localAudioSource" });
		}));
	}
```