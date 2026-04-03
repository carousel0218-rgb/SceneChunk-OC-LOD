using System;
using System.Collections.Generic;
using System.IO;
using MRK.Framework;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(ChunkNode))]
public class CheckWorldTools : EditorWindow 
{   
    string savepath = "Assets/Resources_Pack/Scenes/318";

    private Bounds baseBound;
    
    [Serializable]
    public class ChunkNode
    {   
        [SerializeField]
        public WORLD_BOUND_TYPE boundType;
        [SerializeField]
        public float width;
        [SerializeField]
        public string name;
    }

    
    private ReorderableList _chunkList;
    private SerializedObject serObj;
    private SerializedProperty gosPty;
    [SerializeField]
    public List<ChunkNode> chunks = new List<ChunkNode>
    {
    };
    
    
    public class  CheckBounds
    {
        public List<Bounds> bounds;
        public WORLD_BOUND_TYPE boundType;
        public List<GameObject> objList;
        public List<string> nameList;
        public float width;
        public GameObject parent;
        
        //地形的低模
        public List<GameObject> lowList;

        public List<string> path;
        
        //给活动用的
        // public GameObject baseObj;
        public List<GameObject> activityParentList;
        public Dictionary<string, List<GameObject>> activityList;
    }

    private List<CheckBounds> checkList;

    private Dictionary<WORLD_BOUND_TYPE, int> checkDic;
    
