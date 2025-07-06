using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider2D)), RequireComponent(typeof(Rigidbody2D))]
public class GlueStone : MonoBehaviour
{
    readonly List<StoneController> caught = new();
    bool fused;   // Fuse 한 번만 수행

    void OnTriggerEnter2D(Collider2D col)
    {
        if (fused) return;

        // 돌인지 확인
        if (!col.TryGetComponent(out StoneController sc)) return;

        // Settled 인 돌만 인정
        if (sc.state is not (StoneState.Settled)) return;
        // Fixed 돌이랑 부딪히면 제거
        if (sc.state is (StoneState.Fixed)) Destroy(gameObject); ;

        // 같은 Rigidbody(=이미 같은 덩어리)면 무시
        var rbThis = sc.GetComponent<Rigidbody2D>();
        if (caught.Exists(t => t.GetComponent<Rigidbody2D>() == rbThis)) return;

        caught.Add(sc);
        Debug.Log($"[Glue] add {sc.name}, now {caught.Count}");

        if (caught.Count >= 2)
            FuseNow();
    }
    void FuseNow()
    {
        fused = true;
        Debug.Log("[Glue] FuseNow");

        // 리더 선정--첫 번째 감지된 돌
        var leader = caught[0];
        var rbLead = leader.GetComponent<Rigidbody2D>();
        if (!rbLead) rbLead = leader.gameObject.AddComponent<Rigidbody2D>();
        rbLead.bodyType = RigidbodyType2D.Dynamic;   // 반드시 Dynamic/Static
        rbLead.gravityScale = 1;

        // 나머지 돌을 리더의 자식으로 옮기고 Rigidbody 제거
        for (int i = 1; i < caught.Count; ++i)
        {
            var sc = caught[i];
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
