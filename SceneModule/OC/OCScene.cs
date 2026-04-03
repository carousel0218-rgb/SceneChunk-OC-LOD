using BlueNoah.Math.FixedPoint;
using game_logic;
using System;
using System.Collections.Generic;
using UnityEngine;

public class OCScene : MonoBehaviour
{
    [Serializable]
    public class SceneInfo
    {
        public Transform SceneTran;
        public List<int> roadIds;
    }
    public List<SceneInfo> CheckScene;
    public Vector3 center;

    public Vector3 MAXBounds = new Vector3(2000, 50, 2000);
    public UnityEngine.Bounds mainBound;
    Tree tree;//树
    bool startEnd = false;//是否初始化完毕
    private Transform notAddTransform;
    public Camera cam;//相机
    Plane[] planes;//视椎体的6个面

    private OCItem data;
    private bool justAdd = false;
    private Dictionary<string, List<OCItem>> itemsInTree = new Dictionary<string, List<OCItem>>();
    void Start()
    {
        planes = new Plane[6];//开辟内存
        // float num = (float)Math.Pow(2,6) * CheckDis;
        //初始化场景最大分块
        UnityEngine.Bounds bounds = new(center, MAXBounds);
        //创建树
        tree = new Tree(bounds);
        justAdd = false;
        itemsInTree = new Dictionary<string, List<OCItem>>();
    }


    public void SetTree(Vector3 centerV3, Vector3 maxV3)
    {
        Vector3 pos = new Vector3(centerV3.x, 0, centerV3.z);
        // Vector3 pos2 = new Vector3(maxV3.x, centerV3.y *2, maxV3.z);
        UnityEngine.Bounds bounds = new(pos, maxV3);
        tree = new Tree(bounds);
        justAdd = true;
        cam = PlayerCamera.Instance.GetMainCamera();
        time = 0;
        startEnd = true;
        notAddTransform = null;
    }

    public void AddItem(GameObject obj)
    {
        if (obj == null) return;
        var pos = Vector3.zero;
        var list = new List<OCItem>();
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var child = obj.transform.GetChild(i);
            // var ocObject = child.GetComponent<OCObject>();
            if (child != null)
            {

                foreach (var item in child.GetComponentsInChildren<OCObject>())
                {
                    if (item == null) return;
                    pos = new Vector3(item.transform.position.x, center.y, item.transform.position.z);

                    data = new OCItem(item, pos, item.transform.eulerAngles);
                    //添加的物体默认是隐藏的，防止加载时闪现
                    item.SetIsShow(false);
                    tree.InserData(data);
                    list.Add(data);

                }
            }
        }

        if (itemsInTree.ContainsKey(obj.name))
        {
            itemsInTree[obj.name] = list;
        }
        else
        {
            itemsInTree.Add(obj.name, list);
        }
    }

    public void RemoveItem(string objName)
    {
        if (objName == null) return;
        if (itemsInTree.ContainsKey(objName))
        {
            var list = itemsInTree[objName];
            for (int i = 0; i < list.Count; i++)
            {
                tree.RemoveData(list[i]);
            }
            list.Clear();
            itemsInTree[objName] = null;
            itemsInTree.Remove(objName);
        }
    }


    private int time = 0;
    void Update()
    {
        if (startEnd == false)
        {
            if (justAdd == false)
            {
                if (notAddTransform == null)
                {
                    Save();
                }
                else
                {
                    AddOther();
                }
            }

        }
        if (startEnd && PlayerRoot.Instance.FocusedPlayer != null)//判断初始化结束后
        // if(startEdnd)//判断初始化结束后
        {
            if (PlayerRoot.Instance.FocusedPlayer.Data.Position == Vector3.zero) return;
            time += 1;

            if (time >= 10)
            {
                //给6个面赋值
                GeometryUtility.CalculateFrustumPlanes(cam, planes);
                //通过树判断是否显示
                tree.TriggerMove(planes);
                time = 0;
            }


        }
        // OnDrawGizmos();
    }

    private void Save()
    {

        if (CheckScene == null || CheckScene.Count <= 0) return;
        var pos = Vector3.zero;
        List<Transform> CheckTrans = new List<Transform> { };
        for (int i = 0; i < CheckScene.Count; i++)
        {
            var ids = CheckScene[i].roadIds;
            if (ids.Count <= 0)
            {
                CheckTrans.Add(CheckScene[i].SceneTran);
            }
            else
            {
                int count = 0;
                for (int j = 0; j < ids.Count; j++)
                {
                    // if(PlayerData.Instance.Goal.RoadID == ids[j]){
                    //    
                    //     CheckTrans.Add(CheckScene[i].SceneTran);
                    //     count += 1;
                    // }
                }
                // if(count > 0){
                //     if( CheckScene[i].SceneTran != null){
                //         CheckScene[i].SceneTran.gameObject.SetActive(true);
                //     }
                // }
            }

        }
        for (int i = 0; i < CheckTrans.Count; i++)
        {
            if (CheckTrans[i] == null) continue;
            if (CheckTrans[i].GetComponentsInChildren<OCObject>().Length <= 0)
            {
                notAddTransform = CheckTrans[i];
            }
            foreach (var item in CheckTrans[i].GetComponentsInChildren<OCObject>())
            {
                if (item == null) return;
                pos = new Vector3(item.transform.position.x, center.y, item.transform.position.z);
                data = new OCItem(item, pos, item.transform.eulerAngles);
                tree.InserData(data);
            }
        }

        if (notAddTransform == null)
        {
            //将预制体数据存入树
            cam = PlayerCamera.Instance.GetMainCamera();
            time = 0;
            startEnd = true;
        }

    }

    private void AddOther()
    {
        if (notAddTransform == null) return;
        if (notAddTransform.GetComponentsInChildren<OCObject>().Length > 0)
        {
            var pos = Vector3.zero;
            foreach (var item in notAddTransform.GetComponentsInChildren<OCObject>())
            {
                if (item == null) return;
                pos = new Vector3(item.transform.position.x, center.y, item.transform.position.z);
                data = new OCItem(item, pos, item.transform.eulerAngles);
                tree.InserData(data);
            }
        }
        else
        {
            return;
        }
        cam = PlayerCamera.Instance.GetMainCamera();
        time = 0;
        startEnd = true;
        notAddTransform = null;
    }
    private void OnDrawGizmos()
    {
        if (startEnd)//判断初始化结束后
        {
            //通过树绘制包围盒
            tree.DrawBound();
        }
        else
        {
            Gizmos.DrawWireCube(mainBound.center, mainBound.size);
        }
    }
}