    [MenuItem("Tools/Scene/开放世界切割工具/WorldSlicing", priority = 10)]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        CheckWorldTools window = (CheckWorldTools)EditorWindow.GetWindow(typeof(CheckWorldTools));
        window.Show();
        
       
    }

    private void OnEnable()
    {
        chunks = new List<ChunkNode>();
        chunks.Add(new ChunkNode
        {
            boundType = WORLD_BOUND_TYPE.Terrain_Mesh,
            width = 1000,
        });
        
        chunks.Add(new ChunkNode
        {
            boundType = WORLD_BOUND_TYPE.Static_Obj,
            width = 600,
        });
        chunks.Add(new ChunkNode
        {
            boundType = WORLD_BOUND_TYPE.Big_Obj,
            width = 500,
        });
        chunks.Add(new ChunkNode
        {
            boundType = WORLD_BOUND_TYPE.Middle_Obj,
            width = 200,
        });
        chunks.Add(new ChunkNode
        {
            boundType = WORLD_BOUND_TYPE.Small_Obj,
            width = 60,
        });
        
        // chunks.Add(new ChunkNode
        // {
        //     boundType = WORLD_BOUND_TYPE.Active_obj,
        //     width = 150,
        // });
        // chunks.Add(new ChunkNode
        // {
        //     boundType = WORLD_BOUND_TYPE.RoadOrBase,
        //     width = 1500,
        // });
    }

    void OnDraw()
    {   
        //系列化对象的初始化
        serObj = new SerializedObject(this);
        gosPty = serObj.FindProperty("chunks");
        _chunkList = new ReorderableList(serObj, gosPty
            , true, true, true, true);
 
        //自定义列表名称
        _chunkList.drawHeaderCallback = (Rect rect) =>
        {
            GUI.Label(rect, "分割类型");
        };
 
        //定义元素的高度
        _chunkList.elementHeight = 80;
 
        //自定义绘制列表元素
        _chunkList.drawElementCallback = (Rect rect, int index, bool selected, bool focused) =>
        {
            //根据index获取对应元素 
            SerializedProperty item = _chunkList.serializedProperty.GetArrayElementAtIndex(index);
            rect.height -= 4;
            rect.y += 2;
            EditorGUI.PropertyField(rect, item, new GUIContent("Index " + index));
        };

        //当删除元素时候的回调函数，实现删除元素时，有提示框跳出
        _chunkList.onRemoveCallback = (ReorderableList list) =>
            {
                if (EditorUtility.DisplayDialog("Warnning","Do you want to remove this element?","Remove","Cancel"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };

      
    }

    
    void OnGUI()
    {   
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        savepath = EditorGUILayout.TextField("保存路径", savepath);
        EditorGUILayout.Space();
        
        OnDraw();
        serObj.Update();
        _chunkList.DoLayoutList();
        
       
        serObj.ApplyModifiedProperties();
          

        EditorGUILayout.Space();
        if (GUILayout.Button("Click"))
        {
            TryGetAllCheckNodes();
        }
        
        if (GUILayout.Button("Save"))
        {
            Save();
        }
    }

    private Vector3 center;
    private Vector3 maxBound;

    private WorldParentNode[] AllParentNodes;

    private List<WorldChildNode> _worldChildNodes;
    private List<OCObject> _ocObjects;
    private GameObject activityBaseNode;
    private List<GameObject> meshList;
    private List<GameObject> lowmeshList;

    #region Sclic 1111
       private  void Slicing()
    {
        
        // if (Directory.Exists(savepath)) Directory.Delete(savepath, true);
        // Directory.CreateDirectory(savepath);

        var parentNodes = GameObject.FindObjectsOfType<WorldParentNode>();
        AllParentNodes = parentNodes;
        if (parentNodes == null)
        {
            EditorUtility.DisplayDialog("error", "not found WorldParentNode", "ok");
        }

        float maxX = 0;
        float maxZ = 0;
        float maxY = 0;
        float minX = 0;
        float minY = 0;
        float minZ = 0;
        for (int i = 0; i < parentNodes.Length; i++)
        {   
            parentNodes[i].SetChildType();
            var objs = parentNodes[i].gameObject.GetComponentsInChildren<WorldChildNode>();
            for (int j = 0; j < objs.Length; j++)
            {
                Vector3 pos = objs[i].transform.position;
                if (maxX < pos.x)
                {
                    maxX = pos.x;
                }
                if (maxZ < pos.z)
                {
                    maxZ = pos.z;
                }
                if (maxY < pos.y)
                {
                    maxY = pos.y;
                }

                if (minX > pos.x)
                {
                    minX = pos.x;
                }
                if (minY > pos.y)
                {
                    minY = pos.y;
                }
                if (minZ > pos.z)
                {
                    minZ = pos.z;
                }
            }
        }

        center = new Vector3((maxX - minX) / 2 + minX, (maxY - minY) / 2 + minY, (maxZ - minZ) / 2 + minZ);
        maxBound = new Vector3((maxX - minX) , (maxY - minY), (maxZ - minZ));
        baseBound = new Bounds(center, maxBound);


        checkList = new List<CheckBounds>();
        for (int i = 0; i < chunks.Count; i++)
        {   
            CheckChunks(parentNodes,chunks[i].boundType,chunks[i].width);
        }
        
        

    }


    private void CheckChunks(WorldParentNode[] parentNodes,WORLD_BOUND_TYPE type,float width)
    {   
        GameObject parent = GameObject.Find(type.ToString());
        if (parent == null)
        {
            parent = new GameObject(type.ToString());
        }
        parent.transform.position = Vector3.zero;
        List<Bounds> bounds = new List<Bounds>();
        List<GameObject> curObjs = new List<GameObject>();
        List<string> names = new List<string>();
        CheckBounds checkBounds = new CheckBounds();
        List<string> paths = new List<string>();
        checkBounds.bounds = bounds;
        checkBounds.objList = curObjs;
        checkBounds.nameList = names;
        checkBounds.boundType = type;
        checkBounds.width = width;
        checkBounds.path = paths;
        checkList.Add(checkBounds);
        for (int j = 0; j < parentNodes.Length; j++)
        {
            if (parentNodes[j].parentType ==  type)
            {   
                
                var objs = parentNodes[j].GetAllChild();
                for (int h = 0; h < objs.Count; h++)
                {
                    var v3 = GetVec3Int(objs[h].transform.position,width);
                    int index = TryGetCheckBounds(checkBounds, v3, parent.transform, width);
                    var child = objs[h];
                    child.transform.SetParent(checkBounds.objList[index].transform);
                  
                }
               
            } 
        }

    }
    

    #endregion
 

  
    public void TryGetAllCheckNodes()
    {   
        checkList = new List<CheckBounds>();
        _worldChildNodes = new List<WorldChildNode>();
        _ocObjects = new List<OCObject>();
        checkDic = new Dictionary<WORLD_BOUND_TYPE, int>();
        var parentNodes = GameObject.FindObjectsOfType<GameObject>();
        var base1 = GameObject.Find("TerrainGroup");
        if (base1 == null)
        {
            LogHelper.LogError("当前找不到组件 TerrainGroup");
        }
        var base2 = GameObject.Find("TerrainGroup_Low");
        if (base2 == null)
        {
            LogHelper.LogError("当前找不到组件 TerrainGroup_Low");
        }
        var base3 = GameObject.Find("Activity");
        if (base3 == null)
        {
            LogHelper.LogError("当前找不到组件 Activity");
        }
        for (int i = 0; i < parentNodes.Length; i++)
        {
            if (parentNodes[i].transform.parent == null)
            {
                if (parentNodes[i].transform.name == "TerrainGroup")
                {
                    TryFindTerrainMesh(parentNodes[i]);
                }
                else if (parentNodes[i].transform.name == "TerrainGroup_Low")
                {
                    TryFindTerrainMesh(parentNodes[i],true);
                }
                else if (parentNodes[i].transform.name == "Activity")
                {
                    activityBaseNode = parentNodes[i];
                }
                else if (parentNodes[i].transform.name == "Background")
                {
                    
                }
                else
                {   
                    //先取所有的worldchildNode
                    TryGetChild(parentNodes[i].transform);
                }
            } 
          
        }

        for (int i = 0; i < chunks.Count; i++)
        {
            CheckChunks2(chunks[i].boundType, chunks[i].width);
        }
        
        var parentNodes2 = GameObject.FindObjectsOfType<WorldParentNode>();
        if (parentNodes2 == null)
        {
            EditorUtility.DisplayDialog("error", "not found WorldParentNode", "ok");
        }

        for (int i = 0; i < parentNodes2.Length; i++)
        {
            parentNodes2[i].SetChildType();
        }

        AddChunkItems();
    }
    
    private void AddChunkItems()
    {
        for (int j = 0; j < _worldChildNodes.Count; j++)
        {   
            var node = _worldChildNodes[j];
            if (IsMeshSizeOverThreshold(node.gameObject, 500f))
            {
                SetChunkItem(WORLD_BOUND_TYPE.RoadOrBase, node.gameObject);
                continue; // 避免重复处理
            }
            var type = _worldChildNodes[j].GetCurType();
            SetChunkItem(type, _worldChildNodes[j].gameObject);
        }
        for (int j = 0; j < _ocObjects.Count; j++)
        {
            var ocType = _ocObjects[j].checkType;
            var type = WORLD_BOUND_TYPE.Big_Obj;
            if (ocType == OCItemCheckType.MIDDLE)
            {
                type = WORLD_BOUND_TYPE.Middle_Obj;
            }else if (ocType == OCItemCheckType.SMALL)
            {
                type = WORLD_BOUND_TYPE.Small_Obj;
            }
            else if (ocType == OCItemCheckType.BIG)
            {
                type = WORLD_BOUND_TYPE.Big_Obj;
            }
           
            else if (ocType == OCItemCheckType.ACTIVITY)
            {
                continue;
            }
            SetChunkItem(type, _ocObjects[j].gameObject);
        }

        AddAcitityItem();
        AddMeshItem();
    }

    private void AddMeshItem()
    {
        for (int i = 0; i < meshList.Count; i++)
        {
            SetChunkItem(WORLD_BOUND_TYPE.Terrain_Mesh,meshList[i]);
        }

        if (lowmeshList != null)
        {
            for (int i = 0; i < lowmeshList.Count; i++)
            {
                SetChunkItem(WORLD_BOUND_TYPE.Terrain_Mesh,lowmeshList[i],true);
            } 
        }
       
    }
    private void AddAcitityItem()
    {
        CheckBounds activityCheckBounds = null ;
        if (checkDic.ContainsKey(WORLD_BOUND_TYPE.Active_obj))
        {
            int key = checkDic[WORLD_BOUND_TYPE.Active_obj];
            activityCheckBounds = checkList[key];
            activityCheckBounds.activityParentList = new List<GameObject>();
            activityCheckBounds.activityList = new Dictionary<string, List<GameObject>>();
        }
        if(activityBaseNode == null)return;
        for (int i = 0; i < activityBaseNode.transform.childCount; i++)
        {
            var child = activityBaseNode.transform.GetChild(i);
            var str =  child.name;
            Transform trans = activityCheckBounds.parent.transform.Find(str);
            if (trans == null)
            {
                var obj = new GameObject(str);
                obj.transform.SetParent(activityCheckBounds.parent.transform);
                obj.transform.position = Vector3.zero;
                trans = obj.transform;
                activityCheckBounds.activityParentList.Add(obj);
                TryGetAcitityItem(child.gameObject, obj,activityCheckBounds);
            }
        }
    }

    private void TryFindTerrainMesh(GameObject obj,bool isLow = false)
    {

        if (isLow == false)
        {
            meshList = new List<GameObject>();
            TryGetChildTerrain(obj, meshList);
        }
        else
        {
            lowmeshList = new List<GameObject>();
            TryGetChildTerrain(obj, lowmeshList);
        }
        
        

    }
    
    // private 
    
    /// <summary>
    /// 初始化chunk
    /// </summary>
    /// <param name="type"></param>
    /// <param name="width"></param>
    private void CheckChunks2(WORLD_BOUND_TYPE type,float width)
    {
        GameObject parent = GameObject.Find(type.ToString());
        if (parent == null)
        {
            parent = new GameObject(type.ToString());
        }
        parent.transform.position = Vector3.zero;
        List<Bounds> bounds = new List<Bounds>();
        List<GameObject> curObjs = new List<GameObject>();
        List<string> names = new List<string>();
        CheckBounds checkBounds = new CheckBounds();
        List<string> paths = new List<string>();
        checkBounds.bounds = bounds;
        checkBounds.objList = curObjs;
        checkBounds.nameList = names;
        checkBounds.boundType = type;
        checkBounds.width = width;
        checkBounds.parent = parent;
        checkBounds.path = paths;
        checkList.Add(checkBounds);
        checkDic.Add(type,checkList.Count - 1);
    }

    

    private void TryGetAcitityItem(GameObject curObj,GameObject parent,CheckBounds checkBounds)
    {
        List<GameObject> list = new List<GameObject>();
        TryGetActivityChild(curObj,list);
        for (int i = 0; i < list.Count; i++)
        {
            var obj = list[i];
            var v3 = GetVec3Int(obj.transform.position,checkBounds.width);
            int index = TryGetActivityCheckBounds(checkBounds, v3, parent.transform, checkBounds.width);
            var child =obj;
            child.transform.SetParent(checkBounds.activityList[parent.name][index].transform);
        }
        
        var trans = parent.transform.Find("base");
        if (trans == null)
        {
            GameObject obj = new GameObject("base");
            obj.transform.SetParent(parent.transform);
            obj.transform.position = Vector3.zero;
            trans = obj.transform;
        }

        checkBounds.activityList[parent.name].Add(trans.gameObject);

        var transList = new List<Transform>();
        for (int i = 0; i < curObj.transform.childCount; i++)
        {
            var child = curObj.transform.GetChild(i);
            transList.Add(child.transform);
        }

        for (int i = 0; i < transList.Count; i++)
        {
            transList[i].SetParent(trans);
        }
    }

    private void TryGetActivityChild(GameObject curObj,List<GameObject> list,bool isActivity = true)
    {
        for (int i = 0; i < curObj.transform.childCount; i++)
        {
            var child = curObj.transform.GetChild(i);
            if (child != null)
            {
                if (child.GetComponent<OCObject>())
                {
                   
                    list.Add(child.gameObject);

                    
                }
                else
                {       
                    
                    //静态的先不做处理，看看效果
                     if (IsPrefabInstance(child.gameObject) )
                     {   
                         // Debug.LogError("________当前是预制体并且没有oc/worldnode  "+child.name);
                         // var node = child.gameObject.AddComponent<WorldChildNode>();
                         // node.WorldBoundType = WORLD_BOUND_TYPE.Static_Obj;
                         // list.Add(child.gameObject);
                         
                         // if (child.GetComponent<ModelCloth>())
                         // {
                         // }
                     }
                     else
                     {
                         if (child.childCount > 0 && child.name != "Background"  && child.name != "Road Network")
                         {
                             TryGetActivityChild(child.gameObject, list);
                         }
                     }
                }
                   
                
            }
            
        }
    }
    private void TryGetChildTerrain(GameObject curObj,List<GameObject> list)
    {
        for (int i = 0; i < curObj.transform.childCount; i++)
        {
            var child = curObj.transform.GetChild(i);
            if (child != null)
            {
                if (child.GetComponent<WorldChildNode>())
                {
                    list.Add(child.gameObject);
                }
                else
                {
                    if (child.childCount > 0)
                    {
                        TryGetActivityChild(child.gameObject,list,false);
                    }
                }
            }
            
        }
    }

    private bool IsPrefabInstance(GameObject gameObject)
    {   
        var type = PrefabUtility.GetPrefabAssetType(gameObject);
        var status = PrefabUtility.GetPrefabInstanceStatus(gameObject);
        // 是否为预制体实例判断
        if (type == PrefabAssetType.NotAPrefab || status == PrefabInstanceStatus.NotAPrefab)
        {   
          
            return false;
        }
        return true;

    }

    private void SetChunkItem(WORLD_BOUND_TYPE type, GameObject obj ,bool isLow = false)
    {
        if (IsMeshSizeOverThreshold(obj, 500f))
        {
            type = WORLD_BOUND_TYPE.RoadOrBase; // 强制覆盖为 RoadOrBase
        }

        if (checkDic.ContainsKey(type) )
        {
            int key = checkDic[type];
            var checkBounds = checkList[key];

            var v3 = GetVec3Int(obj.transform.position,checkBounds.width);
            int index = TryGetCheckBounds(checkBounds, v3, checkBounds.parent.transform, checkBounds.width,isLow);
            var child =obj;
            if (isLow)
            {   
                child.transform.SetParent(checkBounds.lowList[index - checkBounds.objList.Count].transform);
            }
            else
            {   
               if(checkBounds.objList.Count <=0)return;
                child.transform.SetParent(checkBounds.objList[index].transform);
                
            }
           
        } 
    }
    
    private bool IsMeshSizeOverThreshold(GameObject obj, float threshold = 500f)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return false;

        // 获取本地空间下的包围盒尺寸
        Vector3 localSize = meshFilter.sharedMesh.bounds.size;
        // 计算世界空间下的实际尺寸（考虑缩放）
        Vector3 worldSize = Vector3.Scale(localSize, obj.transform.lossyScale);

        // 判断任一维度是否超过阈值
        return worldSize.x > threshold || worldSize.y > threshold || worldSize.z > threshold;
    }
    
    public int TryGetCheckBounds(CheckBounds checkBounds,Vector3Int v3,Transform parent,float width,bool isLow = false)
    {
        var type = checkBounds.boundType;
        var str = type.ToString() + "_" + v3.x + "_" + v3.z;
        if (isLow)
        {
            str += "_Low";
        }

        
        GameObject obj = GameObject.Find(str);
        int index = 0;
        if (obj == null)
        {   
            
            obj = new GameObject(str);
            obj.transform.SetParent(parent);
            obj.transform.position =  Vector3.zero;
            checkBounds.nameList.Add(str);
           
            if (isLow)
            {
                if (checkBounds.lowList == null)
                {
                    checkBounds.lowList = new List<GameObject>();
                }
                checkBounds.lowList.Add(obj);
                checkBounds.path.Add(savepath + "/"  + checkBounds.boundType  + "/Low/" + v3.x);
            }
            else
            {   
                if (type == WORLD_BOUND_TYPE.Terrain_Mesh)
                {
                    checkBounds.path.Add(savepath  + "/" + checkBounds.boundType + "/" + v3.x);
                }
                else
                {
                    checkBounds.path.Add(savepath + "/" + checkBounds.boundType);
                }
                checkBounds.objList.Add(obj);
            }
            var curMax = new Vector3(width, 0, width);
            Bounds curBound = new Bounds(v3,curMax);
            checkBounds.bounds.Add(curBound);
            index = checkBounds.nameList.Count - 1;
        }
        else
        {
            for (int i = 0; i <  checkBounds.nameList.Count; i++)
            {
                if (checkBounds.nameList[i] == str)
                {
                    index = i;
                    break;
                }
            }
        }

        return index;

    }
    
    public int TryGetActivityCheckBounds(CheckBounds checkBounds,Vector3Int v3,Transform parent,float width)
    {
        var type = checkBounds.boundType;
        var str = type.ToString() + "_" + v3.x + "_" + v3.z;
        var trans = parent.transform.Find(str);
        int index = 0;
        if (trans == null)
        {   
           
            var obj = new GameObject(str);
            obj.transform.SetParent(parent);
            obj.transform.position =  Vector3.zero;
            if (!checkBounds.activityList.ContainsKey(parent.name))
            {
                List<GameObject> list = new List<GameObject>();
                checkBounds.activityList.Add(parent.name , list);
            }
            checkBounds.activityList[parent.name].Add(obj);
            index = checkBounds.activityList[parent.name].Count - 1;
        }
        else
        {   
            if (checkBounds.activityList.ContainsKey(parent.name))
            {
                for (int i = 0; i <  checkBounds.activityList[parent.name].Count; i++)
                {
                    if (checkBounds.activityList[parent.name][i].name == str)
                    {
                        index = i;
                        break;
                    }
                }
            }
            else
            {
                List<GameObject> list = new List<GameObject>();
                checkBounds.activityList.Add(parent.name , list);
                checkBounds.activityList[parent.name].Add(trans.gameObject);
                index = checkBounds.activityList[parent.name].Count - 1;
            }
            
        }

        return index;

    }


    private void TryGetChild(Transform tran)
    {
        for (int i = 0; i < tran.childCount; i++)
        {
            var obj = tran.GetChild(i);
            
            var worldChild = obj.GetComponent<WorldChildNode>();
            if (worldChild != null)
            {
                _worldChildNodes.Add(worldChild);
            }
            else
            {   
                var oc = obj.GetComponent<OCObject>();
                if (oc != null)
                {
                    _ocObjects.Add(oc);
                }
                else
                {   
                    if (IsPrefabInstance(obj.gameObject) && obj.parent.name != "Background"
                        
                        && obj.parent.name != "Road Network")
                    {   
                        // Debug.LogError("________当前是预制体并且没有oc/worldnode 22  "+obj.name);
                        var node = obj.gameObject.AddComponent<WorldChildNode>();
                        node.WorldBoundType = WORLD_BOUND_TYPE.Static_Obj;
                        _worldChildNodes.Add(node);
                    }
                    else
                    {
                        if (obj.childCount > 0 && obj.name != "Background"   
                                               && obj.name != "Road Network"
                                               && obj.name != "Activity"
                                               && obj.name != "TerrainGroup"
                                               && obj.name != "TerrainGroup_Low")
                        {
                            TryGetChild(obj);
                        } 
                    }
                    
                }

               
            }
           
        }
    }
    
   
    
    Vector3Int GetVec3Int(Vector3 position, float width)
    {
        int x = (int)(Mathf.FloorToInt(position.x / width));
        if (Mathf.Abs((position.x / width) - Mathf.RoundToInt(position.x / width)) < 0.001f)
        {
            x = (int)Mathf.RoundToInt(position.x / width);
        }
        int z = (int)(Mathf.FloorToInt(position.z / width));
        if (Mathf.Abs((position.z / width) - Mathf.RoundToInt(position.z / width)) < 0.001f)
        {
            z = (int)Mathf.RoundToInt(position.z / width);
        }
        return new Vector3Int(x, 0, z);
    }

    #region Save
    /// <summary>
    /// 将所有分割出来的块，按类型存储
    /// </summary>
    private void SaveTxt()
    {
        string path = savepath + "/jsonData_318.json";
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }

        List<SaveJsonData> jsonData = new List<SaveJsonData>();
        //当存储列表为空时
        if (checkList == null || checkList.Count <= 0)
        {
            Debug.LogError("重新来");
            return;
            for (int i = 0; i < chunks.Count; i++)
            {
                var str = chunks[i].boundType.ToString();
                GameObject obj = GameObject.Find(str);
                var data = new SaveJsonData();
                bool hasSet = false;
                for (int j = 0; j < jsonData.Count; j++)
                {
                    if (jsonData[j].boundType ==chunks[i].boundType)
                    {
                        data = jsonData[j];
                        hasSet = true;
                        break;
                    }
                }

                var chunk = new ChunkNode();
                for (int j = 0; j < chunks.Count; j++)
                {
                    if (chunks[j].boundType == chunks[i].boundType)
                    {
                        chunk = chunks[j];
                    }
                }

                if (data.width <= 0)
                {
                    data.boundType = chunks[i].boundType;
                    data.width = chunk.width;
                    data.nameList = new List<string>();
                }
                
                if (obj != null)
                {   
                    //活动的特殊处理，多个父节点
                    if (chunks[i].boundType == WORLD_BOUND_TYPE.Active_obj)
                    {   
                        data.activityNameList = new Dictionary<string, List<string>>();
                        for (int j = 0; j < obj.transform.childCount; j++)
                        {   
                            var child = obj.transform.GetChild(j);
                            List<string> curNameList = new List<string>();
                            for (int k = 0; k < child.childCount; k++)
                            {
                               curNameList.Add(child.GetChild(k).name);
                            }
                            if (data.activityNameList.ContainsKey(child.name))
                            {
                                data.activityNameList[child.name] = curNameList;
                            }
                            else
                            {
                                data.activityNameList.Add(child.name,curNameList);
                            }

                        } 
                    }
                    else
                    {
                        for (int j = 0; j < obj.transform.childCount; j++)
                        {
                            var child = obj.transform.GetChild(j);
                            if (child != null)
                            {
                                data.nameList.Add(child.name);
                            }
                        } 
                    }
                  
                   
                }

                if (hasSet == false)
                {
                    jsonData.Add(data);
                }
            }
        }
        else
        {
            for (int i = 0; i < checkList.Count; i++)
            {
                var data = new SaveJsonData();
                data.boundType = checkList[i].boundType;
                data.nameList = checkList[i].nameList;
                data.width = checkList[i].width;
                data.path = checkList[i].path;
                //活动的特殊处理
                if (checkList[i].boundType == WORLD_BOUND_TYPE.Active_obj)
                {
                    data.activityNameList = new Dictionary<string, List<string>>();
                  
                    var nodes = checkList[i].activityList;
                    foreach (var item in nodes)
                    {
                        var key = item.Key;
                        List<string> curNameList = new List<string>();
                        
                        var list = item.Value;
                        for (int k = 0; k < list.Count; k++)
                        {
                            curNameList.Add(list[k].name);
                        }
                        
                        if (data.activityNameList.ContainsKey(key))
                        {
                            data.activityNameList[key] = curNameList;
                        }
                        else
                        {
                            data.activityNameList.Add(key,curNameList);
                        }
                    }
                    
                }
                
                jsonData.Add(data);
            }
        }
        
        string json = JsonConvert.SerializeObject(jsonData);
        File.WriteAllText(path, json);
        if (GameObject.Find("WorldLoad") == null)
        {
            GameObject loadObj = new GameObject("WorldLoad");
            loadObj.AddComponent<WorldLoad>();
        }
        Debug.LogError("保存成功");
    }

    
    
    private void Save()   
    {   
        string meshPath = savepath + "/Terrain_Mesh";
        if (Directory.Exists(meshPath))
        {
            Directory.Delete(meshPath,true);
        }
        
        SaveTxt();
        //列表为空时
        if (checkList == null || checkList.Count <= 0)
        {
            Debug.LogError("重来");
            return;
            for (int i = 0; i < chunks.Count; i++)
            {
                var str = chunks[i].boundType.ToString();
                GameObject obj = GameObject.Find(str);
                if (obj != null)
                {   
                    string firstPath = savepath + "/" + str;
                    if (!Directory.Exists(firstPath))
                        Directory.CreateDirectory(firstPath);
                    for (int j = 0; j < obj.transform.childCount; j++)
                    {
                        var child = obj.transform.GetChild(j);
                        if (child != null)
                        {

                            string path =  firstPath +"/"+ child.name + ".prefab";
                            if (chunks[i].boundType == WORLD_BOUND_TYPE.Active_obj)
                            {   
                                string secondPath = firstPath + "/" + child.name;
                                if (!Directory.Exists(secondPath))
                                    Directory.CreateDirectory(secondPath);
                                for (int k = 0; k < child.transform.childCount; k++)
                                {
                                   
                                    var nextChild = child.transform.GetChild(k);
                                    path = secondPath + "/"+nextChild.name + ".prefab";
                                    if (nextChild.name != "base")
                                    {
                                        for (int l = 0; l < nextChild.childCount; l++)
                                        {   
                                            var next2Child = nextChild.transform.GetChild(l);
                                            if (next2Child != null)
                                            {
                                                next2Child.gameObject.SetActive(false);
                                            }
                                        }
                                    }
                                    //存路径和物体
                                    // path = AssetDatabase.GenerateUniqueAssetPath(path);
                                    PrefabUtility.SaveAsPrefabAsset(child.gameObject,path);
                                }
                            }
                           
                            else
                            {   
                                if (chunks[i].boundType == WORLD_BOUND_TYPE.Terrain_Mesh || chunks[i].boundType == WORLD_BOUND_TYPE.Static_Obj )
                                {
                                    //地形的不需要子物体隐藏，加载的时候也是默认显示
                                }
                                else
                                {
                                    for (int k = 0; k < child.transform.childCount; k++)
                                    {
                                        var nextChild = child.transform.GetChild(k);
                                        if (nextChild != null)
                                        {
                                            nextChild.gameObject.SetActive(false);
                                        }
                                    }
                                }
                                // path = AssetDatabase.GenerateUniqueAssetPath(path);
                                PrefabUtility.SaveAsPrefabAsset(child.gameObject,path);
                            }
                            
                          
                            
                           
                        }
                    }
                   
                }
            }
        }
        else
        {
            for (int i = 0; i < checkList.Count; i++)
            {   
                    // AssetDatabase.CreateFolder(savepath, checkList[i].boundType.ToString());

                if (checkList[i].boundType == WORLD_BOUND_TYPE.Active_obj)
                {   
                    string firstPath = savepath + "/" + checkList[i].boundType;
                    if (!Directory.Exists(firstPath))
                        Directory.CreateDirectory(firstPath);
                    foreach (var item in checkList[i].activityList)
                    {
                        var key = item.Key;
                        string secondPath = firstPath + "/" + key;
                        if (!Directory.Exists(secondPath))
                            Directory.CreateDirectory(secondPath);
                        var items = item.Value;
                        for (int j = 0; j < items.Count; j++)
                        {
                            if (items[j].name != "base")
                            {
                                for (int k = 0; k < items[j].transform.childCount; k++)
                                {
                                    var nextChild = items[j].transform.GetChild(k);
                                    if (nextChild != null)
                                    {
                                        nextChild.gameObject.SetActive(false);
                                    }
                                }
                            }
                           
                            string path =  secondPath+ "/" + items[j].name + ".prefab";
                            bool isSmall =  CheckInCache(path, items[j]);
                            if (!isSmall)
                            {
                                path = AssetDatabase.GenerateUniqueAssetPath(path);
                                PrefabUtility.SaveAsPrefabAsset(items[j],path);
                            }
                        }
                    }
                }
              
                else
                {
                    for (int j = 0; j < checkList[i].objList.Count; j++)
                    {   
                        string firstPath = checkList[i].path[j];
                        if (!Directory.Exists(firstPath))
                            Directory.CreateDirectory(firstPath);
                        bool isSmall = false;
                        string path =  firstPath +"/"+ checkList[i].objList[j].name + ".prefab";
                        if (chunks[i].boundType == WORLD_BOUND_TYPE.Terrain_Mesh)
                        {
                            //地形的不需要子物体隐藏，加载的时候也是默认显示
                            for (int k = 0; k < checkList[i].objList[j].transform.childCount; k++)
                            {
                                var nextChild = checkList[i].objList[j].transform.GetChild(k);
                                if (nextChild != null)
                                {
                                    if (nextChild.name.Contains("TerrainData"))
                                    {
                                        nextChild.name = checkList[i].objList[j].name;
                                        // var comp = nextChild.GetComponent<TerrainToMeshConversionDetails>();
                                        // if (comp != null)
                                        // {
                                        //     DestroyImmediate(comp);
                                        // }
                                    }
                                }
                            }
                          
                        }
                        else if (chunks[i].boundType == WORLD_BOUND_TYPE.Static_Obj ||chunks[i].boundType == WORLD_BOUND_TYPE.RoadOrBase)
                        {
                            //静态物体不需要隐藏
                            
                            isSmall = CheckInCache(path, checkList[i].objList[j]);
                        }
                        else
                        {   
                            //存到prefab内的子物体，默认隐藏，为了加载的时候不会出现显示再隐藏的现象
                            for (int k = 0; k < checkList[i].objList[j].transform.childCount; k++)
                            {
                                var nextChild = checkList[i].objList[j].transform.GetChild(k);
                                if (nextChild != null)
                                {
                                    nextChild.gameObject.SetActive(false);
                                }
                            }
                            isSmall = CheckInCache(path, checkList[i].objList[j]);
                        }

                        if (!isSmall)
                        {
                            path = AssetDatabase.GenerateUniqueAssetPath(path);
                            PrefabUtility.SaveAsPrefabAsset(checkList[i].objList[j],path);
                        }
                       
        
                    }

                    if (checkList[i].lowList != null)
                    {
                        if ( checkList[i].lowList.Count > 0)
                        {
                            for (int j = 0; j < checkList[i].lowList.Count; j++)
                            {   
                                string firstPath = checkList[i].path[meshList.Count + j];
                                if (!Directory.Exists(firstPath))
                                    Directory.CreateDirectory(firstPath);
                                
                                string path =  firstPath +"/"+ checkList[i].lowList[j].name + ".prefab";
                                path = AssetDatabase.GenerateUniqueAssetPath(path);
                                PrefabUtility.SaveAsPrefabAsset(checkList[i].lowList[j],path);
        
                            }
                        }
                    }
                   
                }
               
            } 
        }

        DelNotInCache();
        SaveToCache();
    }

    private Dictionary<string, List<string>> cacheDic;

    private void SaveCache(string parent, GameObject obj)
    {
        if (cacheDic == null)
        {
            cacheDic = new Dictionary<string, List<string>>();
        }

        List<string> list = new List<string>();
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            list.Add(obj.transform.GetChild(i).name);
        }
        cacheDic.Add(parent,list);
    }

    private Dictionary<string, List<string>> lastCacheDic;
    private bool CheckInCache(string parent, GameObject obj)
    {
        if (lastCacheDic == null || lastCacheDic.Count <= 0)
        {
            var data = Resources.Load<TextAsset>( "cacheData");
            if (data != null)
            {
                lastCacheDic = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data.text);
            }
           
        }

        bool isSmall = false;
        
        if (lastCacheDic != null && lastCacheDic.ContainsKey(parent))
        {

            if (lastCacheDic[parent].Count != obj.transform.childCount)
            {
                isSmall = false;
            }
            else
            {
                int count = 0;
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    if (lastCacheDic[parent].Contains(obj.transform.GetChild(i).name))
                    {
                        count++;
                    }
                }
                isSmall = count == lastCacheDic[parent].Count;
            }
        }

        if (!isSmall)
        {
            AssetDatabase.DeleteAsset(parent);
        }
        SaveCache(parent, obj);
        return  isSmall;
        
    }

    private void DelNotInCache()
    {
        if(lastCacheDic == null) return;
        foreach (var lastItem in lastCacheDic)
        {
            var lastKey = lastItem.Key;
            if (!cacheDic.ContainsKey(lastKey))
            {
                AssetDatabase.DeleteAsset(lastKey);
            }
        }
    }
    
    private void SaveToCache()
    {
        string path = "Assets/Resources/cacheData.json";
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        string json = JsonConvert.SerializeObject(cacheDic);
        File.WriteAllText(path, json);
    }
    
    #endregion
    public class SaveJsonData
    {
        public WORLD_BOUND_TYPE boundType;
        public List<string> nameList;
        public float width;
        public List<string> path;
        public Dictionary<string, List<string>> activityNameList;
    }

  
}

