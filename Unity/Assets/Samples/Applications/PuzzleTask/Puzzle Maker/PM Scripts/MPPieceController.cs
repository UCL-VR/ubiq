using UnityEngine;
using System.Collections;

public class MPPieceController : MonoBehaviour
{

#region "Properties"

    private Texture2D _flippedPieceImage = null;
    public Texture2D FlippedPieceImage
    {
        get { return _flippedPieceImage; }
        set
        {
            if (value != null)
            {
                _flippedPieceImage = value;
            }
        }
    }

    
    private Texture2D _hiddenPieceImage = null;
    public Texture2D HiddenPieceImage
    {
        get { return _hiddenPieceImage; }
        set
        {
            if (value != null)
            {
                _hiddenPieceImage = value;
            }
        }
    }


    private bool _isPieceHidden = true;
    public bool IsPieceHidden
    { 
        get 
        { 
            return _isPieceHidden; 
        } 
    }


    private bool _isPieceFlipping = false;
    public bool IsPieceFlipping
    {
        get { return _isPieceFlipping; }
    }

#endregion


    private int _flipDirection = 1;

    private bool _HideOnShow = false;

    private const float FlipSpeed = 200f;


	void Start () {
        GetComponent<Renderer>().material.mainTexture = _flippedPieceImage;
	}

    void Update () {

        if (_isPieceFlipping)
        {
            gameObject.transform.Rotate(new Vector3(0, _flipDirection, 0), Time.deltaTime * FlipSpeed);

            if (gameObject.transform.rotation.eulerAngles.y >= 90 && _flipDirection == 1)
            {
                _flipDirection = -1;

                if (_isPieceHidden)
                {
                    gameObject.GetComponent<Renderer>().material.mainTexture = _hiddenPieceImage;
                }
                else
                {
                    gameObject.GetComponent<Renderer>().material.mainTexture = _flippedPieceImage;
                }

                OnPieceFlip180();
            }
            else if (gameObject.transform.rotation.eulerAngles.y <= Time.deltaTime * FlipSpeed && _flipDirection == -1)
            {
                _isPieceHidden = !_isPieceHidden;

                _flipDirection = 1;

                if (_HideOnShow)
                {
                    if (_isPieceHidden)
                    {
                        _isPieceFlipping = false;
                        gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                        OnPieceFlip360();
                    }
                    else
                    {
                        _HideOnShow = false;
                    }
                }
                else
                {
                    _isPieceFlipping = false;
                    gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    OnPieceFlip360();
                }

                

            }
            
        }

    }


    public void OnPieceFlip180()
    {
        
    }


    public void OnPieceFlip360()
    {
       
    }


    public void FlipPiece() {

        _HideOnShow = false;
        _flipDirection = 1;

        if (!_isPieceFlipping)
            _isPieceFlipping = true;
    }


    public void HideOnShow()
    {
        _HideOnShow = true;        
    }


}
