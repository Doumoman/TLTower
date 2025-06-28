using System.Collections;
using System.Collections.Generic;
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
    }

    void Start()
    {
        Debug.Log("ScriptDataLoader Start");
        try
        {
            Debug.Log("FindScriptData: " + FindScriptData("Debugging", 0));
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

    public string FindScriptData(string situation, int idx)
    {
        string selectScript = "";
        foreach (var value in script.Data.DataList)
        {
            if (selectScript != "") break; //이미 찾은 경우 반복 종료
            if (value.scriptName == situation && value.scriptIdx == idx)
            {
                selectScript = value.scriptString;
            }
        }
        return selectScript;
    }
}
