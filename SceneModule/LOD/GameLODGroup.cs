using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameSetting;
using Object = UnityEngine.Object;

[Serializable]
public class GameLODLevel
{
    [Header("LOD配置")] public string name = "LOD";
    public string assetAddress = "";
    public float distance = 50f;
}

public class GameLODGroup : MonoBehaviour
{
    [Header("LOD层级配置")] public GameLODLevel[] lodLevels = new GameLODLevel[0];

    // 运行时状态
    private int m_CurrentLODIndex = -1;
    private int m_TargetLODIndex = -1;
    private float m_LastUpdateTime;
    private float m_LastDistance = -1;
    private bool m_IsTransitioning = false;

    // 实例管理
    private Dictionary<int, GameLODItem> m_LODItems = new Dictionary<int, GameLODItem>();
    private Camera m_MainCamera;
    private Vector3 m_LastCameraPosition;

    // 设置缓存
    private LODSettings m_LODSettings;

    // 事件
    public event Action<int, int> OnLODChanged; // (oldIndex, newIndex)
    public event Action<string> OnError;

    // 属性
    public Transform ReferencePoint => transform;
    public Transform LODContainer => transform;

    void Awake()
    {
        InitializeComponents();
        InitializeLODItems();
    }

    void Start()
    {
        Debug.Log($"[LOD] Start: m_MainCamera={m_MainCamera}, m_LODSettings={m_LODSettings}");
        // 注册到全局管理器
        GameLODManager.Instance.RegisterLODGroup(this);

        // 初始LOD检测
        UpdateLOD();
    }

    void OnDestroy()
    {
        // 从全局管理器注销
        if (GameLODManager.Instance != null)
        {
            GameLODManager.Instance.UnregisterLODGroup(this);
        }

        // 清理所有LOD项
        foreach (var item in m_LODItems.Values)
        {
            item.Dispose();
        }

        m_LODItems.Clear();
    }

    private void InitializeComponents()
    {
        m_MainCamera = Camera.main ?? FindObjectOfType<Camera>();

        // 获取LOD设置
        m_LODSettings = new  LODSettings();
        if (m_LODSettings == null)
        {
            Debug.LogError("GameLOD: 无法获取LOD设置，请检查GameSettings配置！");
        }
    }

    private void InitializeLODItems()
    {
        m_LODItems.Clear();

        for (int i = 0; i < lodLevels.Length; i++)
        {
            var lodLevel = lodLevels[i];
            var lodItem = new GameLODItem(lodLevel, i, this);
            m_LODItems.Add(i, lodItem);
        }
    }

    public void UpdateLOD()
    {
        if (m_MainCamera == null || lodLevels.Length == 0 || m_LODSettings == null)
            return;

        // 检查更新间隔
        float currentTime = Time.time;
        if (currentTime - m_LastUpdateTime < m_LODSettings.UpdateInterval)
            return;

        m_LastUpdateTime = currentTime;

        // 获取相机位置
        Vector3 cameraPosition = m_MainCamera.transform.position;
        Vector3 referencePosition = ReferencePoint.position;

        // 计算距离
        float distance = m_LODSettings.UseSquaredDistance
            ? Vector3.SqrMagnitude(cameraPosition - referencePosition)
            : Vector3.Distance(cameraPosition, referencePosition);

        // 应用滞后系数，防止抖动
        if (Mathf.Abs(distance - m_LastDistance) < m_LODSettings.Lag)
            return;

        m_LastDistance = distance;
        m_LastCameraPosition = cameraPosition;

        // 确定目标LOD
        int targetLODIndex = GetTargetLODIndex(distance);

        // 检查是否需要切换
        if (targetLODIndex != m_CurrentLODIndex && !m_IsTransitioning)
        {
            SwitchLOD(targetLODIndex).Forget();
        }
    }

    private int GetTargetLODIndex(float distance)
    {
        if (lodLevels.Length == 0)
            return -1;

        // 带回滞区间的LOD切换逻辑
        int current = m_CurrentLODIndex;
        if (current < 0) current = 0; // 初始情况
        float hysteresis = m_LODSettings != null ? m_LODSettings.Hysteresis : 1f;

        // 切到更低精度（更高LOD索引）
        if (current < lodLevels.Length - 1)
        {
            float upThreshold = m_LODSettings.UseSquaredDistance
                ? (lodLevels[current].distance + hysteresis) * (lodLevels[current].distance + hysteresis)
                : lodLevels[current].distance + hysteresis;
            if (distance > upThreshold)
                return current + 1;
        }

        // 切到更高精度（更低LOD索引）
        if (current > 0)
        {
            float downThreshold = m_LODSettings.UseSquaredDistance
                ? (lodLevels[current].distance - hysteresis) * (lodLevels[current].distance - hysteresis)
                : lodLevels[current].distance - hysteresis;
            if (distance < downThreshold)
                return current - 1;
        }

        // 否则保持当前LOD
        return current;
    }

