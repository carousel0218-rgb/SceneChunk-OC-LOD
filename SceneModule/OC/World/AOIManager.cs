
using System;
using System.Collections.Generic;
using game_logic;
using MRK.Game;
using UnityEngine;
using YooAsset;

public class AOIManager
{
    private WORLD_BOUND_TYPE boundType;
    private Transform parent;
    // 1= 9宫格 2 = 25宫格
    private int loadingRange = 1;

    private int unloadingRange = 1;
     //格子大小
    private int gridSize = 10;
    //格子列表,  当前gridId, 附近格子的gridId
    private Dictionary<int, List<int>> gridList = new Dictionary<int, List<int>>();
    //格子的对象列表
    private Dictionary<int, List<AOIItem>> itemList = new Dictionary<int, List<AOIItem>>();

    private Dictionary<AOIItem, int> itemDic = new Dictionary<AOIItem, int>();

    private Dictionary<string, AOIItem> hasItemDic = new Dictionary<string, AOIItem>();
    private List<string> nameList;

    private int lastGridId = 0;
    private int curGridId = -1;

    private int readyLoadItemCount = 0;

    private Action readyCall;

    private OCScene ocScene;
    private List<string> basePath;
    public AOIManager(OCScene oc,WORLD_BOUND_TYPE type,List<string> names,float width,int range,List<string> path,Action callback = null)
    {
        gridSize = (int)width;
        // radius = width * 2;
        loadingRange = range;
        boundType = type;
        nameList = names;
        basePath = path;
        unloadingRange = loadingRange + 1;
        ocScene = oc;
        
        gridList = new Dictionary<int, List<int>>();
        itemList = new Dictionary<int, List<AOIItem>>();
        itemDic = new Dictionary<AOIItem, int>();
        hasItemDic = new Dictionary<string, AOIItem>();
        
        TryParent();

        if (boundType == WORLD_BOUND_TYPE.Active_obj)
        {
          
            AddAcitityBase();
        }

        readyCall = callback;
    }

    private void AddAcitityBase()
    {
        // string path = ResourceData.Instance.ResourceWorldPrefabPath +
        //               boundType.ToString() + "/" + GameManager.Instance.TerrainType.ToString() + "/base";
        // var item = new AOIItem(ocScene,path, parent,0);
        // item.LoadingItem(0,null);
    }

    private int time;

    public void UpdateGrid()
    {
        var pos = PlayerCamera.Instance.GetCameraPosition();
        
        
        var v3 = GetVec3Int(pos);
        int gridId = EncodeGird(v3.x,v3.z);
        if (lastGridId != gridId)
        {   
            if (!gridList.ContainsKey(gridId))
            {   
                CheckLoad(gridId);
            };
            UpdateLoadItem(gridId,pos);
            UpdateUnLoadItem();
            lastGridId = gridId;
        }
        
        if (boundType == WORLD_BOUND_TYPE.Terrain_Mesh )
        {
            time += 1;
            if (time > 5)
            {
                UpdateMeshLoad(gridId,pos);
                time = 0;
            }
        }
    }

    private void CheckLoad(int gridId)
    {
        if (gridList.ContainsKey(gridId))
        {
            return;
        }
        int t = loadingRange * 2 + 1;
        int total = t * t;
        //计算9宫格、25宫格、49宫格...，从中心点出发，一格一格的往外计算
        int stepx = 0, stepz = 0, dx = 0, dz = -1;
        // var v3 = GetVec3Int(PlayerCamera.Instance.FocusedPlayer.Position);
        var data = DecodeGirdId(gridId);
        int chunkX = data.x;
        int chunkZ = data.z;

        List<int> list = new List<int>();
        for (int i = 0; i < total; i++)
        {
            if ((-loadingRange <= stepx) && (stepx <= loadingRange) && (-loadingRange <= stepz) &&
                (stepz <= loadingRange))
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    if (j != -1)
                        continue;
                    int posX = chunkX + stepx;
                    int posZ = chunkZ + stepz;
                    int curGrid =EncodeGird(posX,posZ);
                    list.Add(curGrid);
                }
            }

            if ((stepx == stepz) || ((stepx < 0) && (stepx == -stepz)) || ((stepx > 0) && (stepx == 1 - stepz)))
            {
                t = dx;
                dx = -dz;
                dz = t;
            }

