using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UGS;
using Unity.VisualScripting;
using UnityEngine;

public class ScriptDataLoader : Singleton<ScriptDataLoader>
{
    protected override void Awake()
    {
        base.Awake();
        LoadScript();
        InitializeScriptMap();

        Debug.Log("ScriptDataLoader Start");
        try
        {
            Debug.Log("FindScriptData: " + FindData("Debugging", 0));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in ScriptDataLoader Start: " + ex.Message);
        }
    }
    void LoadScript()
    {
        UnityGoogleSheet.LoadAllData();
    }

    private Dictionary<(string, int), string> scriptMap = new Dictionary<(string, int), string>();

    public void InitializeScriptMap()
    {
        foreach (var value in script.Data.DataList)
        {
            var key = (value.scriptName, value.scriptIdx);
            if (!scriptMap.ContainsKey(key)) scriptMap[key] = value.scriptString;
        }
    }

    public string FindData(string situation, int idx)
    {
        return scriptMap.TryGetValue((situation, idx), out string script) ? script : "";
    }

    public Dictionary<string, int> currentIndex = new Dictionary<string, int>(); // situation별 마지막 호출한 대사
    public int GetNext(string situation)
    {
        if (!scriptMap.ContainsKey((situation, 0))) //대사 situation이 존재하지 않으면
        {
            Debug.Log($"{situation} 대사 없음!!");
            return -1; //텍스트는 내보내 줌
        }

        if (!currentIndex.TryGetValue(situation, out int idx)) //최초 호출 시 situation이 없으므로
        {
            currentIndex.Add(situation, idx);
            idx = 0; //0 반환 -> 재생, 1 저장
        }
        else if (!scriptMap.ContainsKey((situation, idx))) //situation은 있는데 마지막 대사라면
        {
            currentIndex[situation] = 0; //0번 대사로 돌아가기
            idx = 0; //0 반환 -> 재생, 1 저장
        }
        else idx = currentIndex[situation];

        currentIndex[situation] = currentIndex[situation] + 1; //다음 호출할 대사 저장
        return idx;
    }
    public void ResetScriptMap() //게임 시작할 때 사용!
    {
        foreach (var value in scriptMap)
        {
            var keys = currentIndex.Keys.ToList();
            foreach (var key in keys) currentIndex[key] = 0;
        }
    }

    private const string PrefsKeyPrefix = "currentIndex_";
    private const string PrefsKeyList = "currentIndex_keys";

    public void SavePrefs()
    {
        // 1) 각 상황별 인덱스 저장
        foreach (var kvp in currentIndex)
        {
            PlayerPrefs.SetInt(PrefsKeyPrefix + kvp.Key, kvp.Value);
        }

        // 2) 딕셔너리에 들어있는 모든 키 리스트 저장 (구분자는 '|' 사용)
        var keyList = string.Join("|", currentIndex.Keys);
        PlayerPrefs.SetString(PrefsKeyList, keyList);

        // 3) 디스크에 즉시 기록
        PlayerPrefs.Save();
    }

    public void LoadPrefs()
    {
        currentIndex.Clear();

        // 저장된 키 목록 문자열을 꺼낸다
        string keyList = PlayerPrefs.GetString(PrefsKeyList, "");
        if (string.IsNullOrEmpty(keyList))
            return; // 복원할 게 없으면 종료

        // '|' 로 분리해서 각 키별로 값을 읽어와 딕셔너리에 세팅
        foreach (var key in keyList.Split('|'))
        {
            int value = PlayerPrefs.GetInt(PrefsKeyPrefix + key, 0);
            currentIndex[key] = value;
        }
    }

    public void ClearPrefs()
    {
        string keyList = PlayerPrefs.GetString(PrefsKeyList, "");
        if (!string.IsNullOrEmpty(keyList))
        {
            foreach (var key in keyList.Split('|'))
            {
                PlayerPrefs.DeleteKey(PrefsKeyPrefix + key);
            }
            PlayerPrefs.DeleteKey(PrefsKeyList);
        }
        PlayerPrefs.Save();
    }
}
