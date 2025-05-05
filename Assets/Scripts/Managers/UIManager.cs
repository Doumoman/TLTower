using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum GameState
{
    waiting, game, pause
}
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField]
    [Header("References")]
    public GameObject circle;
    public GameObject saving;
    public GameObject saved;
    public GameObject logo;
    public GameObject menu;
    public GameObject check;
    public GameObject saveLoading;
    public GameObject saveEnd;
    public Button pause;
    public Button ctu;
    public Button exit;
    public Button exitN;
    public Button exitY;
    public TMP_Text tapToStart;

    GameState state;
    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* 버튼들 클릭시 실행할 함수 할당 */
    void Start()
    {
        state = GameState.waiting;
        pause.onClick.AddListener(Pause);
        exit.onClick.AddListener(Exit);
        ctu.onClick.AddListener(Continue);
        exitN.onClick.AddListener(Pause);
    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;     //버튼 클릭시 game전환 방지(계속하기 버튼 클릭시 waiting에서 game으로 바로 넘어가는 버그 존재)
        if (state == GameState.waiting && Input.GetKeyUp(KeyCode.Mouse0))
        {
            Debug.Log("clickEnd");
            state = GameState.game;
            logo.SetActive(false);
            tapToStart.enabled = false;
            circle.SetActive(true);
        }

    }
    public void Pause()
    {
        state = GameState.pause;
        menu.SetActive(true);
        check.SetActive(false);
    }
    public void Continue()
    {
        if(logo.activeSelf)
        {
            state = GameState.waiting;
        }
        else
        {
            state = GameState.game;
        }
        menu.SetActive(false);
    }
    public void Exit()
    {
        menu.SetActive(false);
        check.SetActive(true);
    }
    public void Save()
    {
        saveEnd.SetActive(false);
        saveLoading.SetActive(true);
    }
    public void OnSaveEnd()
    {
        StartCoroutine(SaveEnd());
    }
    /*saved UI가 나타나고, 일정 시간 후 사라지도록 하는 함수*/
    public IEnumerator SaveEnd()
    {
        saveLoading.SetActive(false);
        saveEnd.SetActive(true);

        yield return new WaitForSeconds(2f);

        saveEnd.SetActive(false);
    }
}
