using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    private int span;
    private int spanCount = 0;
    

    [Header("settings")]
    [Range(0, 1f)] public float birdChance = 0.4f;
    public int cycleSpanInit = 5; // 초기 생성 주기 (틱 단위)
    public int cycleSpanMin = 7; // 이후 주기 (틱 단위)
    public int cycleSpanMax = 10;
    public float sittime = 10f;
    public float waitAfterFeather = 8f;

    [Header("References")]
    public GameObject birdPoop;
    public GameObject bird;
    public GameObject feather;
    private bool firstBird = true;

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
                if (firstBird)
                {
                    BosalManager.Instance.Speak("BirdFirst");
                    firstBird = false;
                    BosalManager.Instance.birdBool = true;
                }
                else
                {
                    if (Random.value > 0.5f)
                    {
                        BosalManager.Instance.Speak("Bird");
                        BosalManager.Instance.birdBool = true;
                    }
                }
                    CreateBird(); //사운드 딜레이 이후 새 생성
                    span = Random.Range(cycleSpanMin, cycleSpanMax);
                    spanCount = span;
                    Debug.Log("BirdSpawner: Created a bird.");
                }
            else
            {
                CreateBirdPoop(); //사운드 딜레이 이후 새똥 생성
                span = Random.Range(cycleSpanMin, cycleSpanMax);
                spanCount = span;
                Debug.Log("BirdSpawner: Created a bird poop.");
            }
        }
    }
    GameObject MakeNotice()
    {
        GameObject go = Instantiate(feather);
        go.transform.position = this.transform.position;
        SoundManager.Instance.PlaySFX("bird_alert");
        return go;
    }
    //제일 높은 돌을 기준으로 일정 y좌표 위에서, 무작위로 위치 선정
    void RandomPoint()
    {
        float highY = StoneFixer.Instance.HighestSettledY;
        transform.position = new Vector2(Random.Range(-8.5f, 8.5f), highY + 10);
    }

    void GetStonePoint(out Vector2 hitPoint, out GameObject stone)
    {
        hitPoint = Vector2.zero;
        stone = null;

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
                BosalManager.Instance.Speak("BirdPeace");
                break;
            }
        }
    }
    void GetStonePoint(out Vector2 hitPoint) => GetStonePoint(out hitPoint, out GameObject _);

    void CreateBirdPoop() => StartCoroutine(DropBirdPoop());
    //랜덤 x좌표에 새똥 생성
    IEnumerator DropBirdPoop()
    {
        GetStonePoint(out Vector2 hitpoint);
        GameObject go = MakeNotice();
        yield return new WaitForSeconds(waitAfterFeather); //예고 발생 후 기다리기
        if (go) Destroy(go);

        if (hitpoint != Vector2.zero)
        {
            Instantiate(birdPoop, transform.position, Quaternion.Euler(0, 0, 90));
        }
        else
        {
            Debug.Log("can't find 'PlacedStone' by raycast");
            BosalManager.Instance.Speak("BirdPeace");
        }
    }

    void CreateBird() => StartCoroutine(SendBird());

    //랜덤 x좌표에서 PlacedStone의 표면에 앉는 새 생성
    IEnumerator SendBird()
    {
        GetStonePoint(out Vector2 hitPoint, out GameObject stone);
        GameObject go = MakeNotice();
        yield return new WaitForSeconds(waitAfterFeather);
        if (go) Destroy(go);

        if (hitPoint != Vector2.zero)
        {
            //hit 지점의 x좌표가 0이상이면 화면 오른쪽 밖에, 아니면 화면 왼쪽 밖에 생성
            GameObject aliveBird = (hitPoint.x >= 0) ? Instantiate(bird, new Vector2(15, hitPoint.y + 5), Quaternion.Euler(0, 0, 0)) : Instantiate(bird, new Vector2(-15, hitPoint.y + 5), Quaternion.Euler(0, 0, 0));
            aliveBird.GetComponent<Bird>().Init(stone, hitPoint, sittime);
        }
        else
        {
            Debug.Log("can't find 'PlacedStone' by raycast");
        }
    }
}