[CustomPropertyDrawer(typeof(CheckWorldTools.ChunkNode))]
public class ChunkDrawer : PropertyDrawer 
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //创建一个属性包装器，用于将常规GUI控件与SerializedProperty一起使用
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            //设置属性名宽度
            EditorGUIUtility.labelWidth = 50;
            position.height = EditorGUIUtility.singleLineHeight;
            
            var prefabRect = new Rect(position)
            {
                width = position.width - 80,
                x = position.x + 50
            };
            
            var widthRect = new Rect(prefabRect) 
            {   
                x = position.x,
                y = prefabRect.y + EditorGUIUtility.singleLineHeight + 5
            };

            var nameRect = new Rect(widthRect) 
            {   
                y = prefabRect.y + EditorGUIUtility.singleLineHeight + 30
            };
              
            
            //找到每个属性的序列化值
            SerializedProperty boundTypeProperty = property.FindPropertyRelative("boundType");
            SerializedProperty widthProperty = property.FindPropertyRelative("width");
            SerializedProperty nameProperty = property.FindPropertyRelative("name");

            
            EditorGUI.PropertyField(position, boundTypeProperty,new GUIContent( "类型"));
            position.y += EditorGUIUtility.singleLineHeight;
            
            // boundTypeProperty. = EditorGUI.ObjectField(prefabRect, boundTypeProperty.objectReferenceValue, typeof(WORLD_BOUND_TYPE), false);
            widthProperty.floatValue = EditorGUI.FloatField(widthRect,"分割区域的大小", widthProperty.floatValue);
           
            //绘制name
            nameProperty.stringValue = EditorGUI.TextField(nameRect, "名称", nameProperty.stringValue);

         
        }
    }
}
