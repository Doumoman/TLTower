using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Winter's final checkpoint only; shared seasonal clouds stay untouched.</summary>
public static class WinterSpaceCloudTransition
{
    public const float RushSeconds = 1.4f;
    public const float ClearSeconds = 3f;
    const float DenseExitSeconds = 0.9f;
    const int DenseCloudCount = 18;
    const int TrailingCloudCount = 3;

    sealed class Cloud
    {
        public RectTransform rect;
        public Vector2 coveredPosition;
        public float exitTravel;
        public bool trailing;
    }

    public static IEnumerator Play(IReadOnlyList<RectTransform> artwork, Transform owner,
        Color winterCloudTint, float tintStrength, Action onCovered, Action<float> onProgress)
    {
        var sprites = new List<Sprite>();
        var colors = new List<Color>();
        tintStrength = Mathf.Clamp01(tintStrength);
        if (artwork != null)
        {
            foreach (RectTransform source in artwork)
            {
                if (!source) continue;
                if (source.TryGetComponent(out SpriteRenderer renderer) && renderer.sprite)
                {
                    sprites.Add(renderer.sprite);
                    colors.Add(ApplyTint(renderer.color, winterCloudTint, tintStrength));
                }
                else if (source.TryGetComponent(out Image image) && image.sprite)
                {
                    sprites.Add(image.sprite);
                    colors.Add(ApplyTint(image.color, winterCloudTint, tintStrength));
                }
            }
        }
        if (sprites.Count == 0)
            Debug.LogWarning("[WinterSpaceClouds] Cloud artwork missing; using the cover only.");

        GameObject overlay = new("WinterToSpaceClouds", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        // Scene-owned even though AnimationManager survives scene changes.
        overlay.transform.SetParent(owner, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2160f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        try
        {
            Rect view = Camera.main ? Camera.main.rect : new Rect(0f, 0f, 1f, 1f);
            // Overlay canvases ignore Camera.rect. Constrain all transition graphics
            // to its viewport so they never paint (or leave stale color in) the bars.
            MakeBlackBar("BottomLetterbox", overlay.transform, Vector2.zero, new Vector2(1f, view.y));
            MakeBlackBar("TopLetterbox", overlay.transform, new Vector2(0f, view.yMax), Vector2.one);
            MakeBlackBar("LeftLetterbox", overlay.transform, Vector2.zero, new Vector2(view.x, 1f));
            MakeBlackBar("RightLetterbox", overlay.transform, new Vector2(view.xMax, 0f), Vector2.one);
            GameObject viewport = new("CloudViewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(overlay.transform, false);
            RectTransform viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = new Vector2(view.x, view.y);
            viewportRect.anchorMax = new Vector2(view.xMax, view.yMax);
            viewportRect.sizeDelta = Vector2.zero;

            float height = 1080f * view.height;
            float width = 1080f * Screen.width / Mathf.Max(1, Screen.height) * view.width;
            float cloudWidth = Mathf.Max(width * 0.95f, height * 1.4f);
            Vector2 direction = new(-Mathf.Cos(30f * Mathf.Deg2Rad), -0.5f);
            float travel = (width + cloudWidth) * 1.5f;

            Color coverColor = colors.Count > 0
                ? colors[0]
                : ApplyTint(new Color(0.65f, 0.72f, 0.74f), winterCloudTint, tintStrength);
            coverColor.a = 0f;
            Image cover = MakeImage("CloudCover", viewport.transform, null, coverColor);
            cover.rectTransform.anchorMin = Vector2.zero;
            cover.rectTransform.anchorMax = Vector2.one;
            cover.rectTransform.sizeDelta = Vector2.zero;
            cover.raycastTarget = true;
            // The source clouds themselves are translucent. An opaque backing at
            // peak density prevents the background/hand swap showing through them.

            var clouds = new List<Cloud>(DenseCloudCount + TrailingCloudCount);
            if (sprites.Count > 0)
            {
                for (int i = 0; i < DenseCloudCount + TrailingCloudCount; i++)
                {
                    bool trailing = i >= DenseCloudCount;
                    int art = i % sprites.Count;
                    Color color = colors[art];
                    color.a = 1f;
                    Image image = MakeImage($"Cloud_{i}", viewport.transform, sprites[art], color);
                    RectTransform rect = image.rectTransform;
                    float size = cloudWidth * (trailing ? 0.72f : 1.1f);
                    rect.sizeDelta = new Vector2(size, size * sprites[art].rect.height / sprites[art].rect.width);
                    rect.localRotation = Quaternion.Euler(0f, 0f, 30f);
                    int tail = i - DenseCloudCount;
                    Vector2 coveredPosition = trailing
                        ? new Vector2((-0.25f + tail * 0.33f) * width,
                            (0.35f + tail * 0.15f) * height)
                        : new Vector2((i % 6 - 2.5f) * width * 0.34f,
                            (i / 6 - 1f) * height * 0.38f);
                    float halfWidth = (rect.sizeDelta.x * -direction.x + rect.sizeDelta.y * 0.5f) * 0.5f;
                    float exitTravel = trailing
                        ? (coveredPosition.x + width * 0.56f + halfWidth) / -direction.x
                        : travel;
                    clouds.Add(new Cloud { rect = rect, coveredPosition = coveredPosition,
                        exitTravel = exitTravel, trailing = trailing });
                    rect.anchoredPosition = coveredPosition - direction * travel;
                }
            }

            for (float elapsed = 0f; elapsed < RushSeconds; elapsed += Time.deltaTime)
            {
                float progress = elapsed / RushSeconds;
                foreach (Cloud cloud in clouds)
                    cloud.rect.anchoredPosition = cloud.coveredPosition - direction * travel * (1f - progress);
                coverColor.a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - 0.75f) / 0.25f));
                cover.color = coverColor;
                onProgress?.Invoke(elapsed / (RushSeconds + ClearSeconds));
                yield return null;
            }

            foreach (Cloud cloud in clouds) cloud.rect.anchoredPosition = cloud.coveredPosition;
            coverColor.a = 1f;
            cover.color = coverColor;
            onProgress?.Invoke(RushSeconds / (RushSeconds + ClearSeconds));
            yield return null; // Render full cover before changing anything behind it.
            onCovered?.Invoke();

            for (float elapsed = 0f; elapsed < ClearSeconds; elapsed += Time.deltaTime)
            {
                foreach (Cloud cloud in clouds)
                {
                    float progress = Mathf.Clamp01(elapsed / (cloud.trailing ? ClearSeconds : DenseExitSeconds));
                    // The bulk rushes away; just three trailing clouds remain,
                    // travelling slowly and decelerating as space is revealed.
                    float distance = progress * (1.35f - 0.35f * progress);
                    cloud.rect.anchoredPosition = cloud.coveredPosition + direction * cloud.exitTravel * distance;
                    if (progress >= 1f) cloud.rect.gameObject.SetActive(false);
                }
                coverColor.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.55f));
                cover.color = coverColor;
                onProgress?.Invoke((RushSeconds + elapsed) / (RushSeconds + ClearSeconds));
                yield return null;
            }
            onProgress?.Invoke(1f);
        }
        finally
        {
            // Disabling immediately matters: Destroy alone is deferred until frame end.
            if (overlay)
            {
                overlay.SetActive(false);
                UnityEngine.Object.Destroy(overlay);
            }
        }
    }

    static void MakeBlackBar(string name, Transform parent, Vector2 min, Vector2 max)
    {
        Image bar = MakeImage(name, parent, null, Color.black);
        bar.rectTransform.anchorMin = min;
        bar.rectTransform.anchorMax = max;
        bar.rectTransform.sizeDelta = Vector2.zero;
    }

    static Color ApplyTint(Color original, Color tint, float strength)
    {
        Color result = Color.Lerp(original, tint, strength);
        result.a = original.a;
        return result;
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        return image;
    }
}
