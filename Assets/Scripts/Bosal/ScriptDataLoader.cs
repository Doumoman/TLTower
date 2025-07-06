using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UGS;
using Unity.VisualScripting;
using UnityEngine;

public class ScriptDataLoader : MonoBehaviour
{
    public static ScriptDataLoader Instance { get; private set; }
    void Awake()
    {
        Instance = this;
        LoadScript();
        InitializeScriptMap();
    }

    void Start()
    {
        Debug.Log("ScriptDataLoader Start");
        try
        {
            Debug.Log("FindScriptData: " + FindData("Debugging", 0));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error finding script data: " + ex.Message);
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
    public string GetNext(string situation)
    {
        if (!currentIndex.ContainsKey(situation)) currentIndex[situation] = 0;

        int idx = currentIndex[situation];

        if (scriptMap.TryGetValue((situation, idx), out string script))
        {
            currentIndex[situation] = idx + 1;
            return script;
        }
        else
        {
            if (!scriptMap.ContainsKey((situation, 0)))
            {
                Debug.Log($"{situation} 대사 없음!");
                return "";
            }
            currentIndex[situation] = 1;
            return scriptMap[(situation, 0)];
        }
    }

    public void ResetScriptMap()
    {
        foreach (var value in scriptMap)
        {
            var keys = currentIndex.Keys.ToList();
            foreach (var key in keys) currentIndex[key] = 0;
        }
    }
}
