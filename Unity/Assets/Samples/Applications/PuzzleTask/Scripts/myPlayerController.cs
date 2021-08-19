using UnityEngine;

//[RequireComponent(typeof(ConfigurableJoint))]
[RequireComponent(typeof(myPlayerMotor))]

public class myPlayerController : MonoBehaviour {

	[SerializeField]
	private float speed = 5f;

	
	private myPlayerMotor motor;
    //private ConfigurableJoint joint;
    public GameObject Lhandsposition;
    public GameObject Rhandsposition;

    [SerializeField]
    private GameObject myHead;
    private GameObject myBody;
    private float myHight;
    private float myScale;

    void Start ()
	{
		motor = GetComponent<myPlayerMotor>();
        myHight = 1.6f;
        myScale = 1.0f;
        //make sure avatar's position at 0;
        myBody = this.gameObject.transform.Find("Body").gameObject;
        int children = myBody.transform.childCount;
        for (int i = 0; i < children; ++i) {
            myBody.transform.GetChild(i).gameObject.transform.localPosition = new Vector3(0, 0, 0);
        }
    }
	
	void Update ()
	{
		//Calculate movement velocity as a 3D vector
		float _xMov = Input.GetAxisRaw("Horizontal");
		float _zMov = Input.GetAxisRaw("Vertical");
		
		Vector3 _movHorizontal = transform.right * _xMov;
		Vector3 _movVertical = transform.forward * _zMov;
		
		// Final movement vector
		Vector3 _velocity = (_movHorizontal + _movVertical).normalized * speed;
		
		//Apply movement
		motor.Move(_velocity);
        this.gameObject.transform.Find("TransLeft").gameObject.transform.position = Lhandsposition.transform.position;
        this.gameObject.transform.Find("TransLeft").gameObject.transform.rotation = Lhandsposition.transform.rotation;
        this.gameObject.transform.Find("TransRight").gameObject.transform.position = Rhandsposition.transform.position;
        this.gameObject.transform.Find("TransRight").gameObject.transform.rotation = Rhandsposition.transform.rotation;

        //body position
        if (Input.GetKeyDown("space")) {
            print("space key was pressed");
            myHight = myHead.transform.position.y;
            myScale = myHead.transform.position.y / 1.55f;
        }

        int children = myBody.transform.childCount;
        for (int i = 0; i < children; ++i) {
            myBody.transform.GetChild(i).gameObject.transform.localScale = new Vector3(myScale, myScale, myScale);
        }


        float desiredAngle = myHead.transform.eulerAngles.y;
        Quaternion myrotation = Quaternion.Euler(0, desiredAngle, 0);
        myBody.transform.rotation = myrotation;
        //   float myHeadDown = 0;
        // float myHeadDown = (myHead.transform.position.y - myHight)*1.2f;
        // print(myHeadDown);
        //makeing offset uptodate
        Vector3 offset = myHead.transform.forward;
        offset.y = 0;
        offset.Normalize();
        offset = offset * -0.2f;

        //        myHead.transform.localPosition = new Vector3(0, 0, -0.2f);
//        myHead.transform.localPosition = offset;
        myHead.transform.position = myHead.transform.parent.position + offset;
        myBody.transform.position = new Vector3(myHead.transform.position.x, 0.0f, myHead.transform.position.z);
        //     rbBody.transform.rotation = myHead.transform.rotation;
    }
}
