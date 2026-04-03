using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(ChunkActivityNode))]
public class CheckWorldActivityTools : EditorWindow 
{   
    string savepath = "Assets/Res/Scenes/WorldScene";

    private Bounds baseBound;
    
    [Serializable]
    public class ChunkActivityNode
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
    public List<ChunkActivityNode> activityChunks = new List<ChunkActivityNode>
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
        
        //给活动用的
        // public GameObject baseObj;
        public List<GameObject> activityParentList;
        public Dictionary<string, List<GameObject>> activityList;
    }

    private List<CheckBounds> checkList;

    private Dictionary<WORLD_BOUND_TYPE, int> checkDic;
    
    [MenuItem("Tools/Scene/开放世界切割工具/活动切割", priority = 11)]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        CheckWorldActivityTools window = (CheckWorldActivityTools)EditorWindow.GetWindow(typeof(CheckWorldActivityTools));
        window.Show();
        
       
    }

    private void OnEnable()
    {
        activityChunks = new List<ChunkActivityNode>();
        activityChunks.Add(new ChunkActivityNode
        {
            boundType = WORLD_BOUND_TYPE.Active_obj,
            width = 150,
        });
    }

    void OnDraw()
    {   
        //系列化对象的初始化
        serObj = new SerializedObject(this);
        gosPty = serObj.FindProperty("activityChunks");
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
        
        // EditorGUI.BeginChangeCheck();
      
        // if (EditorGUI.EndChangeCheck())
        // {   
            serObj.ApplyModifiedProperties();
          
        // }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Click"))
        {
            // Slicing();
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
    
  
    public void TryGetAllCheckNodes()
    {   
        checkList = new List<CheckBounds>();
        _worldChildNodes = new List<WorldChildNode>();
        _ocObjects = new List<OCObject>();
        checkDic = new Dictionary<WORLD_BOUND_TYPE, int>();
        var parentNodes = GameObject.FindObjectsOfType<GameObject>();
        for (int i = 0; i < parentNodes.Length; i++)
        {
            if (parentNodes[i].transform.parent == null)
            {
                if (parentNodes[i].transform.name == "Activity")
                {
                    activityBaseNode = parentNodes[i];
                }
            } 
          
        }

        for (int i = 0; i < activityChunks.Count; i++)
        {
            CheckChunks2(activityChunks[i].boundType, activityChunks[i].width);
        }
        

        AddChunkItems();
    }
    
    private void AddChunkItems()
    {
        for (int j = 0; j < _worldChildNodes.Count; j++)
        {
            var type = _worldChildNodes[j].GetCurType();
            SetChunkItem(type, _worldChildNodes[j].gameObject);
        }
        
        AddAcitityItem();

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
        checkBounds.bounds = bounds;
        checkBounds.objList = curObjs;
        checkBounds.nameList = names;
        checkBounds.boundType = type;
        checkBounds.width = width;
        checkBounds.parent = parent;
        checkList.Add(checkBounds);
        checkDic.Add(type,checkList.Count - 1);
    }

    

    private void TryGetAcitityItem(GameObject curObj,GameObject parent,CheckBounds checkBounds)
    {
        List<GameObject> list = new List<GameObject>();
        TryGetChild(curObj,list);
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

    private void TryGetChild(GameObject curObj,List<GameObject> list)
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
                        // list.Add(child.gameObject);
                         
                     }
                     else
                     {
                         if (child.childCount > 0 && child.name != "Background"  && child.name != "Road Network")
                         {
                             TryGetChild(child.gameObject, list);
                         }
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
                child.transform.SetParent(checkBounds.objList[index].transform);
                
            }
           
        } 
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
            }
            else
            {   
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
        string path = savepath + "/jsonData.json";
        List<SaveJsonData> jsonData = new List<SaveJsonData>();
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        else
        {
            var data = File.ReadAllText(path);
            jsonData = JsonConvert.DeserializeObject<List<SaveJsonData>>(data);
         
        }

        
        //当存储列表为空时
        if (checkList == null || checkList.Count <= 0)
        {
            
            for (int i = 0; i < activityChunks.Count; i++)
            {
                var str = activityChunks[i].boundType.ToString();
                GameObject obj = GameObject.Find(str);
                var data = new SaveJsonData();
                bool hasSet = false;
                for (int j = 0; j < jsonData.Count; j++)
                {
                    if (jsonData[j].boundType ==activityChunks[i].boundType)
                    {
                        // data = jsonData[j];
                        // hasSet = true;
                        jsonData[j] = null;
                        break;
                    }
                }

                var chunk = new ChunkActivityNode();
                for (int j = 0; j < activityChunks.Count; j++)
                {
                    if (activityChunks[j].boundType == activityChunks[i].boundType)
                    {
                        chunk = activityChunks[j];
                    }
                }

                if (data.width <= 0)
                {
                    data.boundType = activityChunks[i].boundType;
                    data.width = chunk.width;
                    data.nameList = new List<string>();
                }
                
                if (obj != null)
                {   
                    //活动的特殊处理，多个父节点
                    if (activityChunks[i].boundType == WORLD_BOUND_TYPE.Active_obj)
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
                for (int j = 0; j < jsonData.Count; j++)
                { 
                    
                    if ( jsonData[j].boundType ==checkList[i].boundType)
                    {
                        // data = jsonData[j];
                        // hasSet = true;
                        jsonData[j] = null;
                        jsonData.RemoveAt(j);
                        break;
                    }
                }
                data.boundType = checkList[i].boundType;
                data.nameList = checkList[i].nameList;
                data.width = checkList[i].width;
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
        SaveTxt();
        //列表为空时
        if (checkList == null || checkList.Count <= 0)
        {
            for (int i = 0; i < activityChunks.Count; i++)
            {
                var str = activityChunks[i].boundType.ToString();
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
                            if (activityChunks[i].boundType == WORLD_BOUND_TYPE.Active_obj)
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
                                if (activityChunks[i].boundType == WORLD_BOUND_TYPE.Terrain_Mesh || activityChunks[i].boundType == WORLD_BOUND_TYPE.Static_Obj )
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
                string firstPath = savepath + "/" + checkList[i].boundType.ToString();
                if (!Directory.Exists(firstPath))
                    AssetDatabase.CreateFolder(savepath, checkList[i].boundType.ToString());

                if (checkList[i].boundType == WORLD_BOUND_TYPE.Active_obj)
                {
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
                            path = AssetDatabase.GenerateUniqueAssetPath(path);
                            PrefabUtility.SaveAsPrefabAsset(items[j],path);
        
                        }
                    }
                }
               
            } 
        }

       

    }
    
    #endregion
    public class SaveJsonData
    {
        public WORLD_BOUND_TYPE boundType;
        public List<string> nameList;
        public float width;
        public Dictionary<string, List<string>> activityNameList;
    }


}
[CustomPropertyDrawer(typeof(CheckWorldActivityTools.ChunkActivityNode))]
public class ChunkActivityDrawer : PropertyDrawer 
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


