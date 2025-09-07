using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
/*
    게임 시작 시에 Application.persistentDataPath에 첫 실행을 판별할 파일을 만듦
    이후에 이 파일이 존재하는지 확인 후 존재한다면 무시, 없다면 최초 실행이므로 PlayerPrefs 초기화
*/
public class StartManager : MonoBehaviour
{
    private static string firstPath => Path.Combine(Application.persistentDataPath, "first.txt");
    void Start()
    {
        if (!File.Exists(firstPath))
        {
            PlayerPrefs.DeleteAll();
            //여기에 PlayerPrefs 초기화 코드 작성
            PlayerPrefs.SetFloat("SFXVolume", .75f);
            PlayerPrefs.SetFloat("BGMVolume", .75f);
            PlayerPrefs.SetFloat("VoiceVolume", .75f);

            PlayerPrefs.Save();

            File.WriteAllText(firstPath, System.Guid.NewGuid().ToString());
        }
    }
}
