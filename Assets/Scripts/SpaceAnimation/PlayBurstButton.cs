using UnityEngine;

public class PlayPuzzleBurst : MonoBehaviour
{
    [Tooltip("Inspector에서 드래그할 PuzzleBurst ParticleSystem")]
    public ParticleSystem puzzleBurst;
    void Start()
    {
        Debug.Log("[PuzzleBurst] PlayOnAwake=" + puzzleBurst.main.playOnAwake);
        puzzleBurst.Play();
    }
    // 버튼 OnClick에 이 메서드를 등록합니다.
    public void PlayBurst()
    {
        Debug.Log("PlayBurst 호출됨, puzzleBurst is " + (puzzleBurst == null ? "NULL" : "OK"));
        if (puzzleBurst == null) return;
        puzzleBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        puzzleBurst.Play();
    }

}
