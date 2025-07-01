using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyChapterManager : MonoBehaviour
{
    public float gravityForce = 9.81f;
    [Range(0, 360)] public float seta = 270f;
    // Start is called before the first frame update
    void OnEnable()
    {
        float rad = seta * Mathf.PI / 180;
        Physics2D.gravity = new Vector2(gravityForce * Mathf.Sin(rad), gravityForce * Mathf.Cos(rad));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
