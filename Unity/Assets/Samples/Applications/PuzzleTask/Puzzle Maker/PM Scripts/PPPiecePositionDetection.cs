using UnityEngine;
using System.Collections;

public class PPPiecePositionDetection: MonoBehaviour {

    [HideInInspector]
    public PPPuzzleController PPPuzzleControlerInstance;


    void OnTriggerEnter(Collider ColObj)
    {
        string ObjName = ColObj.name;

        if (ObjName.Contains("Piece"))
        {
            int PieceNumber = System.Convert.ToInt32( ObjName.Substring(5, ObjName.Length - 5) );
            int ThisObjNumber = System.Convert.ToInt32(gameObject.name.Substring(18, gameObject.name.Length - 18));

            if (PieceNumber == ThisObjNumber) //On correct piece place
            {
                gameObject.SendMessageUpwards("PiecePlaceFound", ThisObjNumber, SendMessageOptions.RequireReceiver);
            }
            else
            {
                if (PPPuzzleControlerInstance.AnimateColorOnWrongPiece)
                {
                    PPPuzzleControlerInstance.gameObject.GetComponent<ColorAnimator>().StartEffect(ColObj.gameObject,
                                PPPuzzleControlerInstance.WrongPieceAnimationColor, PPPuzzleControlerInstance.NoOfTimesToBlink,
                                PPPuzzleControlerInstance.BlinkSpeed);
                    PPPuzzleControlerInstance.PlayWrongPlacementSound();
                }
            }
        }
    }

}
