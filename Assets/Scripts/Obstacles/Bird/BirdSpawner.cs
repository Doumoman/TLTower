using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    private int span;
     private int spanCount = 0;
    public static float cycle;

    [Header("settings")]
    [Range(0, 1f)] public float birdChance = 0.4f;
    public int cycleSpanInit = 5; // 초기 생성 주기 (틱 단위)
    public int cycleSpanMin = 7; // 이후 주기 (틱 단위)
    public int cycleSpanMax = 10;

    [Header("References")]
    public GameObject birdPoop;
    public GameObject bird;
    // Start is called before the first frame update
    void Start()
    {
        span = Random.Range(cycleSpanInit, cycleSpanMax);
        spanCount = span;

        TickManager.Instance.OnTickEvent += TickEvent;
    }

    private void TickEvent(object sender, System.EventArgs eventArgs)
    {
        /*
        if (spanCount == 사운드 길이){
        Play("BirdAlert", Soundtype.MELODY, 0);
        }
        */

        if (spanCount-- <= 0)
        {
            if (Random.value < birdChance)
            {
                //Play("BirdSpawn", SoundType.SFX, 0); // 사운드 플레이
                BosalManager.Instance.BirdSpeak();
                CreateBird(); //사운드 딜레이 이후 새 생성
                span = Random.Range(cycleSpanMin, cycleSpanMax);
                spanCount = span;
                Debug.Log("BirdSpawner: Created a bird.");
            }
            else
            {
                //Play("BirdPoopSpawn", SoundType.SFX, 0); // 사운드 플레이
                CreateBirdPoop(); //사운드 딜레이 이후 새똥 생성
                span = Random.Range(cycleSpanMin, cycleSpanMax);
                spanCount = span;
                Debug.Log("BirdSpawner: Created a bird poop.");
            }
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
        BosalManager.Instance.BirdPoopSpeak();
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
            if (stone != null)
            {
                BosalManager.Instance.BirdPeaceSpeak();
                break;
            }
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
