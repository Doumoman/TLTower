using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchController : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] SidePanelSpawner sidePanel;

    [Header("Rotate")]
    [SerializeField] float holdSec = .75f;
    [SerializeField] float rotateSpeed = 90f;

    Camera cam;
    readonly Dictionary<int, TouchInfo> fingers = new();

    void Awake() => cam = Camera.main;

    void Update()
    {
        foreach (var t in Input.touches)
        {
            if (t.phase == TouchPhase.Began && fingers.Count >= 2) continue; // 두 손가락 제한

            if (!fingers.TryGetValue(t.fingerId, out var info))
            {
                info = new TouchInfo();
                fingers[t.fingerId] = info;
            }

            switch (t.phase)
            {
                case TouchPhase.Began: Begin(t, info); break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary: MoveOrRotate(t, info); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled: End(info); fingers.Remove(t.fingerId); break;
            }
        }
    }

    #region --Phase 처리--
    void Begin(Touch t, TouchInfo info)
    {
        if (EventSystem.current.IsPointerOverGameObject(t.fingerId)) return; // UI 터치 무시

        var hit = Physics2D.Raycast(cam.ScreenToWorldPoint(t.position), Vector2.zero);
        if (hit && hit.transform.TryGetComponent(out Stone s))
        {
            info.stone = s;
            info.offset = s.transform.position - cam.ScreenToWorldPoint(t.position);
            info.startT = Time.time;
            info.rotating = false;

            s.SetGrabbed(true);

            // 슬롯에서 꺼낸 첫 순간이면 패널에 알림
            if (s.FromSideSlot)
            {
                s.FromSideSlot = false;
                sidePanel.NotifySlotFreed(s);
                s.SetPhysics(false);
            }
        }

    }

    void MoveOrRotate(Touch t, TouchInfo info)
    {
        if (!info.stone) return;

        if (!info.rotating && Time.time - info.startT >= holdSec)
            info.rotating = true;

        if (info.rotating)
        {
            info.stone.transform.Rotate(Vector3.forward, -rotateSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 target = cam.ScreenToWorldPoint(t.position) + info.offset;
            target.z = 0;
            info.stone.transform.position = target;
        }
    }

    void End(TouchInfo info)
    {
        if (info.stone)
        {
            info.stone.SetGrabbed(false);   // ← 원래 있던 메서드
            info.stone.SetPhysics(true);    // 떨어뜨리기 시작
        }
        info.Reset();
    }
    #endregion

    class TouchInfo
    {
        public Stone stone;
        public Vector3 offset;
        public float startT;
        public bool rotating;
        public void Reset() { stone = null; }
    }
}