            stepx += dx;
            stepz += dz;
        }
        gridList.Add(gridId,list);
    }

    private void UpdateLoadItem(int gridId,Vector3 pos)
    {   
       
        if (!itemList.ContainsKey(gridId))
        {
            var list = gridList[gridId];
            List<AOIItem> items = new List<AOIItem>();
            
            for (int i = 0; i < list.Count; i++)
            {
                var data = DecodeGirdId(list[i]);
                int chunkX = data.x;
                int chunkZ = data.z;
                string path = boundType.ToString() + "_" + chunkX + "_" + chunkZ;
                
                if (CheckIsInJson(path))
                {
                    if (hasItemDic.ContainsKey(path))
                    {
                        items.Add(hasItemDic[path]);
                    }
                    else
                    {
                        int index = GetPathIndex(path);
                        string firstPath = boundType + "/";
                        if (index >= 0 && basePath.Count > 0)
                        {
                            firstPath =  basePath[index]+ "/";
                        }
                        
                        OCScene oc = ocScene;
                        string path2 = null;
                        if (boundType == WORLD_BOUND_TYPE.Active_obj)
                        {
                            // firstPath = ResourceData.Instance.ResourceWorldPrefabPath + 
                            //             firstPath + GameManager.Instance.TerrainType.ToString() + "/";
                        }
                        else if (boundType == WORLD_BOUND_TYPE.Terrain_Mesh)
                        {
                            oc = null;

                            
                            // if (CheckIsLowMesh(list[i], pos))
                            // {
                            //     path2 = path + "_Low";
                            //     path2 = CheckIsInJson(path) ? path2 : null;
                            //     int index2 = GetPathIndex(path2);
                            //      
                            //     if (index2 >= 0)
                            //     {
                            //         firstPath =  basePath[index2]+ "/";
                            //     }
                            // }
                        }
                       
                        string path3 = path2 != null ? path2 : path;
                        firstPath = firstPath.Replace("Assets/Resources_Pack/", "");
                        var item = new AOIItem(oc,firstPath+ path3, parent,list[i],chunkX,chunkZ);
                        items.Add(item);
                      
                        hasItemDic.Add(path,item);
                    }
                  
                }
            }
            itemList.Add(gridId,items);
        }

        if (itemList[gridId].Count <= 0)
        {
            CheckReadyLoad();
            return;
        }

        if (GameManager.Instance.GameState == GameDefines.EGameState.Ready)
        {
            readyLoadItemCount = 0;
            UpdateLoad(gridId);
            return;
        }
        
       
        UpdateLoad(gridId);
        

    }
    
    private void UpdateMeshLoad(int gridId,Vector3 pos)
    {
        for (int i = 0; i < itemList[gridId].Count; i++)
        {   
           
            if (!itemDic.ContainsKey(itemList[gridId][i]))
            {
                itemDic.Add(itemList[gridId][i],gridId);
            }
            else
            {
                itemDic[itemList[gridId][i]] = gridId;
            }

            var item = itemList[gridId][i];
            if (item.eState == AOIItem.EGridState.Loading)
            {
                continue;
            }
            
            string path = boundType.ToString() + "_" + item.chunkX + "_" + item.chunkZ;
            string path2 = null;
            // if (CheckIsLowMesh(item.girdId, pos))
            // {
            //     path2 = path + "_Low";
            //     path2 = CheckIsInJson(path) ? path2 : null;
            // }
            
            string path3 = path2 == null ? path : path2;
            int index = GetPathIndex(path3);
            string firstPath = boundType + "/";
            if (index >= 0)
            {
                firstPath =  basePath[index]+ "/";
            }
            firstPath = firstPath.Replace("Assets/Resources_Pack/", "");
            item.UpdateItem(firstPath  + path3 +".prefab",0);
            
        }
    }

    private void UpdateLoad(int gridId)
    {
        float curTime = 0f;
        for (int i = 0; i < itemList[gridId].Count; i++)
        {
            if (!itemDic.ContainsKey(itemList[gridId][i]))
            {
                itemDic.Add(itemList[gridId][i],gridId);
            }
            else
            {
                itemDic[itemList[gridId][i]] = gridId;
            }
           
            if (itemList[gridId][i].eState == AOIItem.EGridState.Loading ||
                itemList[gridId][i].eState == AOIItem.EGridState.Load)
            {
                continue;
            }
            if (GameManager.Instance.GameState == GameDefines.EGameState.Ready)
            {
                readyLoadItemCount += 1;
            }
            curTime += 0.1f;
            itemList[gridId][i].LoadingItem(curTime,CheckReadyLoad);
            
        }
    }

    private int hasLoadCount = 0;
    private void CheckReadyLoad()
    {     
        if(readyLoadItemCount == -1)return;
        hasLoadCount += 1;
       
        if (hasLoadCount >= readyLoadItemCount)
        {   
            readyLoadItemCount = -1;
           
            readyCall?.Invoke();
        }
    }

    private void UpdateUnLoadItem()
    {   
        if(lastGridId == curGridId) return;
        if (!gridList.ContainsKey(lastGridId))
        {
            return;
        }
        var data = DecodeGirdId(lastGridId);
        int chunkX = data.x;
        int chunkZ = data.z;
        
        
        if (Mathf.Abs(chunkX - gridSize) > unloadingRange || Mathf.Abs(chunkZ - gridSize) > unloadingRange)
        {
            for (int i = 0; i < itemList[lastGridId].Count; i++)
            {
                if (!itemDic.ContainsKey(itemList[lastGridId][i]))
                {
                    itemDic.Add(itemList[lastGridId][i],lastGridId);
                }
                else
                {
                    if (itemDic[itemList[lastGridId][i]] == lastGridId)
                    {
                        itemList[lastGridId][i].UnLoadingItem();
                    }
                }
            }
        }
       
    }

    public void DestoryAllItem()
    {
        foreach (var item in hasItemDic)
        {
            var curItem = item.Value;
            if (curItem != null)
            {
                curItem.UnLoadingItem();
            }
        }
        gridList.Clear();
        itemList.Clear();
        itemDic.Clear();
        hasItemDic.Clear();
        gridList = new Dictionary<int, List<int>>();
        itemList = new Dictionary<int, List<AOIItem>>();
        itemDic = new Dictionary<AOIItem, int>();
        hasItemDic = new Dictionary<string, AOIItem>();
    }

    private void TryParent()
    {
        if (parent == null)
        {   
            string parentName = boundType.ToString();
            GameObject obj = GameObject.Find(parentName);
            if (obj == null)
            {
                obj = new GameObject(parentName);
            }

          
            if (boundType == WORLD_BOUND_TYPE.Active_obj)
            {
                // var trans = obj.transform.Find(GameManager.Instance.TerrainType.ToString());
                // if (trans == null)
                // {
                //     var obj2 = new GameObject(GameManager.Instance.TerrainType.ToString());
                //     obj2.transform.SetParent(obj.transform);
                //     obj2.transform.position = Vector3.zero;
                //     trans = obj2.transform;
                // }

                // parent = trans;
            }
            else
            {
                parent = obj.transform;
            }
           

          
        }
    }

    private bool CheckIsLowMesh(int gridId,Vector3 pos)
    {
        var data = DecodeGirdId(gridId);
        int chunkX = data.x;
        int chunkZ = data.z;
        pos.y = 0;
        int count = gridSize / 2;
        float dis = Vector3.Distance(pos, new Vector3(chunkX * gridSize + count, 0, chunkZ  * gridSize + count));
        return dis > gridSize;
        
    }

    private bool CheckIsInJson(string path)
    {
        if (nameList == null || nameList.Count <= 0) return false;
        return nameList.Contains(path);
    }

    private int GetPathIndex(string path)
    {
        if (nameList == null || nameList.Count <= 0) return -1;
        return nameList.IndexOf(path);
    }
    
    private Vector3Int GetVec3Int(Vector3 position)
    {
        int x = (int)(Mathf.FloorToInt(position.x / gridSize));
        if (Mathf.Abs((position.x / gridSize) - Mathf.RoundToInt(position.x / gridSize)) < 0.001f)
        {
            x = (int)Mathf.RoundToInt(position.x / gridSize);
        }
        int z = (int)(Mathf.FloorToInt(position.z / gridSize));
        if (Mathf.Abs((position.z / gridSize) - Mathf.RoundToInt(position.z / gridSize)) < 0.001f)
        {
            z = (int)Mathf.RoundToInt(position.z / gridSize);
        }
        return new Vector3Int(x, 0, z);
    }
    
    private const int Offset = 999;
    private const int Range = 1999;
    
    public static int EncodeGird(int x, int z)
    {
        // 转换为非负数坐标
        int xPos = x + Offset;
        int zPos = z + Offset;

        // 编码计算
        checked // 添加溢出保护
        {
            return xPos * Range + zPos;
        }
    }
    
    public (int x, int z) DecodeGirdId(int id)
    {
        int zMapped = (int)(id % Range);
        int xMapped = (int)(id / Range);

        int x = xMapped - Offset;
        int z = zMapped - Offset;

        // 还原原始值
        return (x, z);
    }
}
