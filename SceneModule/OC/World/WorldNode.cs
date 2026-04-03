using UnityEngine;

/// <summary>
/// 分隔地图物体的类型
/// </summary>
public enum WORLD_BOUND_TYPE
{
    /// <summary>
    /// 地形
    /// </summary>
    Terrain_Mesh,
    /// <summary>
    /// 静态物体
    /// </summary>
    Static_Obj,
    Big_Obj,
    Middle_Obj,
    Small_Obj,
    /// <summary>
    /// 活动物体
    /// </summary>
    Active_obj,
    /// <summary>
    /// 道路或者基础物体
    /// </summary>
    RoadOrBase,
}

/// <summary>
/// 大地图物体的基本类型
/// </summary>
public class WorldChildNode :MonoBehaviour
{   
  
    public WORLD_BOUND_TYPE WorldBoundType;
    public WORLD_BOUND_TYPE GetCurType()
    {
        return WorldBoundType;
    }

    public void SetType(WORLD_BOUND_TYPE type)
    {
        WorldBoundType = type;
    }
}

 

