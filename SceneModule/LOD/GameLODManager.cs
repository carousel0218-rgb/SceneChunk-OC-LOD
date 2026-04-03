using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameSetting;

public class GameLODManager : MonoBehaviour
{
    private static GameLODManager s_Instance;
    public static GameLODManager Instance 
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
    
    // LOD组管理
    private List<GameLODGroup> m_LODGroups = new List<GameLODGroup>();
    private float m_LastUpdateTime;
    private float m_LastGCTime;
    
    // 性能监控
    private int m_TotalLODGroups = 0;
    private int m_ActiveLODGroups = 0;
    private float m_MemoryUsage = 0f;
    private int m_FrameUpdates = 0;
    
    // 设置缓存
    private GameSetting.LODSettings m_LODSettings;
    
    // 事件
    public static event Action<GameLODStats> OnPerformanceUpdate;
    public static event Action<string> OnError;
    
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
        InitializeObjectPool();
    }
    
    private static void CreateInstance()
    {
        GameObject go = new GameObject("GameLODManager");
        s_Instance = go.AddComponent<GameLODManager>();
    }
    
    private void InitializeSettings()
    {
        m_LODSettings = new LODSettings();
        if (m_LODSettings == null)
        {
            Debug.LogError("GameLODManager: 无法获取LOD设置，请检查GameSettings配置！");
        }
    }
    
    private void InitializeObjectPool()
    {
        // 初始化LOD对象池
        if (GameLODObjectPool.Instance == null)
        {
            GameObject poolGO = new GameObject("GameLODObjectPool");
            poolGO.transform.SetParent(transform);
            poolGO.AddComponent<GameLODObjectPool>();
        }
    }
    
    void Update()
    {
        if (m_LODSettings == null)
            return;
            
        float currentTime = Time.time;
        
        // 全局更新间隔检查
        if (currentTime - m_LastUpdateTime < m_LODSettings.GlobalUpdateInterval)
            return;
            
        m_LastUpdateTime = currentTime;
        
        // 批量更新LOD组
        UpdateLODGroups();
        
        // 性能监控
        if (m_LODSettings.EnablePerformanceMonitoring)
        {
            UpdatePerformanceStats();
        }
        
        // 自动垃圾回收
        if (m_LODSettings.EnableAutoGC && currentTime - m_LastGCTime > m_LODSettings.GCInterval)
        {
            PerformGarbageCollection();
            m_LastGCTime = currentTime;
        }
    }
    
    private void UpdateLODGroups()
    {
        if (m_LODGroups.Count == 0)
            return;
            
        // 限制每帧更新数量
        int updatesThisFrame = 0;
        int totalGroups = m_LODGroups.Count;
        
        for (int i = 0; i < totalGroups && updatesThisFrame < m_LODSettings.MaxLODUpdatesPerFrame; i++)
        {
            if (m_LODGroups[i] != null && m_LODGroups[i].gameObject.activeInHierarchy)
            {
                m_LODGroups[i].UpdateLOD();
                updatesThisFrame++;
            }
        }
        
        m_FrameUpdates = updatesThisFrame;
    }
    
    private void UpdatePerformanceStats()
    {
        m_TotalLODGroups = m_LODGroups.Count;
        m_ActiveLODGroups = 0;
        m_MemoryUsage = 0f;
        
        foreach (var group in m_LODGroups)
        {
            if (group != null && group.gameObject.activeInHierarchy)
            {
                m_ActiveLODGroups++;
                
                var stats = group.GetPerformanceStats();
                // 这里可以累积更详细的统计信息
            }
        }
        
        // 触发性能更新事件
        var globalStats = new GameLODStats
        {
            currentLODIndex = -1, // 全局统计
            isTransitioning = false,
            lastDistance = 0f,
            loadedLODCount = m_ActiveLODGroups
        };
        
        OnPerformanceUpdate?.Invoke(globalStats);
    }
    
    private void PerformGarbageCollection()
    {
        // 清理对象池
        GameLODObjectPool.Instance?.ClearUnusedPools();
        
        // 强制垃圾回收
        GC.Collect();
        Resources.UnloadUnusedAssets();
        
        Debug.Log($"GameLOD: 执行垃圾回收 - 活跃组数: {m_ActiveLODGroups}, 总组数: {m_TotalLODGroups}");
    }
    
    public void RegisterLODGroup(GameLODGroup group)
    {
        if (group == null || m_LODGroups.Contains(group))
            return;
            
        m_LODGroups.Add(group);
        
        // 监听LOD组事件
        group.OnLODChanged += OnLODGroupChanged;
        group.OnError += OnLODGroupError;
        
        Debug.Log($"GameLOD: 注册LOD组 {group.name}, 总数: {m_LODGroups.Count}");
    }
    
    public void UnregisterLODGroup(GameLODGroup group)
    {
        if (group == null || !m_LODGroups.Contains(group))
            return;
            
        m_LODGroups.Remove(group);
        
        // 取消监听LOD组事件
        group.OnLODChanged -= OnLODGroupChanged;
        group.OnError -= OnLODGroupError;
        
        Debug.Log($"GameLOD: 注销LOD组 {group.name}, 总数: {m_LODGroups.Count}");
    }
    
    private void OnLODGroupChanged(int oldIndex, int newIndex)
    {
        Debug.Log($"GameLOD: LOD切换 {oldIndex} -> {newIndex}");
    }
    
    private void OnLODGroupError(string error)
    {
        Debug.LogError($"GameLOD错误: {error}");
        OnError?.Invoke(error);
    }
    
    
    public void ForceUpdateAllLODs()
    {
        foreach (var group in m_LODGroups)
        {
            if (group != null && group.gameObject.activeInHierarchy)
            {
                group.UpdateLOD();
            }
        }
    }
    
    public void PauseAllLODs()
    {
        foreach (var group in m_LODGroups)
        {
            if (group != null)
            {
                group.enabled = false;
            }
        }
    }
    
    public void ResumeAllLODs()
    {
        foreach (var group in m_LODGroups)
        {
            if (group != null)
            {
                group.enabled = true;
            }
        }
    }
    
    public GameLODGlobalStats GetGlobalStats()
    {
        return new GameLODGlobalStats
        {
            totalLODGroups = m_TotalLODGroups,
            activeLODGroups = m_ActiveLODGroups,
            memoryUsage = m_MemoryUsage,
            frameUpdates = m_FrameUpdates,
            updateInterval = m_LODSettings?.GlobalUpdateInterval ?? 0.1f
        };
    }
    
    // 编辑器调试功能
    [ContextMenu("强制垃圾回收")]
    public void ForceGarbageCollection()
    {
        PerformGarbageCollection();
    }
    
    [ContextMenu("显示性能统计")]
    public void ShowPerformanceStats()
    {
        var stats = GetGlobalStats();
        Debug.Log($"LOD性能统计:\n" +
                  $"总LOD组数: {stats.totalLODGroups}\n" +
                  $"活跃组数: {stats.activeLODGroups}\n" +
                  $"内存使用: {stats.memoryUsage:F2} MB\n" +
                  $"帧更新数: {stats.frameUpdates}\n" +
                  $"更新间隔: {stats.updateInterval:F3}s");
    }
    
    [ContextMenu("清理所有LOD")]
    public void ClearAllLODs()
    {
        foreach (var group in m_LODGroups)
        {
            if (group != null)
            {
                // 这里可以添加清理单个LOD组的逻辑
            }
        }
        
        GameLODObjectPool.Instance?.Clear();
    }
    
    void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }
}

[Serializable]
public class GameLODGlobalStats
{
    public int totalLODGroups;
    public int activeLODGroups;
    public float memoryUsage;
    public int frameUpdates;
    public float updateInterval;
} 