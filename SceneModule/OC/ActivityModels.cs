using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ActivityModels : MonoBehaviour
{   

    private bool isShow;

    private Transform obj;


    private void Awake()
    {   
        isShow = false;
        // TimerManager.AddTimer(3000,()=>{
        //     EventManager.Instance.RegistEvent(Events.SendOnExit, OnExit);
        // });
    }


    private void Update(){
        // if(GameSystem.IsWorking && isShow == false){
        //     if(terrainDic.ContainsKey(GameSettings.Instance.TerrainType)){
        //         obj = transform.Find(terrainDic[GameSettings.Instance.TerrainType]);
        //         if(obj != null){
        //             obj.gameObject.SetActive(true);
        //         }
        //     }
        //     isShow = true;
        // }
    }

    // private void OnExit(EventParam param){
    //     isShow = false;
    // }



    private void OnDestroy()
    {
       

    }
}
