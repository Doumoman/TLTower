using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdPoop : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject obj = collision.gameObject;
        //접촉 대상이 돌이면 고정 조인트 형성
        if (obj.tag == "FixedStone" || obj.tag == "PlacedStone")
        {
            FixedJoint2D joint2d = gameObject.AddComponent<FixedJoint2D>();
            joint2d.connectedBody = obj.GetComponent<Rigidbody2D>();
        }
    }
}
