using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using System;
using GameSetting;

public class GameLODObjectPool : MonoBehaviour
{
    private static GameLODObjectPool s_Instance;
    public static GameLODObjectPool Instance
    {
        get
        {
            if (s_Instance == null)
            {
                CreateInstance();
            }
            return s_Instance;
        }
    }
    
    [System.Serializable]
    private class LODPoolItem
    {
        public string Address;
        public Queue<GameObject> IdleObjects;
        public HashSet<GameObject> ActiveObjects;
        public AssetHandle AssetHandle;
        public float LastUsedTime;
        public int MaxPoolSize;
        public int TotalCount => IdleObjects.Count + ActiveObjects.Count;
        
        public LODPoolItem(string address, int maxSize = 10)
        {
            Address = address;
            IdleObjects = new Queue<GameObject>();
            ActiveObjects = new HashSet<GameObject>();
            LastUsedTime = Time.time;
            MaxPoolSize = maxSize;
        }
        
        public void ReleaseAsset()
        {
            AssetHandle?.Release();
            AssetHandle = null;
        }
        
        public bool IsExpired(float expireTime)
        {
            return Time.time - LastUsedTime > expireTime && ActiveObjects.Count == 0;
        }
        
        public void UpdateUsedTime()
        {
            LastUsedTime = Time.time;
        }
    }
    
    private readonly Dictionary<string, LODPoolItem> m_PoolDictionary = new Dictionary<string, LODPoolItem>();
    private Transform m_PoolRoot;
    private float m_LastCleanupTime;
    
    // 设置缓存
    private GameSetting.LODSettings m_LODSettings;
    
    // 统计信息
    public int TotalActiveLODs
    {
        get
        {
            int total = 0;
            foreach (var pool in m_PoolDictionary.Values)
            {
                total += pool.ActiveObjects.Count;
            }
            return total;
        }
    }
    
    public int TotalIdleLODs
    {
        get
        {
            int total = 0;
            foreach (var pool in m_PoolDictionary.Values)
            {
                total += pool.IdleObjects.Count;
            }
            return total;
        }
    }
    
