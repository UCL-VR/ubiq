using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class myBodyPos: MonoBehaviour {

    [SerializeField]
    private GameObject myHead;
    private GameObject myBody;
    private float myHight;
    private float myScale;
    /*  Quaternion rotation;
      void Awake() {
          rotation = transform.rotation;
      }
      void LateUpdate() {
          transform.rotation = rotation; //Y does not stay static
      }
      */

    // Use this for initialization
    void Start() {
        myHight = 1.6f;
        myScale = 1.0f;
        //make sure avatar's position at 0;
        int children = this.gameObject.transform.childCount;
        for (int i = 0; i < children; ++i) {
            this.gameObject.transform.GetChild(i).gameObject.transform.localPosition = new Vector3(0, 0, 0);
        }

    }

    // Update is called once per frame
    void Update() {

        if (Input.GetKeyDown("space")) {
            print("space key was pressed");
            myHight = myHead.transform.position.y;
            myScale = myHead.transform.position.y/1.5f;
        }

        int children = this.gameObject.transform.childCount;
        for (int i = 0; i < children; ++i) {
            this.gameObject.transform.GetChild(i).gameObject.transform.localScale = new Vector3(myScale, myScale, myScale);
         }


    float desiredAngle = myHead.transform.eulerAngles.y;
        Quaternion myrotation = Quaternion.Euler(0, desiredAngle, 0);
        gameObject.transform.rotation = myrotation;
     //   float myHeadDown = 0;
       float myHeadDown = myHead.transform.position.y - myHight;
       // print(myHeadDown);
        //makeing offset uptodate
        myHead.transform.localPosition = new Vector3(0,0, myHeadDown-0.3f);
        gameObject.transform.position = new Vector3(myHead.transform.position.x , 0.0f, myHead.transform.position.z);
   //     rbBody.transform.rotation = myHead.transform.rotation;
    }




}
