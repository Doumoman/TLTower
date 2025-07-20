using System.Collections;
using System.Collections.Generic;
using FMOD;
using UnityEngine;

public class BirdPoop : MonoBehaviour
{
    readonly List<StoneController> caught = new(); 
    StoneController anchor;
    bool fused;   // Fuse 한 번만 수행
    float VoiceRate = 2 / 3;

    void PlaySound(float rate = 1)
    {
        SoundManager.Instance.PlaySFX("bird_poop");
        if (Random.value > rate) BosalManager.Instance.Speak("BirdPoop");
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (fused) return;

        // 돌인지 확인
        if (!col.TryGetComponent(out StoneController sc)) return;


        // Settled 인 돌만 인정
        if (sc.state != StoneState.Settled && sc.state != StoneState.Dropping)
            return;

        PlaySound(VoiceRate);

        // Fixed 돌이랑 부딪히면 제거
        if (sc.state is (StoneState.Fixed)) Destroy(gameObject); ;

        // 같은 Rigidbody(=이미 같은 덩어리)면 무시
        if (anchor == null)
        {
            StickTo(sc);   // 여기서 새똥 정지 & 고정
            return;        // 두 번째 돌을 기다린다
        }

        if (sc == anchor || caught.Contains(sc)) return;

        caught.Add(sc);                                   // 두 번째 돌 등록
        UnityEngine.Debug.Log($"[Glue] add {sc.name}, now {caught.Count}");
        SoundManager.Instance.PlaySFX("stone_connect");

        if (caught.Count >= 2)
            FuseNow();
    }
    void StickTo(StoneController first)
    {
        anchor = first;
        caught.Add(first);

        // Rigidbody2D 를 그대로 두되 완전히 ‘멈춘’ 상태로 바꿈
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // 중력·충돌력 無
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        // 필요하면 회전/이동 모두 잠그기
        // rb.constraints = RigidbodyConstraints2D.FreezeAll;
        foreach (var myCol in GetComponents<Collider2D>())
            foreach (var stCol in first.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(myCol, stCol, true);
        // 돌에 자식으로 붙이기 → 돌을 드래그하면 새똥도 같이 이동
        transform.SetParent(first.transform, true);
    }
    void FuseNow()
    {
        if (anchor == null) return;
        fused = true;
        UnityEngine.Debug.Log("[Glue] FuseNow");

        // 리더 선정--첫 번째 감지된 돌
        var leader = anchor;
        var rbLead = leader.GetComponent<Rigidbody2D>();
        if (!rbLead) rbLead = leader.gameObject.AddComponent<Rigidbody2D>();
        rbLead.bodyType = RigidbodyType2D.Dynamic;   // 반드시 Dynamic/Static
        rbLead.gravityScale = 1;

        // 나머지 돌을 리더의 자식으로 옮기고 Rigidbody 제거
        for (int i = 1; i < caught.Count; ++i)
        {
            var sc = caught[i];
            if (sc == leader) continue;
            sc.Glued();
            sc.transform.SetParent(leader.transform, true);
            var rb = sc.GetComponent<Rigidbody2D>();
            if (rb) Destroy(rb);
            sc.enabled = false;
        }

        // leader 자신 포함 모든 PolygonCollider2D 는 그대로 두면
        //     자동으로 리더 RB에 귀속됨 (Composite 불필요)
        Physics2D.SyncTransforms();
        Destroy(gameObject);   // 새똥 오브젝트 제거
    }
}


//public class BirdPoop : MonoBehaviour
//{
//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        GameObject obj = collision.gameObject;
//        //접촉 대상이 돌이면 고정 조인트 형성
//        if (obj.tag == "FixedStone" || obj.tag == "PlacedStone")
//        {
//            FixedJoint2D joint2d = gameObject.AddComponent<FixedJoint2D>();
//            joint2d.connectedBody = obj.GetComponent<Rigidbody2D>();
//        }
//    }
//}
