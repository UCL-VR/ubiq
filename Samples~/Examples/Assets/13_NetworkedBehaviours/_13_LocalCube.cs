using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Examples
{
    public class _13_LocalCube : MonoBehaviour
    {
        void Update()
        {
            var time = Mathf.PingPong(Time.time,1);

            var maxPosition = new Vector3(0,1.5f,0);
            var minPosition = new Vector3(0,0.5f,0);
            var t = Mathf.SmoothStep(0,1,time);
            transform.localPosition = Vector3.Lerp(minPosition,maxPosition,t);

            var minRotation = Quaternion.Euler(0,-30.0f,0);
            var maxRotation = Quaternion.Euler(0,30.0f,0);
            t = Mathf.SmoothStep(0,1,time);
            transform.localRotation = Quaternion.Slerp(minRotation,maxRotation,t);

            var minColor = Color.green;
            var maxColor = Color.blue;
            t = Mathf.SmoothStep(0,1,time);
            var color = Color.Lerp(minColor,maxColor,t);
            GetComponent<_13_NetworkedBehaviourCube>().color = color;
        }
    }
}