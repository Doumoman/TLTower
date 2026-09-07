using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SkyCloudSpawner가 소유하는 프리팹별 구름 풀.
/// 런타임 컴포넌트의 지연 파괴가 끝난 다음 프레임부터 반환된 구름을 재사용한다.
/// </summary>
public sealed class SkyCloudPool
{
    private readonly struct PendingRelease
    {
        public readonly CloudController Cloud;
        public readonly int ReleasedFrame;

        public PendingRelease(CloudController cloud, int releasedFrame)
        {
            Cloud = cloud;
            ReleasedFrame = releasedFrame;
        }
    }

    private readonly Transform poolRoot;
    private readonly List<Transform> spawnEntries = new List<Transform>();
    private readonly Dictionary<Transform, Queue<CloudController>> availableByPrefab =
        new Dictionary<Transform, Queue<CloudController>>();
    private readonly Dictionary<CloudController, Transform> prefabByInstance =
        new Dictionary<CloudController, Transform>();
    private readonly HashSet<CloudController> activeClouds = new HashSet<CloudController>();
    private readonly HashSet<CloudController> queuedClouds = new HashSet<CloudController>();
    private readonly List<PendingRelease> pendingReleases = new List<PendingRelease>();

    public SkyCloudPool(Transform owner, Transform[] prefabs)
    {
        GameObject rootObject = new GameObject("SkyCloudPool");
        poolRoot = rootObject.transform;
        poolRoot.SetParent(owner, false);

        if (prefabs == null) return;

        foreach (Transform prefab in prefabs)
        {
            if (!prefab) continue;

            spawnEntries.Add(prefab);
            if (!availableByPrefab.ContainsKey(prefab))
                availableByPrefab.Add(prefab, new Queue<CloudController>());
        }
    }

    public void Prewarm(int totalCount)
    {
        if (totalCount <= 0 || spawnEntries.Count == 0) return;

        for (int i = 0; i < totalCount; i++)
        {
            Transform prefab = spawnEntries[i % spawnEntries.Count];
            CloudController cloud = Create(prefab);
            if (cloud) Enqueue(prefab, cloud);
        }
    }

    public CloudController Get(Transform prefab)
    {
        if (!prefab || !availableByPrefab.TryGetValue(prefab, out Queue<CloudController> queue))
            return null;

        PromoteReleasedClouds();

        CloudController cloud = null;
        while (queue.Count > 0 && !cloud)
        {
            cloud = queue.Dequeue();
            if (cloud) queuedClouds.Remove(cloud);
        }

        if (!cloud) cloud = Create(prefab);
        if (!cloud) return null;

        activeClouds.Add(cloud);
        return cloud;
    }

    public void Release(CloudController cloud)
    {
        if (!cloud || !activeClouds.Remove(cloud)) return;
        if (!prefabByInstance.ContainsKey(cloud)) return;

        cloud.StoreInPool(poolRoot);
        pendingReleases.Add(new PendingRelease(cloud, Time.frameCount));
    }

    public void PromoteReleasedClouds()
    {
        for (int i = pendingReleases.Count - 1; i >= 0; i--)
        {
            PendingRelease pending = pendingReleases[i];
            CloudController cloud = pending.Cloud;

            if (!cloud)
            {
                pendingReleases.RemoveAt(i);
                continue;
            }

            if (Time.frameCount <= pending.ReleasedFrame || !cloud.IsPoolCleanupComplete)
                continue;

            pendingReleases.RemoveAt(i);
            if (prefabByInstance.TryGetValue(cloud, out Transform prefab))
                Enqueue(prefab, cloud);
        }
    }

    private CloudController Create(Transform prefab)
    {
        Transform instance = Object.Instantiate(prefab, poolRoot);
        if (!instance.TryGetComponent(out CloudController cloud))
        {
            Debug.LogError($"Cloud prefab '{prefab.name}' does not have a CloudController.");
            Object.Destroy(instance.gameObject);
            return null;
        }

        prefabByInstance.Add(cloud, prefab);
        cloud.InitializePool(this);
        cloud.StoreInPool(poolRoot);
        return cloud;
    }

    private void Enqueue(Transform prefab, CloudController cloud)
    {
        if (!cloud || !queuedClouds.Add(cloud)) return;
        availableByPrefab[prefab].Enqueue(cloud);
    }
}
