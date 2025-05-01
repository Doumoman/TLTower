using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CamHeightTrigger : MonoBehaviour
{
    public float snapOffset = 0f;   // 옵션

    void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(50f, 1f);

        if (!TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Stone")) return;   // 태그로만 판단

        float newHeight = transform.position.y + snapOffset;
        Debug.Log($"[CamTrigger] hit by {other.name}, cam→{newHeight:F2}");
    }
}