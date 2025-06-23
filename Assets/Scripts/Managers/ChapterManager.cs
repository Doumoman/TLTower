using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum chapter {ground, spring, summer, autumn, winter, space};
public class ChapterManager : MonoBehaviour
{
    public chapter chapter = chapter.ground;
    public int[] stonesForChapter = {  };
    [Range(0, 1)] public float chance = 0.1f;
    public float obstaclesSpan = 30f;
    public int summonCount = 5;
    int stoneCount = 0;
    int summonCounter = 0;
    int idx = 0;
    Dictionary<GameObject, Coroutine> co = new Dictionary<GameObject, Coroutine>();

    public GameObject birdSpawner;
    public GameObject rain;
    public GameObject wind;
    public GameObject snowParticle;

    public static ChapterManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        TickManager.Instance.OnTickEvent += TickEvent;
    }

    void ChangeChapter()
    {
        chapter[] arr = { chapter.ground, chapter.spring, chapter.summer,
            chapter.autumn, chapter.winter, chapter.space };
        if (idx < arr.Count()-1 && stoneCount >= stonesForChapter[idx])
        {
            chapter = arr[++idx];
            Debug.Log(chapter);
        }
    }
    public void AddCount() { stoneCount += 1; CreateObstacle(); ChangeChapter(); }
    public void RemoveCount() { stoneCount -= 1; }

    void TickEvent(object sender, System.EventArgs eventArgs)
    { 
        
    }

    void CreateObstacle()
    {
        List<GameObject> obstacles = new List<GameObject>();
        switch (idx)
        {
            case 0:
                return;
            case 1:
                birdSpawner.SetActive(true);
                return;
            case 2:
            case 3:
                obstacles = new List<GameObject> { rain, wind }; break;
            case 4:
                obstacles = new List<GameObject> { wind, snowParticle }; break;
            case 5:
                return;

        }

        summonCounter += 1;
        if (summonCounter >= summonCount)
        {
            foreach (GameObject go in obstacles)
            {
                if (Random.value <= chance)
                {
                    if (go == snowParticle)
                    {
                        ParticleSystem ps = go.GetComponent<ParticleSystem>();
                        ps.Play();
                        if (co.TryGetValue(go, out Coroutine c)) StopCoroutine(co[go]);
                        co.Add(go, StartCoroutine(StopParticle(go, obstaclesSpan)));
                    }
                    else
                    {
                        go.SetActive(true);
                        if (co.TryGetValue(go, out Coroutine c)) StopCoroutine(co[go]);
                        co.Add(go, StartCoroutine(DisableObject(go, obstaclesSpan)));
                    }
                }
            }
            summonCounter = 0;
        }
    }

    IEnumerator DisableObject(GameObject go, float waittime)
    {
        yield return new WaitForSeconds(waittime);
        go.SetActive(false);
        co.Remove(go);
    }

    IEnumerator StopParticle(GameObject go, float waittime)
    {
        yield return new WaitForSeconds(waittime);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        ps.Stop();
        co.Remove(go);
    }
}
