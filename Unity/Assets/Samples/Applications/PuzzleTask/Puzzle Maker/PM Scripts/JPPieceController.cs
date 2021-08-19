using UnityEngine;
using System.Collections;
using PuzzleMaker;


public class JPPieceController : MonoBehaviour {

    [HideInInspector]
    public JPPuzzleController JpPuzzleControllerInstance;
    [HideInInspector]
    public SPieceInfo ThisPieceData;


	void Start () {
        gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 0.001f);
        //Adjust boxcollider according to joints and piece scale

    }
	
	void Update () {
	    
	}


    void OnTriggerStay (Collider ColObj)
    {

        if (ColObj.name.Contains("Piece"))
            if (transform.parent == null && transform.childCount == 0)
                OnTriggerEnter(ColObj);

    }

    void OnTriggerEnter (Collider ColObj) {


        if (ColObj.name.Contains("Piece"))
        {
            if (JpPuzzleControllerInstance.IsHoldingPiece())
            {
                
                // Is it a side collision  or top or bottom collision using collision range

                Vector3 CollisionVector = transform.position - ColObj.transform.position;
                CollisionVector.Normalize();

                float CollisionThreshold = JpPuzzleControllerInstance.PieceJoinSensitivity;
                float DirectionCollisionThreshold = 0.3f;

                GameObject ColGameObject = ColObj.gameObject;
                JPPieceController ColObjController = ColGameObject.GetComponent<JPPieceController>();

                //Check for correct or wrong piece collision for display of wrong piece animation
                if ( ThisPieceData.ID == JpPuzzleControllerInstance.HoldingPieceID() )
                    JpPuzzleControllerInstance.WrongPieceCollision(ColGameObject);

                if (CollisionVector.y < CollisionThreshold && CollisionVector.y > -CollisionThreshold)
                {
                    
                    if (CollisionVector.x < DirectionCollisionThreshold) //Right Collision
                    {
                        //Debug.Log("Right Collision Piece " + ThisPieceData.ID + " " + CollisionVector.normalized);
                        
                        if (ColObjController.ThisPieceData.HaveJoint(EJointPosition.Left))
                            OnRightCollision(ColObjController.ThisPieceData.ID, ColGameObject, ColObjController.ThisPieceData, ColObjController);
                    
                    }
                    else if (CollisionVector.x > -DirectionCollisionThreshold) //Left Collision
                    {
                        //Debug.Log("Left collision Piece " + ThisPieceData.ID + " " + CollisionVector.normalized);

                        if (ColObjController.ThisPieceData.HaveJoint(EJointPosition.Right))
                            OnLeftCollision(ColObjController.ThisPieceData.ID, ColGameObject, ColObjController.ThisPieceData, ColObjController);
                        
                    }

                }

                else if (CollisionVector.x < CollisionThreshold && CollisionVector.x > -CollisionThreshold)
                {

                    if (CollisionVector.y < DirectionCollisionThreshold) //Top Collision
                    {
                        //Debug.Log("Top Collision Piece " + ThisPieceData.ID + " " + CollisionVector.normalized);

                        if (ColObjController.ThisPieceData.HaveJoint(EJointPosition.Bottom))
                            OnTopCollision(ColObjController.ThisPieceData.ID, ColGameObject, ColObjController.ThisPieceData, ColObjController);
                        
                    }
                    else if (CollisionVector.y > DirectionCollisionThreshold) //Bottom collision
                    {
                        //Debug.Log("Bottom collision Piece " + ThisPieceData.ID + " " + CollisionVector.normalized);

                        if (ColObjController.ThisPieceData.HaveJoint(EJointPosition.Top))
                            OnBottomCollision(ColObjController.ThisPieceData.ID, ColGameObject, ColObjController.ThisPieceData, ColObjController);
                        
                    }

                }


            }
        }


    }



    void OnRightCollision(int CollidingPieceId, GameObject ColP, SPieceInfo ColPPieceData, JPPieceController ColObjController)
    {
        //Check if this is child of current holding piece then apply collision logic
        bool IsChildOfHoldingPiece = false;

        JPPieceController[] ChildrenControllers = transform.GetComponentsInChildren<JPPieceController>();
        foreach (JPPieceController item in ChildrenControllers)
        {
            if (ThisPieceData.ID == item.ThisPieceData.ID)
            {
                IsChildOfHoldingPiece = true;
                break;
            }
        }


        if (ThisPieceData.ID == JpPuzzleControllerInstance.HoldingPieceID() || IsChildOfHoldingPiece)
        {

            if (ThisPieceData.ID == CollidingPieceId - 1)
            {

                //If is child of holding piece make it parent
                //Make this piece parent of all
                Transform Temp = transform.root;
                transform.parent = null;
                Temp.parent = transform;



                JpPuzzleControllerInstance.UnholdPiece();

                Vector3 CalculatedPos = new Vector3();


                float LeftJointWorldScale = 0;
                float RightJointWorldScale = 0;
                float TopJointWorldScale = 0;
                float BotJointWorldScale = 0;

                float ColPLeftJointWorldScale = 0;
                float ColPRightJointWorldScale = 0;
                float ColPTopJointWorldScale = 0;
                float ColPBotJointWorldScale = 0;


                //Calculate required data
                CalculateDataForCollisionPlacement(ColPPieceData, out ColPLeftJointWorldScale, out ColPRightJointWorldScale,
                                        out ColPTopJointWorldScale, out ColPBotJointWorldScale);
                CalculateDataForCollisionPlacement(ThisPieceData, out LeftJointWorldScale, out RightJointWorldScale,
                                        out TopJointWorldScale, out BotJointWorldScale);

                //Calculate X
                //Calculated X without joints
                float CalcXWJ = ColP.transform.position.x - JpPuzzleControllerInstance.PieceWidthInWorld;


                CalculatedPos.x = CalcXWJ - (ColPRightJointWorldScale / 2) + (ColPLeftJointWorldScale / 2) +
                                            (RightJointWorldScale / 2) - (LeftJointWorldScale / 2);



                //Calculate Y
                CalculatedPos.y = ColP.transform.position.y + (TopJointWorldScale / 2) - (BotJointWorldScale / 2)
                                                        - (ColPTopJointWorldScale / 2) + (ColPBotJointWorldScale / 2);



                CalculatedPos.z = transform.position.z;

                transform.position = CalculatedPos;

                ColP.transform.root.parent = transform;

                OnPieceJoined();

                CheckForPuzzleComplete(transform);
            }
        }
        else
        {
            //Wrong Piece

        }
        
    }

    void OnLeftCollision(int CollidingPieceId, GameObject ColP, SPieceInfo ColPPieceData, JPPieceController ColObjController)
    {

        //Check if this is child of current holding piece then apply collision logic
        bool IsChildOfHoldingPiece = false;

        JPPieceController[] ChildrenControllers = transform.GetComponentsInChildren<JPPieceController>();
        foreach (JPPieceController item in ChildrenControllers)
        {
            if (ThisPieceData.ID == item.ThisPieceData.ID)
            {
                IsChildOfHoldingPiece = true;
                break;
            }
        }


        if (ThisPieceData.ID == JpPuzzleControllerInstance.HoldingPieceID() || IsChildOfHoldingPiece)
        {

            if (ThisPieceData.ID == CollidingPieceId + 1)
            {
                //If is child of holding piece make it parent
                //Make this piece parent of all
                Transform Temp = transform.root;
                transform.parent = null;
                Temp.parent = transform;


                JpPuzzleControllerInstance.UnholdPiece();

                Vector3 CalculatedPos = new Vector3();


                float LeftJointWorldScale = 0;
                float RightJointWorldScale = 0;
                float TopJointWorldScale = 0;
                float BotJointWorldScale = 0;

                float ColPLeftJointWorldScale = 0;
                float ColPRightJointWorldScale = 0;
                float ColPTopJointWorldScale = 0;
                float ColPBotJointWorldScale = 0;


                //Calculate required data
                CalculateDataForCollisionPlacement(ColPPieceData, out ColPLeftJointWorldScale, out ColPRightJointWorldScale,
                                        out ColPTopJointWorldScale, out ColPBotJointWorldScale);
                CalculateDataForCollisionPlacement(ThisPieceData, out LeftJointWorldScale, out RightJointWorldScale,
                                        out TopJointWorldScale, out BotJointWorldScale);



                //Calculate X

                //Calculated X without joints
                float CalcXWJ = ColP.transform.position.x + JpPuzzleControllerInstance.PieceWidthInWorld;


                CalculatedPos.x = CalcXWJ - (ColPRightJointWorldScale / 2) + (ColPLeftJointWorldScale / 2) +
                                            (RightJointWorldScale / 2) - (LeftJointWorldScale / 2);



                //Calculate Y
                CalculatedPos.y = ColP.transform.position.y + (TopJointWorldScale / 2) - (BotJointWorldScale / 2)
                                                        - (ColPTopJointWorldScale / 2) + (ColPBotJointWorldScale / 2);



                CalculatedPos.z = transform.position.z;

                transform.position = CalculatedPos;

                ColP.transform.root.parent = transform;

                OnPieceJoined();

                CheckForPuzzleComplete(transform);
            }
        }
        
    }

    void OnTopCollision(int CollidingPieceId, GameObject ColP, SPieceInfo ColPPieceData, JPPieceController ColObjController)
    {
        //Check if this is child of current holding piece then apply collision logic
        bool IsChildOfHoldingPiece = false;

        JPPieceController[] ChildrenControllers = transform.GetComponentsInChildren<JPPieceController>();
        foreach (JPPieceController item in ChildrenControllers)
        {
            if (ThisPieceData.ID == item.ThisPieceData.ID)
            {
                IsChildOfHoldingPiece = true;
                break;
            }
        }


        if (ThisPieceData.ID == JpPuzzleControllerInstance.HoldingPieceID() || IsChildOfHoldingPiece)
        {
            

            //Debug.Log(CollidingPieceId + "," + ThisPieceData.ID + " Top Collision");

            //Get colliding piece position in grid
            int CPElementRow = -1;
            int CPElementCol = -1;
            PPPuzzleController.ArrayPosToRC(CollidingPieceId, JpPuzzleControllerInstance.PiecesInCol,
                            JpPuzzleControllerInstance.PiecesInRow, out CPElementRow, out CPElementCol);

            

            //Get this piece position in grid
            int PElementRow = -1;
            int PElementCol = -1;
            PPPuzzleController.ArrayPosToRC(ThisPieceData.ID, JpPuzzleControllerInstance.PiecesInCol,
                            JpPuzzleControllerInstance.PiecesInRow, out PElementRow, out PElementCol);

            if (ThisPieceData.ID < CollidingPieceId && PElementCol == CPElementCol && PElementRow == CPElementRow - 1)
            {

                //If is child of holding piece make it parent
                //Make this piece parent of all
                Transform Temp = transform.root;
                transform.parent = null;
                Temp.parent = transform;

                JpPuzzleControllerInstance.UnholdPiece();

                Vector3 CalculatedPos = new Vector3();


                float LeftJointWorldScale = 0;
                float RightJointWorldScale = 0;
                float TopJointWorldScale = 0;
                float BotJointWorldScale = 0;

                float ColPLeftJointWorldScale = 0;
                float ColPRightJointWorldScale = 0;
                float ColPTopJointWorldScale = 0;
                float ColPBotJointWorldScale = 0;


                //Calculate required data
                CalculateDataForCollisionPlacement(ColPPieceData, out ColPLeftJointWorldScale, out ColPRightJointWorldScale,
                                        out ColPTopJointWorldScale, out ColPBotJointWorldScale);
                CalculateDataForCollisionPlacement(ThisPieceData, out LeftJointWorldScale, out RightJointWorldScale,
                                        out TopJointWorldScale, out BotJointWorldScale);




                //Calculate X
                CalculatedPos.x = ColP.transform.position.x - (ColPRightJointWorldScale / 2) + (ColPLeftJointWorldScale / 2) +
                                            (RightJointWorldScale / 2) - (LeftJointWorldScale / 2);

                //Calculate Y
                float CalcYWJ = ColP.transform.position.y - JpPuzzleControllerInstance.PieceHeightInWorld;
                CalculatedPos.y = CalcYWJ + (TopJointWorldScale / 2) - (BotJointWorldScale / 2)
                                                        - (ColPTopJointWorldScale / 2) + (ColPBotJointWorldScale / 2);


                CalculatedPos.z = transform.position.z;

                transform.position = CalculatedPos;

                ColP.transform.root.parent = transform;

                OnPieceJoined();

                CheckForPuzzleComplete(transform);
            }
        }
        

    }

    void OnBottomCollision(int CollidingPieceId, GameObject ColP, SPieceInfo ColPPieceData, JPPieceController ColObjController)
    {
        //Check if this is child of current holding piece then apply collision logic
        bool IsChildOfHoldingPiece = false;

        JPPieceController[] ChildrenControllers = transform.GetComponentsInChildren<JPPieceController>();
        foreach (JPPieceController item in ChildrenControllers)
        {
            if (ThisPieceData.ID == item.ThisPieceData.ID)
            {
                IsChildOfHoldingPiece = true;
                break;
            }
        }


        if (ThisPieceData.ID == JpPuzzleControllerInstance.HoldingPieceID() || IsChildOfHoldingPiece)
        {

            //Get colliding piece position in grid
            int CPElementRow = 0;
            int CPElementCol = 0;
            PPPuzzleController.ArrayPosToRC(CollidingPieceId, JpPuzzleControllerInstance.PiecesInCol,
                            JpPuzzleControllerInstance.PiecesInRow, out CPElementRow, out CPElementCol);


            //Get this piece position in grid
            int PElementRow = 0;
            int PElementCol = 0;
            PPPuzzleController.ArrayPosToRC(ThisPieceData.ID, JpPuzzleControllerInstance.PiecesInCol,
                            JpPuzzleControllerInstance.PiecesInRow, out PElementRow, out PElementCol);


            if (ThisPieceData.ID > CollidingPieceId && PElementCol == CPElementCol && PElementRow == CPElementRow + 1)
            {
                //If is child of holding piece make it parent
                //Make this piece parent of all
                Transform Temp = transform.root;
                transform.parent = null;
                Temp.parent = transform;


                JpPuzzleControllerInstance.UnholdPiece();

                Vector3 CalculatedPos = new Vector3();


                float LeftJointWorldScale = 0;
                float RightJointWorldScale = 0;
                float TopJointWorldScale = 0;
                float BotJointWorldScale = 0;

                float ColPLeftJointWorldScale = 0;
                float ColPRightJointWorldScale = 0;
                float ColPTopJointWorldScale = 0;
                float ColPBotJointWorldScale = 0;


                //Calculate required data
                CalculateDataForCollisionPlacement(ColPPieceData, out ColPLeftJointWorldScale, out ColPRightJointWorldScale,
                                        out ColPTopJointWorldScale, out ColPBotJointWorldScale);
                CalculateDataForCollisionPlacement(ThisPieceData, out LeftJointWorldScale, out RightJointWorldScale,
                                        out TopJointWorldScale, out BotJointWorldScale);


                //Calculate X
                CalculatedPos.x = ColP.transform.position.x - (ColPRightJointWorldScale / 2) + (ColPLeftJointWorldScale / 2) +
                                            (RightJointWorldScale / 2) - (LeftJointWorldScale / 2);

                //Calculate Y
                float CalcYWJ = ColP.transform.position.y + JpPuzzleControllerInstance.PieceHeightInWorld;
                CalculatedPos.y = CalcYWJ + (TopJointWorldScale / 2) - (BotJointWorldScale / 2)
                                                        - (ColPTopJointWorldScale / 2) + (ColPBotJointWorldScale / 2);


                CalculatedPos.z = transform.position.z;

                transform.position = CalculatedPos;

                ColP.transform.root.parent = transform;

                OnPieceJoined();

                CheckForPuzzleComplete(transform);
            }

        }
        
    }



    /// <summary>
    /// Used to calculate required data for joining of pieces together
    /// </summary>
    void CalculateDataForCollisionPlacement(SPieceInfo PieceData, out float LeftJointWorldScale, out float RightJointWorldScale,
                        out float TopJointWorldScale, out float BottomJointWorldScale)
    {

        bool LeftJointPresent = false;
        bool RightJointPresent = false;
        bool TopJointPresent = false;
        bool BotJointPresent = false;

        SJointInfo TempLeftJointInfo = PieceData.GetJoint(EJointPosition.Left, out LeftJointPresent);
        SJointInfo TempRightJointInfo = PieceData.GetJoint(EJointPosition.Right, out RightJointPresent);
        SJointInfo TempTopJointInfo = PieceData.GetJoint(EJointPosition.Top, out TopJointPresent);
        SJointInfo TempBotJointInfo = PieceData.GetJoint(EJointPosition.Bottom, out BotJointPresent);

        float PMPieceWidthWOJoint = (float)JpPuzzleControllerInstance.PuzzleMakerPieceWidthWithoutJoint;

        LeftJointWorldScale = LeftJointPresent && TempLeftJointInfo.JointType == EJointType.Male ?
            (float)TempLeftJointInfo.JointWidth / PMPieceWidthWOJoint : 0;
        RightJointWorldScale = RightJointPresent && TempRightJointInfo.JointType == EJointType.Male ?
            (float)TempRightJointInfo.JointWidth / PMPieceWidthWOJoint : 0;
        TopJointWorldScale = TopJointPresent && TempTopJointInfo.JointType == EJointType.Male ?
            (float)TempTopJointInfo.JointHeight / PMPieceWidthWOJoint : 0;
        BottomJointWorldScale = BotJointPresent && TempBotJointInfo.JointType == EJointType.Male ?
            (float)TempBotJointInfo.JointHeight / PMPieceWidthWOJoint : 0;

    }

    void CheckForPuzzleComplete(Transform rootTransform)
    {
     /*   int ChildCount = 0;

        Transform[] Temp = rootTransform.GetComponentsInChildren<Transform>();
        ChildCount += Temp.Length;

        if ((JpPuzzleControllerInstance.PiecesInCol * JpPuzzleControllerInstance.PiecesInRow) == ChildCount)
        {
            JpPuzzleControllerInstance.PuzzleCompleteCentrePosition(rootTransform.position);

            OnPuzzleComplete();
            JpPuzzleControllerInstance.PlaySFXSound(JpPuzzleControllerInstance.PuzzleCompletionSound);
        }*/

    }



    void OnPieceJoined()
    {
       // JpPuzzleControllerInstance.PlaySFXSound(JpPuzzleControllerInstance.PieceJoinedSound);

    }

    void OnPuzzleComplete()
    {
        Debug.Log("Puzzle successfully complete");
    }

}