    private async UniTask SwitchLOD(int targetIndex)
    {
        if (m_IsTransitioning)
            return;

        m_IsTransitioning = true;
        int oldIndex = m_CurrentLODIndex;

        try
        {
            // 预加载下一级LOD（如果启用）
            if (m_LODSettings.PreloadNext && targetIndex + 1 < lodLevels.Length)
            {
                if (m_LODItems.TryGetValue(targetIndex + 1, out var nextItem))
                {
                     nextItem.PreloadAsync().Forget();
                }
            }

            // 加载目标LOD
            if (targetIndex >= 0 && m_LODItems.TryGetValue(targetIndex, out var targetItem))
            {
                bool success = await targetItem.LoadAsync();
                if (success)
                {
                    m_CurrentLODIndex = targetIndex;

                    // 隐藏旧LOD
                    if (oldIndex >= 0 && m_LODItems.TryGetValue(oldIndex, out var oldItem))
                    {
                        oldItem.SetVisible(false);

                        // 延迟释放旧LOD
                        if (m_LODSettings.AutoRelease)
                        {
                            DelayedRelease(oldItem, m_LODSettings.ReleaseDelay).Forget();
                        }
                    }

                    // 显示新LOD
                    targetItem.SetVisible(true);

                    // 触发事件
                    OnLODChanged?.Invoke(oldIndex, targetIndex);
                }
                else
                {
                    OnError?.Invoke($"Failed to load LOD {targetIndex}");
                }
            }
            else
            {
                // 隐藏所有LOD
                foreach (var item in m_LODItems.Values)
                {
                    item.SetVisible(false);
                }

                m_CurrentLODIndex = -1;
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke($"LOD switch error: {e.Message}");
        }
        finally
        {
            m_IsTransitioning = false;
        }
    }

    private async UniTask DelayedRelease(GameLODItem item, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        // 确保该LOD不是当前活动的
        if (item.LODIndex != m_CurrentLODIndex)
        {
            item.Release();
        }
    }

    public async void ForceLoadLOD(int index)
    {
        if (index >= 0 && index < lodLevels.Length && m_LODItems.TryGetValue(index, out var item))
        {
            // 隐藏当前LOD
            if (m_CurrentLODIndex >= 0 && m_LODItems.TryGetValue(m_CurrentLODIndex, out var currentItem))
            {
                currentItem.SetVisible(false);
            }

            // 加载目标LOD
            bool success = await item.LoadAsync();
            if (success)
            {
                item.SetVisible(true);
                m_CurrentLODIndex = index;
                Debug.Log($"强制加载LOD {index} 成功");
            }
            else
            {
                Debug.LogError($"强制加载LOD {index} 失败");
            }
        }
    }

    // 性能统计
    public GameLODStats GetPerformanceStats()
    {
        var stats = new GameLODStats();
        stats.currentLODIndex = m_CurrentLODIndex;
        stats.isTransitioning = m_IsTransitioning;
        stats.lastDistance = m_LastDistance;
        stats.loadedLODCount = 0;

        foreach (var item in m_LODItems.Values)
        {
            if (item.IsLoaded)
                stats.loadedLODCount++;
        }

        return stats;
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!UnityEditor.Selection.Contains(gameObject))
            return;
        if (m_LODSettings == null || !m_LODSettings.EnableDebugDraw || lodLevels.Length == 0)
            return;
        Vector3 position = ReferencePoint.position;
        for (int i = 0; i < lodLevels.Length; i++)
        {
            Color color = i == m_CurrentLODIndex ? Color.green : Color.white;
            color.a = 0.8f;
            UnityEditor.Handles.color = color;
            float distance = lodLevels[i].distance;
            UnityEditor.Handles.DrawWireDisc(position, Vector3.up, distance);
            // 绘制标签
            UnityEditor.Handles.Label(position + Vector3.up * 2 + Vector3.right * distance, $"LOD {i}\n{distance}m");
        }
    }

    public void EditorPreviewLOD(int index)
    {
        EditorClearLOD();

        var lodLevel = lodLevels[index];
        if (!string.IsNullOrEmpty(lodLevel.assetAddress))
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(lodLevel.assetAddress);
            if (prefab != null)
            {
                GameObject go = Object.Instantiate(prefab, transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.name = $"{prefab.name} Preview LOD {index}";
            }
        }
    }

    public void EditorClearLOD()
    {
        //清除transform下的所有子物体
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
#endif
}

[Serializable]
public class GameLODStats
{
    public int currentLODIndex;
    public bool isTransitioning;
    public float lastDistance;
    public int loadedLODCount;
}