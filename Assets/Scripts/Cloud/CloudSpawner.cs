using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class CloudSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    
    [Header("Cloud Controller")]
    public GameObject MainCamera;
    public GameObject CloudPrefab;
    public int FrequencyY = 10;
    public int ScreenDivision = 22;
    public int numberOfClouds = 7;
    public int Randomness = 3;

    private float cameraThreshold = 0;

    
    void Start()
    {   
        if(numberOfClouds>ScreenDivision){
            numberOfClouds = ScreenDivision-2;
        }

        if(numberOfClouds<0){
            numberOfClouds = 0;
        }
    }

    void Update()
    {
        if (MainCamera.transform.position.y <= cameraThreshold)
        {
            return;
        }
        cameraThreshold += FrequencyY;
        SavePositions();
    }
    void SavePositions(){
        List<Vector2> positions = new List<Vector2>();
        for(int i=0; i<=ScreenDivision; i++){
            Vector2 Left = new Vector2(- MainCamera.GetComponent<Camera>().orthographicSize * MainCamera.GetComponent<Camera>().aspect, MainCamera.transform.position.y+10);
            Vector2 Spawn = Left + new Vector2(MainCamera.GetComponent<Camera>().orthographicSize * MainCamera.GetComponent<Camera>().aspect / ScreenDivision * i * 2, 0);

            positions.Add(Spawn); //구름을 생성할 모든 위치를 저장
        }

        SpawnCloud(positions);
    }
    
    void SpawnCloud(List<Vector2> positions){
        //화면을 N개의 조각으로 나누고 각 조각에 구름을 생성
        //positions에서 랜덤으로 위치를 선택하여 구름을 생성
        int pos = Random.Range(numberOfClouds/2, ScreenDivision - numberOfClouds/2);
        int rand = Random.Range(-Randomness, Randomness);
        int posmin = pos - numberOfClouds/2 - rand/2;
        int posmax = pos + numberOfClouds/2 + rand/2;

        if(posmin<0) posmin = 0;
        if(posmax>ScreenDivision) posmax = ScreenDivision;

        //Spawn에 해당하는 위치에 구름을 생성

        for(int i=0; i<positions.Count; i++){
            if(i>=posmin && i<=posmax){
                Instantiate(CloudPrefab, positions[i], Quaternion.identity);
            }
        }
    }   
}
