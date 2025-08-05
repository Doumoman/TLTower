using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleHDR : MonoBehaviour
{
    [ColorUsage(true,true)]            // Inspector에서 HDR 슬라이더
    [SerializeField] Color[] _colorsHDR;

    ParticleSystemRenderer psr;
    static readonly int Emission = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        psr = GetComponent<ParticleSystemRenderer>();
        ApplyRandomColor();
    }

    public void ApplyRandomColor()
    {
        if (_colorsHDR == null || _colorsHDR.Length == 0) return;
        Color c = _colorsHDR[Random.Range(0, _colorsHDR.Length)];
        psr.material.SetColor(Emission, c);      // 머티리얼에 바로 HDR 컬러 주입
    }
}