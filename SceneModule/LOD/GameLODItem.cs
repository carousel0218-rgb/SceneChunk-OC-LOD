using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using YooAsset;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameLODItem : IDisposable
{
    public int LODIndex { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool IsVisible { get; private set; }
    public GameObject Instance { get; private set; }
    
    private GameLODLevel m_LODLevel;
    private GameLODGroup m_Owner;
    private AssetHandle m_AssetHandle;
    private bool m_IsLoading = false;
    private bool m_IsPreloading = false;
    private bool m_IsDisposed = false;
    
    public GameLODItem(GameLODLevel lodLevel, int lodIndex, GameLODGroup owner)
    {
        m_LODLevel = lodLevel;
        LODIndex = lodIndex;
        m_Owner = owner;
    }
    
    public async UniTask<bool> LoadAsync()
    {
        if (m_IsDisposed || m_IsLoading || IsLoaded)
            return IsLoaded;
            
        m_IsLoading = true;
        
        try
        {
            // 如果已经预加载了资源，直接使用
            if (m_AssetHandle != null && m_AssetHandle.IsValid)
            {
                return await CreateInstance();
            }
            
            // 异步加载资源
            m_AssetHandle = YooAssets.LoadAssetAsync<GameObject>(m_LODLevel.assetAddress);
            await m_AssetHandle.Task;
            
            if (m_AssetHandle.Status == EOperationStatus.Succeed)
            {
                return await CreateInstance();
            }
            else
            {
                Debug.LogError($"Failed to load LOD asset: {m_LODLevel.assetAddress}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading LOD {LODIndex}: {e.Message}");
            return false;
        }
        finally
        {
            m_IsLoading = false;
        }
    }
    
    public async UniTask<bool> PreloadAsync()
    {
        if (m_IsDisposed || m_IsPreloading || m_AssetHandle != null)
            return m_AssetHandle?.IsValid ?? false;
            
        m_IsPreloading = true;
        
        try
        {
            // 只预加载资源，不创建实例
            m_AssetHandle = YooAssets.LoadAssetAsync<GameObject>(m_LODLevel.assetAddress);
            await m_AssetHandle.Task;
            
            return m_AssetHandle.Status == EOperationStatus.Succeed;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error preloading LOD {LODIndex}: {e.Message}");
            return false;
        }
        finally
        {
            m_IsPreloading = false;
        }
    }
    
    private async UniTask<bool> CreateInstance()
    {
        if (Instance != null)
            return true;
            
        try
        {
            var prefab = m_AssetHandle.AssetObject as GameObject;
            if (prefab == null)
            {
                Debug.LogError($"Asset is not a GameObject: {m_LODLevel.assetAddress}");
                return false;
            }
            
            // 从对象池获取实例
            Instance = GameLODObjectPool.Instance.Spawn(m_LODLevel.assetAddress, prefab, m_Owner.LODContainer);
            
            if (Instance != null)
            {
                Instance.name = $"{prefab.name}_LOD{LODIndex}";
                Instance.SetActive(false); // 初始隐藏
                IsLoaded = true;
                return true;
            }
            else
            {
                Debug.LogError($"Failed to create LOD instance: {m_LODLevel.assetAddress}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating LOD instance {LODIndex}: {e.Message}");
            return false;
        }
    }
    
    public void SetVisible(bool visible)
    {
        if (Instance != null && IsLoaded)
        {
            Instance.SetActive(visible);
            IsVisible = visible;
        }
    }
    
    public void Release()
    {
        if (m_IsDisposed)
            return;
            
        // 回收实例到对象池
        if (Instance != null)
        {
            GameLODObjectPool.Instance.Recycle(Instance);
            Instance = null;
        }
        
        IsLoaded = false;
        IsVisible = false;
        
        // 延迟释放资源句柄
        if (m_AssetHandle != null && m_AssetHandle.IsValid)
        {
            // 不立即释放，让对象池管理
            // m_AssetHandle.Release();
            // m_AssetHandle = null;
        }
    }
    
    public void Dispose()
    {
        if (m_IsDisposed)
            return;
            
        m_IsDisposed = true;
        
        // 立即释放所有资源
        if (Instance != null)
        {
            UnityEngine.Object.Destroy(Instance);
            Instance = null;
        }
        
        if (m_AssetHandle != null && m_AssetHandle.IsValid)
        {
            m_AssetHandle.Release();
            m_AssetHandle = null;
        }
        
        IsLoaded = false;
        IsVisible = false;
    }
    
    // 获取内存占用估算
    public float GetMemoryUsage()
    {
        if (!IsLoaded || Instance == null)
            return 0f;
            
        float totalSize = 0f;
        
        // 计算网格内存
        var meshFilters = Instance.GetComponentsInChildren<MeshFilter>();
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null)
            {
                totalSize += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(meshFilter.sharedMesh) / 1024f / 1024f; // MB
            }
        }
        
        // 计算材质内存
        var renderers = Instance.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterials != null)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        totalSize += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(material) / 1024f / 1024f; // MB
                    }
                }
            }
        }
        
        return totalSize;
    }
    
    
    

} 