
using System.Collections.Generic;
using System.Resources;
using BlueNoah.Math.FixedPoint;
using Cysharp.Threading.Tasks;
using game_logic;
using GameSetting;
using HotFix;
using MRK;
using MRK.Framework;
using MRK.Framework.AssetLoad;
using MRK.Game;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class WorldLoad : MonoBehaviour
{
    public class WorldJsonData
    {
        public WORLD_BOUND_TYPE boundType;
        public List<string> nameList;
        public float width;
        public List<string> path;
        public Dictionary<string, List<string>> activityNameList;
    }

    public int loadingRange = 1;

    private List<WorldJsonData> worldJsonDatas;
    private List<AOIManager> aoiManagers;

    private OCScene ocScene;
    private bool hasSetOC = false;

    private int readyCheckCount = 0;
    private bool isReadyCheckOk = false;
    private string typeKey = null;
    string PackageName;
    private void Awake()
    {
       
        aoiManagers = new List<AOIManager>();

        var obj = GameObject.Find("OCScene");
        if (obj == null)
        {
            obj = new GameObject("OCScene");
            ocScene = obj.AddComponent<OCScene>();

        }
        else
        {
            ocScene = obj.GetComponent<OCScene>();
        }

        PackageName = GameSettingsUtils.GameSettings.DownloadSettings.ScenePackageName;
     
        hasSetOC = false;
        isReadyCheckOk = false;

      
        LoadJson();
     
    }



    private async void LoadJson()
    {
        string path = "Scenes/318/jsonData_318.json";

        worldJsonDatas = new List<WorldJsonData>();
        var textAsset = await AssetMgr.Instance.GetAsset<TextAsset>(path,packageName:PackageName);

        if (textAsset == null)
        {
            LogHelper.LogError("加载不到对应的json文件:" + path);
            return;

        }
        else
        {
            worldJsonDatas = JsonConvert.DeserializeObject<List<WorldJsonData>>(textAsset.text);
        }
    }

    private void Update()
    {
        if (PlayerRoot.Instance.FocusedPlayer != null)
        {
            if (PlayerRoot.Instance.FocusedPlayer.Data.Position == Vector3.zero)
            {
                return;
            }
            if (worldJsonDatas != null && worldJsonDatas.Count > 0)
            {

                if (hasSetOC == false)
                {
                    ocScene.SetTree(new Vector3(0, 0, 0), new Vector3(6000, 400, 6000));
                    hasSetOC = true;
                }

                if (aoiManagers.Count <= 0)
                {
                    int range = loadingRange;
                    for (int i = 0; i < worldJsonDatas.Count; i++)
                    {
                        if (worldJsonDatas[i] == null) continue;
                        var showList = worldJsonDatas[i].nameList;
                        if (worldJsonDatas[i].boundType == WORLD_BOUND_TYPE.Big_Obj)
                        {
                            range = 1;
                        }
                        else if (worldJsonDatas[i].boundType == WORLD_BOUND_TYPE.Active_obj)
                        {
                            showList = GetAcivityList(worldJsonDatas[i]);
                        }
                        if (showList == null || showList.Count <= 0) continue;
                        var aoi = new AOIManager(ocScene, worldJsonDatas[i].boundType, showList
                           , worldJsonDatas[i].width, range, worldJsonDatas[i].path, CheckReadyLoad);
                        aoiManagers.Add(aoi);
                    }
                }
                for (int i = 0; i < aoiManagers.Count; i++)
                {
                    if (aoiManagers[i] == null) continue;
                    aoiManagers[i].UpdateGrid();
                }


            }
        }
    }


    private async void CheckReadyLoad()
    {
        readyCheckCount += 1;
        if (readyCheckCount >= aoiManagers.Count)
        {
            if (isReadyCheckOk) return;
            isReadyCheckOk = true;

            await UniTask.Delay(500);
            GameEventsUtil.FireNow(EGameEvents.OnWorldLoadOver);
            // SceneEventDefine.SceneLoadOver.SendEventMessage();
        }
    }

    private List<string> GetAcivityList(WorldJsonData datas)
    {
        List<string> list = new List<string>();

        if (datas.activityNameList == null || datas.activityNameList.Count <= 0)
        {
            return list;
        }

        if (datas.activityNameList.ContainsKey(typeKey))
        {
            for (int i = 0; i < datas.activityNameList[typeKey].Count; i++)
            {
                list.Add(datas.activityNameList[typeKey][i]);
            }
        }

        return list;
    }


    #region LoadBase

    private Dictionary<int, string> sceneName = new Dictionary<int, string>()
    {
        
    };

    private GameObject sceneBaseNode;
    private async void LoadSceneBase()
    {
        // if (DataManager.PlayerData.Goal == null) return;
        string path;
        // sceneName.TryGetValue(DataManager.PlayerData.Goal.RoadId, out path);
        // if (!string.IsNullOrEmpty(path))
        // {
        //     var obj = await AssetMgr.Instance.GetAsset(ResourceData.Instance.ResourceWorldBasePrefabPath + path, null);
        //     sceneBaseNode = obj;
        // }
    }

    private void DestroyBase()
    {
        // InstancedAnimation.ClearBuffers();
        GameObject.Destroy(sceneBaseNode);
        sceneBaseNode = null;
    }

    #endregion

    private void OnDestroy()
    {
        DestroyBase();
        if (aoiManagers != null)
        {
            for (int i = 0; i < aoiManagers.Count; i++)
            {
                if (aoiManagers[i] == null) continue;
                aoiManagers[i].DestoryAllItem();
            }
            aoiManagers.Clear();
            aoiManagers = new List<AOIManager>();
        }

    }


}
