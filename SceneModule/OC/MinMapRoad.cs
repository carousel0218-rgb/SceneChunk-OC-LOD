// using System.Collections;
// using System.Collections.Generic;
// using System;
// using UnityEngine;
//
// public class MinMapRoad : MonoBehaviour
// {   
//     
//     private bool isShow;
//     private bool isLoading = false;
//     private GameObject miniMap;
//     private bool isStart = false;
//     private void Start()
//     {   
//         isShow = false;
//         isLoading = false;
//
//         StartCoroutine(Wait());
//     }
//
//     private IEnumerator Wait()
//     {
//         yield return new WaitForSeconds(3);
//         LoadMiniMap();
//         
//     }
//
//     private void Update()
//     {
//         if (isStart)
//         {
//             LoadMiniMap();
//         }
//     }
//
//
//     public void LoadMiniMap()
//     {
//         isStart = true;
//         if(GameSystem.IsWorking && isShow == false && !isLoading){
//             if(PlayerData.Instance.Goal != null && PlayerData.Instance.Goal.RoadID >0)
//             {
//                 isStart = false;
//                 EventManager.Instance.RegistEvent(Events.SendOnExit, OnExit);
//                 isLoading = true;
//                 LoadMiniMap(PlayerData.Instance.Goal.RoadID);
//                
//             }
//         }
//         
//     }
//
//     private void LoadMiniMap(int roadId)
//     {
//         if (CheckUtil.CheckIsLoopRoad())
//             return;
//
//         GameRoot.Root.StartCoroutine(ResourceManager.Instance.LoadInstanceAsync("Assets/Res/Terrains/Road/Dao_Lu_" + roadId, (obj) =>
//         {
//             if (obj == null)
//             {   
//                 Console.LogError("当前路径找不到模型  Assets/Res/Terrains/Road/"+roadId);
//                 return;
//             }
//             else
//             {
//                 obj.SetActive(true);
//                 Transform curTran = obj.transform;
//                 curTran.SetParent(transform);
//                 // curTran.localPosition = Vector3.zero;
//                 isShow = true;
//                 miniMap = obj;
//
//                 // var mesh = miniMap.GetComponent<MeshFilter>();
//                 // var reander = miniMap.GetComponent<MeshRenderer>();
//                 // mesh.mesh = reander.;
//             }
//             isLoading = false;
//         }));
//     }
//
//     
//     private void OnExit(){
//        
//         isShow = false;
//         isLoading = false;
//         if (miniMap != null)
//         {
//             DestroyImmediate(miniMap);
//         }
//     }
//
//
//     private void OnDestroy()
//     {
//        
//
//     }
// }
