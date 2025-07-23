using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SkyCloudSpawner : MonoBehaviour
{
    public float spanMin;
    public float spanMax;
    public Transform[] skyCloud;

    Coroutine co = null;
    private float xPos;
    private int directionalForce = 1;

    public static SkyCloudSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    IEnumerator MakeCloud()
    {
        while (true)
        {
            //무작위 대기시간 설정
            float span = UnityEngine.Random.Range(spanMin, spanMax);
            yield return new WaitForSeconds(span);

            //무작위 y위치로 구름 생성
            xPos = transform.position.x;
            int idx = UnityEngine.Random.Range(0, skyCloud.Length);
            Transform t = Instantiate(skyCloud[idx]);
            t.position = new Vector2(xPos * directionalForce, transform.position.y + UnityEngine.Random.Range(-5, 5));
            CloudController cc = t.GetComponent<CloudController>();
            cc.flowSpeed *= directionalForce;
        }
    }

    public void Changedirection()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
        directionalForce *= -1;
        co = StartCoroutine(MakeCloud());
    }

    private void OnEnable()
    {
        xPos = transform.position.x;
        co = StartCoroutine(MakeCloud());
    }

    private void OnDisable()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }
}
