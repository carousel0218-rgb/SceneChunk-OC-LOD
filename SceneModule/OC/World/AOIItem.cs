
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using GameSetting;
using MRK.Framework.AssetLoad;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

public class AOIItem
{   
    public enum EGridState
    {
        Invaild,
        Loading,
        Load,
        UnLoading,
        Exit,
    }

    public EGridState eState;

    private string namePath;

    private GameObject curGameObject;

    private Transform parent;

    public string curName;
    
    private OCScene ocScene;

    public int girdId;

    public int chunkX;
    public int chunkZ;
    
    string PackageName = GameSettingsUtils.GameSettings.DownloadSettings.ScenePackageName;

    public AOIItem(OCScene oc,string path,Transform parentObj,int id,int x = 0 ,int z = 0)
    {
        namePath = path;
        parent = parentObj;
        eState = EGridState.Invaild;
        ocScene = oc;
        girdId = id;
        
        chunkX =  x;
        chunkZ =  z;
    }

    public void UpdateItem(string path,float waitLoadTime)
    {
        if(namePath == path) return;
        namePath = path;
        UnLoadingItem();
       
        LoadingItem(waitLoadTime,null);
    }

    public async void LoadingItem(float waitLoadTime,Action callback)
    {   
        if (eState == EGridState.Loading || eState == EGridState.Load )return;
        eState = EGridState.Loading;
       
        await LoadInstanceAsync( namePath,waitLoadTime,callback);
    }
    
    public async UniTask LoadInstanceAsync(string path,float waitLoadTime,Action callback)
    {   
        curGameObject = await AssetMgr.Instance.GetAsset(path, parent,packageName:PackageName);
        eState = EGridState.Load;
        callback?.Invoke();
        curName = curGameObject.name;
        if (ocScene != null)
        {
            ocScene.AddItem(curGameObject);
        }
        
    }

    public void UnLoadingItem()
    {   
        eState = EGridState.Exit;
        if (eState == EGridState.Loading)
        {
            return;
        }
        if(curGameObject == null) return;


        if (ocScene != null)
        {
            ocScene.RemoveItem(curName);
        }

        GameObject.Destroy(curGameObject);
        curGameObject = null;

    }
    
}
