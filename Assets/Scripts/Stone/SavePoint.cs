using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SavePoint : MonoBehaviour
{
    StoneFixer fixer;
    bool alreadyTriggered;          // 중복 방지용

    public void Init(StoneFixer f)
    {
        fixer = f;
        CheckImmediateOverlap();    // 생성 직후 겹침 여부 확인
    }

    /* 새로운 돌이 들어오는 경우 */
    void OnTriggerEnter2D(Collider2D other) => TryTrigger(other);

    /* 이미 겹친 상태로 유지되는 경우 */
    void OnTriggerStay2D(Collider2D other) => TryTrigger(other);

    void TryTrigger(Collider2D other)
    {
        if (alreadyTriggered) return;

        var stone = other.GetComponent<StoneController>();
        if (stone && stone.state == StoneState.Settled)
        {
            alreadyTriggered = true;
            fixer?.FixAllStones();
            ChapterManager.Instance.ChangeChapter();    //챕터 변경 요청
            ResetStone.Instance.ColToSc(other);     //초기화 기준 돌 전달
        }
    }

    /* 최초 생성 직후 주변에 겹친 돌이 있는지 검사 */
    void CheckImmediateOverlap()
    {
        var col = GetComponent<Collider2D>();
        var results = new Collider2D[16];
        var filter = new ContactFilter2D { useTriggers = false };

        int cnt = col.OverlapCollider(filter, results);
        for (int i = 0; i < cnt; i++)
        {
            var stone = results[i].GetComponent<StoneController>();
            if (stone && stone.state == StoneState.Settled)
            {
                ResetStone.Instance.GetSc(stone);     //초기화 기준 돌 전달
                ChapterManager.Instance.ChangeChapter();    //챕터 변경 요청

                alreadyTriggered = true;
                fixer?.FixAllStones();
                break;
            }
        }
    }
}