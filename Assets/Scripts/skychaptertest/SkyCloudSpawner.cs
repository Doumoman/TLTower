using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SkyCloudSpawner : MonoBehaviour
{
    public float spanMin;
    public float spanMax;
    public Transform skyCloud;

    Coroutine co = null;
    private float xPos;

    IEnumerator MakeCloud()
    {
        while (true)
        {
            //무작위 대기시간 설정
            float span = Random.Range(spanMin, spanMax);
            yield return new WaitForSeconds(span);

            //무작위 y위치로 구름 생성
            xPos = transform.position.x;
            Transform t = Instantiate(skyCloud);
            t.position = new Vector2(xPos, transform.position.y + Random.Range(-5, 5));
        }
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
        }
    }
}
