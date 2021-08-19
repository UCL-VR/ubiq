using UnityEngine;
using System.Collections;

public class ColorAnimator : MonoBehaviour {

    private GameObject _pieceObjForAnimation;

    private int _noOfTimesToBlink = 3;
    private bool _playEffect = false;
    private float _blinkSpeed = 9f;
    private Color _effectColor = Color.red;


    private int _currentBlinkNo = 0;
    private bool _isThisBlinkHigh = true; //High means going towards defined color , low towards white

    public bool IsPlaying
    {
        get { return _playEffect; }
    }

    void Awake ()
    {
        this.enabled = true;

        //For testing
        _pieceObjForAnimation = gameObject;
    }
	
	
	void Update () {
        
        if (_playEffect)
        {
            Color TempColorVal = _pieceObjForAnimation.GetComponent<Renderer>().material.color;

            if (_isThisBlinkHigh)
            {
                _pieceObjForAnimation.GetComponent<Renderer>().material.color = MoveTowards(TempColorVal,
                    _effectColor, Time.deltaTime * (_blinkSpeed*2));

                Color NewTempColorVal = _pieceObjForAnimation.GetComponent<Renderer>().material.color;

                if (Mathf.Abs(_effectColor.r - NewTempColorVal.r) < 0.3f &&
                        Mathf.Abs(_effectColor.g - NewTempColorVal.g) < 0.3f &&
                        Mathf.Abs(_effectColor.b - NewTempColorVal.b) < 0.3f)
                    _isThisBlinkHigh = false;
                
            }
            else
            {
                _pieceObjForAnimation.GetComponent<Renderer>().material.color = MoveTowards(TempColorVal, 
                    Color.white, Time.deltaTime * (_blinkSpeed*2) );

                Color NewTempColorVal = _pieceObjForAnimation.GetComponent<Renderer>().material.color;

                if (Mathf.Abs(Color.white.r - NewTempColorVal.r) < 0.3f &&
                        Mathf.Abs(Color.white.g - NewTempColorVal.g) < 0.3f &&
                            Mathf.Abs(Color.white.b - NewTempColorVal.b) < 0.3f)
                {
                    _isThisBlinkHigh = true;
                    _currentBlinkNo++;
                }

            }


            if (_currentBlinkNo > _noOfTimesToBlink)
            {
                ResetAnimator();
            }
            

        }

	}


    public void ResetAnimator()
    {
        _playEffect = false;
        _isThisBlinkHigh = true;
        _currentBlinkNo = 0;

        _pieceObjForAnimation.GetComponent<Renderer>().material.color = Color.white;

        _pieceObjForAnimation = null;
    }

    public void StartEffect(GameObject PieceObjForAnimation, Color EffectColor,
        int NoOfTimesToBlink = 1, float BlinkSpeed = 1f)
    {
        if (NoOfTimesToBlink > 0 && PieceObjForAnimation != null && BlinkSpeed > 0)
        {
            _noOfTimesToBlink = NoOfTimesToBlink;

            _pieceObjForAnimation = PieceObjForAnimation;

            _effectColor = EffectColor;

            _blinkSpeed = BlinkSpeed;

            _playEffect = true;
        }
    }

    private Color MoveTowards(Color MoveFrom, Color MoveTowards, float t)
    {
        Color ResultedColor = MoveFrom;

        ResultedColor.r = MoveFrom.r < MoveTowards.r ? (MoveFrom.r += t) : MoveFrom.r -= t;
        ResultedColor.g = MoveFrom.g < MoveTowards.g ? (MoveFrom.g += t) : MoveFrom.g -= t;
        ResultedColor.b = MoveFrom.b < MoveTowards.b ? (MoveFrom.b += t) : MoveFrom.b -= t;

        return ResultedColor;
    }

}
