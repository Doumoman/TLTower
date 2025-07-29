using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public ParticleSystem[] psArray;

    private void OnEnable()
    {
        foreach (ParticleSystem ps in psArray) ps.Play();
    }

    private void OnDisable()
    {
        foreach (ParticleSystem ps in psArray) ps.Stop();
    }
}
