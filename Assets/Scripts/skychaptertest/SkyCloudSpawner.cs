using System.Collections;
using UnityEngine;

public class SkyCloudSpawner : MonoBehaviour
{
    public float spanMin;
    public float spanMax;
    public Transform[] skyCloud;

    [Header("Pool")]
    [SerializeField, Min(0)] private int initialPoolSize = 20;

    [Header("Base Color Noise")]
    [SerializeField, Range(0f, 0.25f)] private float minBaseColorNoise = 0.08f;
    [SerializeField, Range(0f, 0.25f)] private float maxBaseColorNoise = 0.15f;

    Coroutine co = null;
    private float xPos;
    private int directionalForce = 1;
    private SkyCloudPool cloudPool;
    private bool isDuplicate;

    public static SkyCloudSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            isDuplicate = true;
            enabled = false;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cloudPool = new SkyCloudPool(transform, skyCloud);
        cloudPool.Prewarm(initialPoolSize);
    }

    IEnumerator MakeCloud()
    {
        while (true)
        {
            //무작위 대기시간 설정
            float span = UnityEngine.Random.Range(spanMin, spanMax);
            yield return new WaitForSeconds(span);

            if (cloudPool == null || skyCloud == null || skyCloud.Length == 0)
                yield break;

            //무작위 y위치로 구름 생성
            xPos = transform.position.x;
            int idx = UnityEngine.Random.Range(0, skyCloud.Length);
            Transform prefab = skyCloud[idx];
            if (!prefab) continue;

            CloudController cc = cloudPool.Get(prefab);
            if (!cc) continue;

            Vector3 spawnPosition = new Vector3(
                xPos * directionalForce,
                transform.position.y + UnityEngine.Random.Range(-5, 5),
                0f);

            cc.PrepareForSpawn(
                spawnPosition,
                directionalForce,
                minBaseColorNoise,
                maxBaseColorNoise);
        }
    }

    public void Changedirection()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
        int autumnStageIndex = (int)ChapterManager.Instance.chapter - (int)chapter.autumn;
        directionalForce = autumnStageIndex % 2 == 0 ? 1 : -1;
        co = StartCoroutine(MakeCloud());
    }

    private void OnEnable()
    {
        if (isDuplicate) return;

        xPos = transform.position.x;
        co = StartCoroutine(MakeCloud());
    }

    private void Update()
    {
        cloudPool?.PromoteReleasedClouds();
    }

    private void OnDisable()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
