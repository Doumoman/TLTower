using UnityEngine;
using UnityEngine.Playables;

public class title : MonoBehaviour
{
    public float speed = 1f;
    PlayableDirector director;
    Playable rootPlayable;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
        director.timeUpdateMode = DirectorUpdateMode.GameTime;
    }

    void Start()
    {
        director.Play();
        rootPlayable = director.playableGraph.GetRootPlayable(0);
    }

    void Update()
    {
        // 매 프레임 최신 핸들에 속도를 설정
        rootPlayable.SetSpeed(speed);
    }
}
