using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindSystem : MonoBehaviour
{
    private const float Z_SIZE = 8.5f;
    private const float X_POSITION = 15;

    [Header("References")]
    public ParticleSystem wind;
    public ParticleSystem windcol;
    public float force = 100f;
    public float width = 6f;
    public bool left = false;   //후에 방해 시스템 매니저 스크립트에 의존하도록 변형 예정

    private void OnEnable()
    {
        //왼쪽 오르쪽 설정
        if (left)
        {
            wind.transform.position = new Vector2(-X_POSITION, StoneFixer.Instance.HighestSettledY);
            wind.transform.rotation = Quaternion.Euler(0, 90, 90);
        }
        else
        {
            wind.transform.position = new Vector2(X_POSITION, StoneFixer.Instance.HighestSettledY);
            wind.transform.rotation = Quaternion.Euler(0, -90, 90);
        }

        //크기 설정
        ParticleSystem.ShapeModule shape = wind.shape;
        shape.scale = new Vector3(width, 1, Z_SIZE);
        shape = windcol.shape;
        shape.scale = new Vector3(width, 1, Z_SIZE);
        //힘 설정
        ParticleSystem.CollisionModule collision = windcol.collision;
        collision.colliderForce = force;

        wind.Play();
        
    }
    private void OnDisable()
    {
        wind.Stop();
    }
}
