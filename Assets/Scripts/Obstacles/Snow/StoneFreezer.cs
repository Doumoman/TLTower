using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneFreezer : MonoBehaviour
{
    [Header("확인용")]
    [SerializeField] float unFreezeTimer = 0f;
    [SerializeField] float freezeTimer = 0f;
    PolygonCollider2D[] cols;
    PhysicsMaterial2D normal;
    Coroutine coroutine;
    bool isrunning = false;
    bool isTouchingSnow = false;

    private bool freezeOnStart = false;
    private bool isFreezed = false;
    public bool playTest = false;       //작동 테스트용

    ParticleSystem ps;
    GameObject snowParticle;
    SpriteRenderer spriteRenderer;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    [Header("references")]
    public PhysicsMaterial2D frozen;

    [Header("Settings")]
    public float freezeTime;
    public float unfreezeTime;
    public float timerResetTime;
    public UnityEngine.Color color;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        snowParticle = GameObject.Find("SnowParticle");
        ps = snowParticle.GetComponent<ParticleSystem>();
        if (FreezerSettler.Instance == null) Destroy(this);

        //FreezerSettler를 통해 값 설정
        Sprite spr = spriteRenderer.sprite;
        float[] timeSetting = FreezerSettler.Instance.GetTime(spr);
        freezeTime = timeSetting[0];
        unfreezeTime = timeSetting[1];
        timerResetTime = timeSetting[2];
        color = FreezerSettler.Instance.color;
        freezeOnStart = FreezerSettler.Instance.freezeOnStart;

        if (freezeOnStart)
        {
            FreezeStone();
            freezeOnStart = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //픽스되면 비활성화
        if (gameObject.tag == "FixedStone")
        {
            if (isFreezed) UnFreeze();
            this.enabled = false;
            return;
        }

        //얼고나서 일정 시간 후 원상태로 되돌림
        if (isFreezed)
        {
            unFreezeTimer += Time.deltaTime;
            if (unFreezeTimer >= unfreezeTime)
            {
                UnFreeze();
                unFreezeTimer = 0f;
            }
        }

        //테스트용
        if (playTest)
        {
            playTest = false;
            isFreezed = true;
            FreezeStone();
        }

        //파티클에 닿은 채로 일정 시간 지나면 얼음
        if (freezeTimer > 0 && freezeTimer > freezeTime)
        {
            FreezeStone();
            SoundManager.Instance.PlaySFX("stone_freeze");
            freezeTimer = 0f;
        }

        if (isTouchingSnow)
        {
            freezeTimer += Time.deltaTime;
            int numEvents = ParticlePhysicsExtensions.GetCollisionEvents(ps, gameObject, collisionEvents);

            //닿은 파티클이 없으면 타이머 초기화
            if (!isrunning && numEvents == 0)
            {
                isTouchingSnow = false;
                coroutine = StartCoroutine(ResetTimer());
            }
        }
    }

    IEnumerator ResetTimer()
    {
        isrunning = true;
        yield return new WaitForSeconds(timerResetTime);
        if (!isTouchingSnow) freezeTimer = 0f;
        isrunning = false;
    }

    //돌의 색과 마찰을 바꿈
    public void FreezeStone()
    {
        isFreezed = true;
        
        cols = GetComponents<PolygonCollider2D>();
        foreach (PolygonCollider2D col in cols)
        {
            if (col.sharedMaterial != null)
            {
                normal = col.sharedMaterial;
                col.sharedMaterial = frozen;
                break;
            }
        }
        spriteRenderer.color = color;
    }

    //돌의 색과 마찰 원래대로
    public void UnFreeze()
    {
        cols = GetComponents<PolygonCollider2D>();
        foreach (PolygonCollider2D col in cols)
        {
            if (col.sharedMaterial == frozen)
            {
                col.sharedMaterial = normal;
                break;
            }
        }
        spriteRenderer.color = UnityEngine.Color.white;
        isFreezed = false;
    }

    //snow파티클과 충돌시 시간 재기
    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Snow") && !isFreezed)
        {
            if (coroutine != null) StopCoroutine(coroutine);
            isrunning = false;
            isTouchingSnow = true;
        }
    }
}
