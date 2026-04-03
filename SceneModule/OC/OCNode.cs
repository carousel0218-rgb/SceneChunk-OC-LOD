using System.Collections.Generic;
using game_logic;
using UnityEngine;
 
public class OCNode
{
    public Bounds bound;
    public int myDepth;//当前层数
    
    public Tree tree;
    public List<OCItem> datas= new List<OCItem>();//数据
    public OCNode[] childs;//子节点
    public Vector2[] bif = new Vector2[]
    {
        new Vector2(-1,1),
        new Vector2(1,1),
        new Vector2(-1,-1),
        new Vector2(1,-1),
    };

  
    private bool IsAABB = false;

    private Vector3 curCameraPos = Vector3.zero;
    public OCNode(Bounds bound, int myDepth, Tree tree)
    {
        this.bound = bound;
        this.myDepth = myDepth;
        this.tree = tree;
    }
    public void InserData(OCItem data)
    {
        //层级没到上限 且 没有子节点 可以创建子节点
        if(myDepth<tree.maxDepth&&childs==null)
        {   
            creatChild();
        }
        if(childs!=null)
        {
            for (int i = 0; i < childs.Length; i++)
            {
                //判断数据的位置是否归属于该子节点的区域
                if (childs[i].bound.Contains(data.pos))
                {
                    //继续去下一层查找
                    childs[i].InserData(data);
                    break;
                }
            }
        }
        else
        {   
            
            datas.Add(data);
        }
    }
    
    //移除数据
    public void RemoveData(OCItem data)
    {
        if(childs!=null)
        {
            for (int i = 0; i < childs.Length; i++)
            {
                //判断数据的位置是否归属于该子节点的区域
                if (childs[i].bound.Contains(data.pos))
                {
                    //继续去下一层查找
                    childs[i].RemoveData(data);
                    break;
                }
            }
        }
        else
        {   
            datas.Remove(data);
        }
    }
 
    private void creatChild()
    {
        childs = new OCNode[tree.maxChildCount];
        for (int i = 0; i < tree.maxChildCount; i++)
        {
            //计算相对坐标
            Vector3 center= new Vector3(bif[i].x * bound.size.x / 4, 0, bif[i].y*bound.size.z/4);
            //计算大小
            Vector3 size = new Vector3(bound.size.x / 2, bound.size.y, bound.size.z / 2);
            //设置矩阵
            Bounds childbound = new Bounds(center + bound.center, size);
            //给子节点赋值
            childs[i] = new OCNode(childbound, myDepth + 1, tree);
        }
    }
    public void DrawBound()
    {
        //有数据画蓝色框框
        if (datas.Count!=0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(bound.center, bound.size - Vector3.one * 0.1f);
        }
        else//没数据画绿色框框
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bound.center, bound.size - Vector3.one * 0.1f);
        }
        if(childs!=null)
        {
            for (int i = 0; i < childs.Length; i++)
            {
                childs[i].DrawBound();
            }
        }
    }
    public void TriggerMove(Plane[] planes)
    {
        //有子物体让子物体去判断是否重叠
        if(childs!=null)
        {
            for (int i = 0; i <  childs.Length; i++)
            {
                childs[i].TriggerMove(planes);
            }
        }
        if(datas.Count > 0)
        {
            var pos = PlayerCamera.Instance.GetCameraPosition();
            if(curCameraPos == pos) return;
             curCameraPos = pos;
            //判断矩阵与6个面是否重叠 
            IsAABB = GeometryUtility.TestPlanesAABB(planes, bound);
            for (int i = 0; i < datas.Count; i++)
            {
                if(!IsAABB){
                    // if(datas[i].prefab.gameObject.activeSelf){
                        //超过一定距离才会隐藏
                        datas[i].UpdateVisible(curCameraPos);
                    // }
                }   
                else{
                    datas[i].CheckNearShow(curCameraPos);
                }
                
            
            }
        }
           
       
    }

   

#region  视锥检测
    // public enum InsideResult//视锥体检测的结果
    // {
    //     Out,//外侧
    //     In,//包含在内（指整个包围盒都在相机视锥体内）
    //     Partial//部分包含
    // };
    //
    //
    // private float4 m_FrustumPlanes;
    //
    // /// <summary>
    // /// 方形包围盒剔除
    // /// </summary>
    // /// <param name="center">盒子中心</param>
    // /// <param name="extents">外延尺寸（size的一半）</param>
    // public InsideResult Inside(Plane[] planes ,Vector3 center, Vector3 extents)
    // {
    //     int length = planes.Length;
    //     bool all_in = true;
    //     for (int i = 0; i < length; i++)
    //     {
    //         var plane = planes[i];
    //         m_FrustumPlanes = new float4(plane.normal,plane.distance);
    //         float dist = math.dot(m_FrustumPlanes.xyz, center) + m_FrustumPlanes.w;
    //         float radius = math.dot(extents, math.abs(m_FrustumPlanes.xyz));
    //         if (dist <= -radius)
    //             return InsideResult.Out;
    //
    //         all_in &= dist > radius;
    //     }
    //
    //     return all_in ? InsideResult.In : InsideResult.Partial;
    // }
#endregion
}