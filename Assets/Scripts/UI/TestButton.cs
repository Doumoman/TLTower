using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestButton : MonoBehaviour
{
    public void Play()
    {
        AnimationManager.Instance.Play();
    }
    public void Lower()
    {
        CameraController.Instance.LowerBackgrounds();
    }
}
