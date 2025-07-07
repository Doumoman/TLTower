using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class MaterialDissolvePlayableAsset : PlayableAsset
{
    public ExposedReference<Material> targetMat;  // 타임라인 창에서 드래그
    public float from = 0f;
    public float to = 1f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var pb = new MaterialDissolvePlayableBehaviour
        {
            targetMat = targetMat.Resolve(graph.GetResolver()),
            from = from,
            to = to
        };
        return ScriptPlayable<MaterialDissolvePlayableBehaviour>.Create(graph, pb);
    }
}