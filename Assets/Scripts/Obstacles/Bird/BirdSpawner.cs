using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    private int span;
    private float time;
    public static float cycle;

    [Header("settings")]
    [Range(0, 1f)] public float birdChance = 0f;
    public float cycleValue = 4f;

    [Header("References")]
    public GameObject birdPoop;
    public GameObject bird;
    // Start is called before the first frame update
    void Start()
    {
        span = Random.Range(5, 11);
        time = 0f;
        cycle = cycleValue;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time >= cycle * span)
        {
            if (Random.value > birdChance) CreateBirdPoop();
            else CreateBird();
            span = Random.Range(1, 11);
            time = 0f;
        }

    }
    void MakeNotice()
    {

    }
    //제일 높은 돌을 기준으로 일정 y좌표 위에서, 무작위로 위치 선정
    void RandomPoint()
    {
        float highY = StoneFixer.Instance.HighestSettledY;
        transform.position = new Vector2(Random.Range(-8.5f, 8.5f), highY + 10);
    }
    //랜덤 x좌표에 새똥 생성
    void CreateBirdPoop()
    {
        RandomPoint();
        Instantiate(birdPoop, transform.position, Quaternion.Euler(0, 0, 90));
    }
    //랜덤 x좌표에서 PlacedStone의 표면에 앉는 새 생성
    void CreateBird()
    {
        
        Vector2 hitPoint = new Vector2();
        GameObject stone = null;
        RandomPoint();
        int safty = 100;
        while (safty-- > 0)
        {
            RandomPoint();
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, 20);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject.tag == "PlacedStone")
                {
                    hitPoint = hit.point;
                    stone = hit.collider.gameObject;
                    break;
                }
            }
            if (stone != null) break;
        }
        if (hitPoint != Vector2.zero)
        {
            //hit 지점의 x좌표가 0이상이면 화면 오른쪽 밖에, 아니면 화면 왼쪽 밖에 생성
            GameObject aliveBird = (hitPoint.x >= 0) ? Instantiate(bird, new Vector2(15, hitPoint.y + 5), Quaternion.Euler(0, 0, 0)) : Instantiate(bird, new Vector2(-15, hitPoint.y + 5), Quaternion.Euler(0, 0, 0));
            aliveBird.GetComponent<Bird>().Init(stone, hitPoint);
        }
        else
        {
            Debug.Log("can't find 'PlacedStone' by raycast");
        }
    }
}
