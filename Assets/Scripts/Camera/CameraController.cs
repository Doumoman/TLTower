using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    /* ───────── Drag Follow ───────── */
    [Header("Drag Follow")]
    [SerializeField] float dragMaxOffset = 2f;
    [SerializeField] float dragSmooth = 4f;

    /* ───────── Height Snap ───────── */
    [Header("Snap Heights (World-Y)")]
    public float[] snapLevels = { 4f, 8f, 12f, 16f, 20f };
    [SerializeField] float snapSpeed = 4f;

    /* ───────── Panel Follow ───────── */
    [Header("Slot Panel (optional)")]
    [Tooltip("슬롯/패널 부모 Transform. 비우면 패널 이동 안 함")]
    public Transform slotPanel;

    bool snapping;
    Camera cam;
    StoneController dragTarget;
    Vector3 basePos;       // 드래그 시작 시 기준점
    float targetY;       // 스냅 타겟 높이
    float initialZ;
    float panelYOffset;
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = GetComponent<Camera>();
        targetY = transform.position.y;
        initialZ = transform.position.z;
    }

    public void BeginDrag(StoneController sc)
    {
        dragTarget = sc;
        basePos = transform.position;
    }
    public void EndDrag() => dragTarget = null;

    public void TrySnapFromStone(float stoneY)
    {
        // 현 카메라보다 높은 snapLevel 중 가장 낮은 값을 찾음
        foreach (float lvl in snapLevels)
        {
            if (lvl > targetY + 0.01f && stoneY >= lvl)
            {
                targetY = lvl;
                snapping = true;
                basePos.y = lvl;      
                break;
            }
        }
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;

        if (snapping)
        {
            pos.y = Mathf.MoveTowards(pos.y, targetY, snapSpeed * Time.deltaTime);
            if (Mathf.Abs(pos.y - targetY) < 0.02f) snapping = false;
        }

        if (dragTarget && dragTarget.state == StoneState.Dragging)
        {
            Vector2 dir = ((Vector2)Input.mousePosition -
                          new Vector2(Screen.width, Screen.height) * 0.5f).normalized;

            Vector3 desired = basePos + (Vector3)(dir * dragMaxOffset);
            desired.y = pos.y;               
            desired.z = initialZ;            
            pos = Vector3.Lerp(pos, desired, Time.deltaTime * dragSmooth);
        }
        else
        {
            pos = Vector3.Lerp(pos,
                    new Vector3(basePos.x, pos.y, initialZ),
                    Time.deltaTime * dragSmooth);
        }

        pos.z = initialZ;
        transform.position = pos;

        if (slotPanel)
        {
            Vector3 p = slotPanel.position;
            p.y = pos.y + panelYOffset;
            p.z = slotPanel.position.z;      
            slotPanel.position = p;
        }
    }
}