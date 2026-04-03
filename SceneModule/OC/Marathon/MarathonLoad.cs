//
// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// public class MarathonLoad : MonoBehaviour
// {
//     public class MarathonJsonData
//     {
//         public string prefabName;  // 保存预制体路径
//         public OCItemCheckType checkType;
//         public float[] position;   // 将 position 转换为 float[] 数组 [x, y, z]
//
//         public float[] rotation;   // 将 rotation 转换为 float[] 数组 [x, y, z]
//
//         public float[] scale;      // 将 scale 转换为 float[] 数组 [x, y, z]
//         
//         public Vector3 Position;
//         public Vector3 Rotation;
//         public Vector3 Scale;
//         public GameObject obj;
//         public int index;
//     }
//     
//     public class WorldJsonData
//     {
//         public WORLD_BOUND_TYPE boundType;
//         public List<string> nameList;
//         public float width;
//         public List<string> path;
//         public Dictionary<string, List<string>> activityNameList;
//     }
//
//     public int loadingRange = 1;
//
//     private List<WorldJsonData> worldJsonDatas;
//     private List<AOIManager> aoiManagers;
//     private Dictionary<string,List<MarathonJsonData>> marathonJsonDatas;
//     private Dictionary<string, List<GameObject>> hidePrefabs;
//     private Dictionary<string, List<MarathonJsonData>> showMarathonDatas;
//     private Dictionary<string, GameObject> basePrefabs;
//     
//     public float SHOWDISTANCE = 150;
//     public Dictionary<OCItemCheckType,int> TYPE_DIS = new Dictionary<OCItemCheckType, int>{
//         {OCItemCheckType.BIG,1000},{OCItemCheckType.MIDDLE,275},{OCItemCheckType.SMALL,60},
//         {OCItemCheckType.ACTIVITY,200}
//     };
//     private int readyCheckCount = 0;
//     private bool isReadyCheckOk = false;
//     private void Awake()
//     {
//         LoadJson();
//         LoadJson2();
//         aoiManagers = new List<AOIManager>();
//         hidePrefabs = new Dictionary<string, List<GameObject>>();
//         basePrefabs = new Dictionary<string, GameObject>();
//         showMarathonDatas = new Dictionary<string, List<MarathonJsonData>>();
//     }
//
//     private void LoadJson()
//     {
//         string path = "Assets/Res/Scenes/MarathonInfo/mesh_data.json";
//
//         marathonJsonDatas = new Dictionary<string, List<MarathonJsonData>>();
//         GameRoot.Root.StartCoroutine(ResourceManager.Instance.LoadAsync<TextAsset>(path, (obj) =>
//         {
//             if (obj == null)
//             {
//                 Console.LogError("加载不到对应的json文件:" + path);
//                 return;
//
//             }
//             else
//             {
//                 marathonJsonDatas = JsonConvert.DeserializeObject<Dictionary<string,List<MarathonJsonData>>>(obj.text);
//                 
//                 foreach (var marathonJsonData in marathonJsonDatas)
//                 {
//                     var list = marathonJsonData.Value;
//                     for (int i = 0; i < list.Count; i++)
//                     {
//                         list[i].Position = ConvertToV3(list[i].position);
//                         list[i].Rotation = ConvertToV3(list[i].rotation);
//                         list[i].Scale = ConvertToV3(list[i].scale);
//                         list[i].index = i;
//                     }
//             
//                 }
//             }
//
//         }));
//     }
//     
//     private void LoadJson2()
//     {
//         string path = "Assets/Res/Scenes/MarathonInfo/jsonData.json";
//
//         worldJsonDatas = new List<WorldJsonData>();
//         GameRoot.Root.StartCoroutine(ResourceManager.Instance.LoadAsync<TextAsset>(path, (obj) =>
//         {
//             if (obj == null)
//             {
//                 Console.LogError("加载不到对应的json文件:" + path);
//                 return;
//
//             }
//             else
//             {
//                 worldJsonDatas = JsonConvert.DeserializeObject<List<WorldJsonData>>(obj.text);
//             }
//
//         }));
//     }
//     
//     // 将位置、旋转、缩放从本地坐标系转换为世界坐标系
//     private Vector3 ConvertToV3(float[] localCoordinates)
//     {
//         if (localCoordinates.Length == 3)
//         {
//             Vector3 localPosition = new Vector3(localCoordinates[0], localCoordinates[1], localCoordinates[2]);
//             return localPosition;
//         }
//         return Vector3.zero;  // 如果不是3个元素，返回零向量
//     }
//
//     private int updateCount = 0;
//     private void Update()
//     {
//
//         if (GameSystem.IsWorking == false) return;
//
//         updateCount += 1;
//         if (updateCount < 3)
//         {
//             return;
//         }
//
//         updateCount = 0;
//         
//         if (PlayerCamera.Instance.FocusedPlayer != null)
//         {
//             if (PlayerCamera.Instance.FocusedPlayer.Position == Vector3.zero)
//             {
//                 return;
//             }
//             if (marathonJsonDatas != null && marathonJsonDatas.Count > 0)
//             {
//                 UpdatePrefabsInView();
//             }
//             
//             if (worldJsonDatas != null && worldJsonDatas.Count > 0)
//             {
//                 if (aoiManagers.Count <= 0)
//                 {
//                     int range = loadingRange;
//                     for (int i = 0; i < worldJsonDatas.Count; i++)
//                     {
//                         if (worldJsonDatas[i] == null) continue;
//                         var showList = worldJsonDatas[i].nameList;
//                       
//                         if (showList == null || showList.Count <= 0) continue;
//
//                         var aoi = new AOIManager(null, worldJsonDatas[i].boundType, showList
//                             , worldJsonDatas[i].width, range,worldJsonDatas[i].path, CheckReadyLoad);
//                         aoiManagers.Add(aoi);
//                     }
//                 }
//                 for (int i = 0; i < aoiManagers.Count; i++)
//                 {
//                     if (aoiManagers[i] == null) continue;
//                     aoiManagers[i].UpdateGrid();
//                 }
//
//
//             }
//         }
//     }
//     
//     private void CheckReadyLoad()
//     {
//         readyCheckCount += 1;
//         if (readyCheckCount <= aoiManagers.Count)
//         {
//             if (isReadyCheckOk) return;
//             isReadyCheckOk = true;
//             EventManager.Instance.Send(Events.FullSceneLoaded);
//             // LoadSceneBase();
//         }
//     }
//  
//
//     // 更新视野内的预制体位置，重置或销毁超出视野的预制体
//     private void UpdatePrefabsInView()
//     {
//         var pos = PlayerCamera.Instance.ViewCamera.transform.position;
//         foreach (var items in showMarathonDatas)
//         {
//             var key = items.Key;
//             var list = items.Value;
//         
//             // 为了避免在循环中频繁添加或删除对象，先保存所有需要处理的对象
//             List<GameObject> objectsToHide = new List<GameObject>();
//             List<MarathonJsonData> datas = new List<MarathonJsonData>();
//            
//             for (int i = 0; i < list.Count; i++)
//             {
//                 var prefab = list[i].obj;
//                 if (prefab == null)
//                     continue;
//                 float curOffest = Vector3.Distance(pos, list[i].Position);
//                 float dis = TYPE_DIS[list[i].checkType];
//                 if ( curOffest > -dis && curOffest < dis)
//                 {
//                     continue; // 如果在距离范围内，跳过此物体
//                 }
//                 // 如果不在范围内，准备隐藏物体
//                 prefab.SetActive(false);
//                 objectsToHide.Add(prefab);
//                 datas.Add(list[i]);
//             }
//             
//             for (int i = datas.Count - 1; i >= 0; i--)
//             {
//                 list.Remove(datas[i]);
//                 datas[i].obj = null;
//             }
//             // 如果需要隐藏的物体不为空，进行处理
//             if (objectsToHide.Count > 0)
//             {
//                 // 确保 hidePrefabs[items.Key] 已经存在
//                 if (!hidePrefabs.ContainsKey(key))
//                 {
//                     hidePrefabs[key] = new List<GameObject>();
//                 }
//         
//                 for (int i = 0; i < objectsToHide.Count; i++)
//                 {
//                     hidePrefabs[key].Add(objectsToHide[i]);
//                 }
//             }
//
//         }
//
//
//         UpdatePosToPrefabs();
//     }
//     
//     private void UpdatePosToPrefabs()
//     {
//         var pos = PlayerCamera.Instance.ViewCamera.transform.position;
//         foreach (var marathonJsonData in marathonJsonDatas)
//         {
//             var list = marathonJsonData.Value;
//             List<MarathonJsonData> datas = new List<MarathonJsonData>();
//             for (int i = 0; i < list.Count; i++)
//             {
//                 if (list[i].obj != null)
//                 {
//                     continue;
//                 }
//                 float curOffest = Vector3.Distance(pos, list[i].Position);
//                
//                 float dis = TYPE_DIS[list[i].checkType];
//                 if ( curOffest > -dis &&  curOffest < dis)
//                 {
//                     datas.Add(list[i]);
//                 }
//             }
//
//             if (datas.Count > 0)
//             {
//                 InstantiatePrefab(marathonJsonData.Key, datas);
//             }
//            
//         }
//     }
//
//     private void InstantiatePrefab(string key, List<MarathonJsonData> datas)
//     {
//         if (!showMarathonDatas.ContainsKey(key)) 
//         {
//             LoadPrefab(key, datas);
//             return;
//         }
//
//         var item = showMarathonDatas[key];
//         if (item.Count <= 0)
//         {
//             LoadPrefab(key, datas);
//             return;
//         }
//
//         var prefab = item[0].obj;
//         if (prefab == null)
//         {
//             for (int i = 0; i < item.Count; i++)
//             {
//                 if (item[i].obj != null)
//                 {
//                     prefab = item[i].obj;
//                     break;
//                 }
//             }   
//         }
//         List<GameObject> currentHidePrefabs = hidePrefabs.ContainsKey(key) ? hidePrefabs[key] : new List<GameObject>();
//         
//         int hideCount = currentHidePrefabs.Count;
//         int dataCount = datas.Count;
//         if (hideCount == 0)
//         {
//             // 如果没有需要隐藏的 Prefab，直接实例化剩下的
//             for (int i = 1; i < dataCount; i++)
//             {
//                 GameObject curObj = Instantiate(prefab);
//                 SetObjMarathonData(curObj, datas[i]);
//             }
//         }
//         else
//         {
//             int remainingCount = dataCount - hideCount;
//         
//             if (remainingCount > 0)
//             {
//                 // 首先更新并移除已隐藏的 Prefab
//                 for (int i = 0; i < hideCount; i++)
//                 {
//                     if (currentHidePrefabs[i] != null)
//                     {
//                         SetObjMarathonData(currentHidePrefabs[i], datas[i]);
//                     }
//                 }
//         
//                 // 创建新的实例化对象，补充剩余的数量
//                 for (int i = hideCount; i < dataCount; i++)
//                 {
//                     GameObject curObj = Instantiate(prefab);
//                     SetObjMarathonData(curObj, datas[i]);
//                 }
//             }
//             else
//             {
//                 // 数据量小于或等于隐藏物体数量时，重新设置所有隐藏的物体
//                 for (int i = 0; i < dataCount; i++)
//                 {
//                     if (currentHidePrefabs[i] != null)
//                     {
//                         SetObjMarathonData(currentHidePrefabs[i], datas[i]);
//                     }
//                 }
//             }
//
//             if (hidePrefabs.ContainsKey(key))
//             {
//                 // 移除已使用的隐藏 Prefab
//                 hidePrefabs[key].RemoveRange(0, Mathf.Min(hideCount, dataCount));
//             }
//         }
//     }
//
//     private Dictionary<string, bool> pathIsLoading;
//     private void LoadPrefab(string path, List<MarathonJsonData> datas)
//     {
//         if (pathIsLoading == null)
//         {
//             pathIsLoading = new Dictionary<string, bool>();
//         }
//
//         if (pathIsLoading.ContainsKey(path) )
//         {
//             if (pathIsLoading[path])
//             {
//                 return;
//             }
//         }
//         else
//         {
//             pathIsLoading.Add(path,true);
//         }
//         GameRoot.Root.StartCoroutine(
//             ResourceManager.Instance.LoadInstanceAsync( path,
//                 (obj)=>
//                 {
//                     pathIsLoading[path] = false;
//                     if (!showMarathonDatas.ContainsKey(datas[0].prefabName))
//                     {
//                         showMarathonDatas[datas[0].prefabName] = new List<MarathonJsonData>();
//                     }
//
//                     if (datas.Count > 1)
//                     {
//                         for (int i = 1; i < datas.Count; i++)
//                         {
//                             GameObject curObj = Instantiate(obj);
//                             SetObjMarathonData(curObj,datas[i]);
//                         }
//                     }
//                     SetObjMarathonData(obj,datas[0]);
//                 }));
//     }
//
//     private void SetObjMarathonData(GameObject obj, MarathonJsonData data)
//     {
//         obj.SetActive(true);
//         obj.transform.name = data.index.ToString();
//         obj.transform.position = data.Position;
//         obj.transform.eulerAngles = data.Rotation;
//         obj.transform.localScale = data.Scale;
//         if(basePrefabs.ContainsKey(data.prefabName))
//         {
//             obj.transform.SetParent(basePrefabs[data.prefabName].transform);
//         }
//         else
//         {
//             GameObject baseObj = new GameObject(data.prefabName);
//             basePrefabs.Add(data.prefabName,baseObj);
//             obj.transform.SetParent(baseObj.transform);
//         }
//
//         data.obj = obj;
//         if(showMarathonDatas.ContainsKey(data.prefabName))
//         {
//             showMarathonDatas[data.prefabName].Add(data);
//         }
//         else
//         {
//             showMarathonDatas.Add(data.prefabName,new List<MarathonJsonData>(){data});
//         }
//     }
//     
//     private void OnDestroy()
//     {
//        
//         if (aoiManagers != null)
//         {
//             for (int i = 0; i < aoiManagers.Count; i++)
//             {
//                 if (aoiManagers[i] == null) continue;
//                 aoiManagers[i].DestoryAllItem();
//             }  
//             aoiManagers.Clear();
//             aoiManagers = new List<AOIManager>();
//         }
//
//         if (showMarathonDatas != null)
//         {
//             foreach (var data in showMarathonDatas)
//             {
//                 var info = data.Value;
//                 for (int i = 0; i < info.Count; i++)
//                 {
//                     if (info[i].obj != null)
//                     {
//                         GameObject.Destroy(info[i].obj);
//                     }
//                 }
//             }
//             showMarathonDatas.Clear();
//             showMarathonDatas = null;
//         }
//
//         if (hidePrefabs != null)
//         {
//             foreach (var data in hidePrefabs)
//             {
//                 var info = data.Value;
//                 for (int i = 0; i < info.Count; i++)
//                 {
//                     GameObject.Destroy(info[i]);
//                 }
//             }
//             hidePrefabs.Clear();
//             hidePrefabs = null;
//         }
//         pathIsLoading.Clear();
//         basePrefabs.Clear();
//     }
// }