    void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        s_Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeSettings();
        InitializePoolRoot();
    }
    
    private static void CreateInstance()
    {
        GameObject go = new GameObject("GameLODObjectPool");
        s_Instance = go.AddComponent<GameLODObjectPool>();
    }
    
    private void InitializeSettings()
    {
        m_LODSettings = new LODSettings();
        if (m_LODSettings == null)
        {
            Debug.LogError("GameLODObjectPool: 无法获取LOD设置，请检查GameSettings配置！");
        }
    }
    
    private void InitializePoolRoot()
    {
        if (m_PoolRoot == null)
        {
            GameObject poolRoot = new GameObject("LOD Pool Root");
            poolRoot.transform.SetParent(transform);
            poolRoot.transform.localPosition = Vector3.zero;
            m_PoolRoot = poolRoot.transform;
        }
    }
    
    void Update()
    {
        if (m_LODSettings == null)
            return;
            
        // 定期清理过期的对象池
        if (Time.time - m_LastCleanupTime > 60f) // 每分钟检查一次
        {
            CleanupExpiredPools();
            m_LastCleanupTime = Time.time;
        }
    }
    
    public GameObject Spawn(string address, GameObject prefab, Transform parent = null)
    {
        if (string.IsNullOrEmpty(address) || prefab == null)
        {
            Debug.LogError("LOD地址或预制体为空！");
            return null;
        }
        
        LODPoolItem poolItem;
        if (!m_PoolDictionary.TryGetValue(address, out poolItem))
        {
            int maxSize = m_LODSettings?.MaxPoolSize ?? 20;
            poolItem = new LODPoolItem(address, maxSize);
            m_PoolDictionary.Add(address, poolItem);
        }
        
        poolItem.UpdateUsedTime();
        
        GameObject instance = null;
        
        // 尝试从空闲队列中获取对象
        while (poolItem.IdleObjects.Count > 0)
        {
            instance = poolItem.IdleObjects.Dequeue();
            if (instance != null)
            {
                break;
            }
        }
        
        // 如果没有可用对象，创建新对象
        if (instance == null)
        {
            instance = CreateNewInstance(prefab, address);
        }
        
        if (instance != null)
        {
            // 设置父对象
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }
            else
            {
                instance.transform.SetParent(m_PoolRoot, false);
            }
            
            // 添加到活跃对象集合
            poolItem.ActiveObjects.Add(instance);
            instance.SetActive(true);
        }
        
        return instance;
    }
    
    public void Recycle(GameObject obj)
    {
        if (obj == null) return;
        
        // 查找对象所属的池
        foreach (var poolItem in m_PoolDictionary.Values)
        {
            if (poolItem.ActiveObjects.Remove(obj))
            {
                if (obj != null)
                {
                    // 检查池容量
                    if (poolItem.IdleObjects.Count < poolItem.MaxPoolSize)
                    {
                        // 移动到池根节点
                        obj.transform.SetParent(m_PoolRoot);
                        obj.transform.localPosition = Vector3.zero;
                        obj.transform.localRotation = Quaternion.identity;
                        obj.transform.localScale = Vector3.one;
                        
                        obj.SetActive(false);
                        poolItem.IdleObjects.Enqueue(obj);
                        poolItem.UpdateUsedTime();
                    }
                    else
                    {
                        // 池已满，直接销毁
                        Destroy(obj);
                    }
                }
                else
                {
                    Destroy(obj);
                }
                return;
            }
        }
        
        // 如果没有找到对应的池，直接销毁
        Destroy(obj);
    }
    
    private GameObject CreateNewInstance(GameObject prefab, string address)
    {
        if (prefab == null)
        {
            Debug.LogError("预制体为空！");
            return null;
        }
        
        try
        {
            GameObject instance = Instantiate(prefab, m_PoolRoot);
            instance.name = $"{prefab.name}_LOD_Pool";
            
            // 添加LOD池标识组件
            var poolTag = instance.GetComponent<GameLODPoolTag>();
            if (poolTag == null)
            {
                poolTag = instance.AddComponent<GameLODPoolTag>();
            }
            poolTag.PoolAddress = address;
            
            return instance;
        }
        catch (Exception e)
        {
            Debug.LogError($"创建LOD实例失败: {e.Message}");
            return null;
        }
    }
    
    
    private void CleanupExpiredPools()
    {
        var expiredPools = new List<string>();
        float expireTime = m_LODSettings?.PoolExpireTime ?? 300f;
        
        foreach (var kvp in m_PoolDictionary)
        {
            if (kvp.Value.IsExpired(expireTime))
            {
                expiredPools.Add(kvp.Key);
            }
        }
        
        foreach (var address in expiredPools)
        {
            if (m_PoolDictionary.TryGetValue(address, out var poolItem))
            {
                // 销毁所有空闲对象
                while (poolItem.IdleObjects.Count > 0)
                {
                    var obj = poolItem.IdleObjects.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                
                // 释放资源
                poolItem.ReleaseAsset();
                
                // 从字典中移除
                m_PoolDictionary.Remove(address);
                
                Debug.Log($"GameLOD: 清理过期池 {address}");
            }
        }
    }
    
    public void ClearUnusedPools(float unusedThreshold = 30f)
    {
        var unusedPools = new List<string>();
        
        foreach (var kvp in m_PoolDictionary)
        {
            if (kvp.Value.ActiveObjects.Count == 0 && 
                Time.time - kvp.Value.LastUsedTime > unusedThreshold)
            {
                unusedPools.Add(kvp.Key);
            }
        }
        
        foreach (var address in unusedPools)
        {
            if (m_PoolDictionary.TryGetValue(address, out var poolItem))
            {
                // 销毁所有空闲对象
                while (poolItem.IdleObjects.Count > 0)
                {
                    var obj = poolItem.IdleObjects.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                
                poolItem.ReleaseAsset();
                m_PoolDictionary.Remove(address);
                
                Debug.Log($"GameLOD: 清理未使用池 {address}");
            }
        }
    }
    
    public void Clear()
    {
        foreach (var poolItem in m_PoolDictionary.Values)
        {
            // 销毁所有活跃对象
            foreach (var obj in poolItem.ActiveObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            // 销毁所有空闲对象
            while (poolItem.IdleObjects.Count > 0)
            {
                var obj = poolItem.IdleObjects.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            poolItem.ReleaseAsset();
        }
        
        m_PoolDictionary.Clear();
        Debug.Log("GameLOD: 清理所有对象池");
    }
    
    public void PrewarmPool(string address, GameObject prefab, int count)
    {
        if (string.IsNullOrEmpty(address) || prefab == null || count <= 0)
            return;
            
        LODPoolItem poolItem;
        if (!m_PoolDictionary.TryGetValue(address, out poolItem))
        {
            int maxSize = m_LODSettings?.MaxPoolSize ?? 20;
            poolItem = new LODPoolItem(address, maxSize);
            m_PoolDictionary.Add(address, poolItem);
        }
        
        // 预热指定数量的对象
        for (int i = 0; i < count && poolItem.IdleObjects.Count < poolItem.MaxPoolSize; i++)
        {
            GameObject obj = CreateNewInstance(prefab, address);
            if (obj != null)
            {
                obj.SetActive(false);
                poolItem.IdleObjects.Enqueue(obj);
            }
        }
        
        Debug.Log($"GameLOD: 预热池 {address}，创建了 {count} 个对象");
    }
    
    public GameLODPoolStats GetPoolStats()
    {
        var stats = new GameLODPoolStats();
        stats.totalPools = m_PoolDictionary.Count;
        stats.totalActiveObjects = TotalActiveLODs;
        stats.totalIdleObjects = TotalIdleLODs;
        stats.poolDetails = new Dictionary<string, GameLODPoolDetail>();
        
        foreach (var kvp in m_PoolDictionary)
        {
            var detail = new GameLODPoolDetail
            {
                address = kvp.Key,
                activeCount = kvp.Value.ActiveObjects.Count,
                idleCount = kvp.Value.IdleObjects.Count,
                maxSize = kvp.Value.MaxPoolSize,
                lastUsedTime = kvp.Value.LastUsedTime
            };
            stats.poolDetails.Add(kvp.Key, detail);
        }
        
        return stats;
    }
    
    // 编辑器调试功能
    [ContextMenu("显示池统计")]
    public void ShowPoolStats()
    {
        var stats = GetPoolStats();
        Debug.Log($"LOD对象池统计:\n" +
                  $"总池数: {stats.totalPools}\n" +
                  $"活跃对象: {stats.totalActiveObjects}\n" +
                  $"空闲对象: {stats.totalIdleObjects}");
                  
        foreach (var detail in stats.poolDetails.Values)
        {
            Debug.Log($"池 {detail.address}: 活跃={detail.activeCount}, 空闲={detail.idleCount}, 最大={detail.maxSize}");
        }
    }
    
    void OnDestroy()
    {
        Clear();
        
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }
}



// 池统计数据
[Serializable]
public class GameLODPoolStats
{
    public int totalPools;
    public int totalActiveObjects;
    public int totalIdleObjects;
    public Dictionary<string, GameLODPoolDetail> poolDetails;
}

[Serializable]
public class GameLODPoolDetail
{
    public string address;
    public int activeCount;
    public int idleCount;
    public int maxSize;
    public float lastUsedTime;
} 