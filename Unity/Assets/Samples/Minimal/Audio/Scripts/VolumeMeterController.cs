using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeMeterController : MonoBehaviour
{
    private Image image;
    private Material material;

    public float Volume;

    private void Awake()
    {
        image = GetComponent<Image>();
        material = image.material; // take a copy
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        material.SetFloat("_Volume", Volume);
    }
}
