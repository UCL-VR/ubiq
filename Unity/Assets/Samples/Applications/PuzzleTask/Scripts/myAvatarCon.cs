using UnityEngine;
using System.Collections;

public class myAvatarCon : MonoBehaviour {
    //public FullBodyBipedIK ik;
    public Transform RightHandTarget;
    public Transform LeftHandTarget;

    GameObject localAvatar;
    GameObject remoteAvatar;

	// Use this for initialization
	void Start () {
        //ik = localAvatar.AddComponent<FullBodyBipedIK>();

    }
	
	// Update is called once per frame
	void Update () {
	
	}



    //void LateUpdate() {
    //    ik.solver.rightHandEffector.position = RightHandTarget.position;
    //    ik.solver.rightHandEffector.rotation = RightHandTarget.rotation;
    //    ik.solver.rightHandEffector.positionWeight = 1f;
    //    ik.solver.rightHandEffector.rotationWeight = 1f;

    //    ik.solver.leftHandEffector.position = LeftHandTarget.position;
    //    ik.solver.leftHandEffector.rotation = LeftHandTarget.rotation;
    //    ik.solver.leftHandEffector.positionWeight = 1f;
    //    ik.solver.leftHandEffector.rotationWeight = 1f;
    //}

}
