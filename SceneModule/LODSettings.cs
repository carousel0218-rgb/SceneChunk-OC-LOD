using UnityEngine;

namespace GameSetting
{
    [System.Serializable]
    public class LODSettings
    {
        [Header("检测设置")]
        [SerializeField] private float m_UpdateInterval = 0.1f; // 更新间隔
        [SerializeField] private float m_Lag = 0.1f; // 滞后系数，防止抖动
        [SerializeField] private bool m_UseSquaredDistance = true; // 使用平方距离
        
        [Header("异步加载")]
        [SerializeField] private int m_MaxConcurrentLoads = 3; // 最大并发加载数
        [SerializeField] private bool m_PreloadNext = false; // 是否预加载下一级LOD
        
        [Header("调试")]
        [SerializeField] private bool m_EnableDebugDraw = false;
        
        [Header("全局管理")]
        [SerializeField] private float m_GlobalUpdateInterval = 0.1f;
        [SerializeField] private int m_MaxLODUpdatesPerFrame = 10;
        [SerializeField] private bool m_EnablePerformanceMonitoring = true;
        [SerializeField] private bool m_EnableAutoGC = false;
        [SerializeField] private float m_GCInterval = 300f;
        
        [Header("回滞设置")]
        [SerializeField] private float m_Hysteresis = 1f; // LOD切换回滞，默认1米
        
        [Header("对象池")]
        [SerializeField] private int m_MaxPoolSize = 20;
        [SerializeField] private float m_PoolExpireTime = 300f; // 5分钟
        
        [Header("内存管理")]
        public bool autoRelease = true;
        public float releaseDelay = 15f; // 延迟释放时间
        
        public float UpdateInterval => m_UpdateInterval;
        public float Lag => m_Lag;
        public bool UseSquaredDistance => m_UseSquaredDistance;
        public bool PreloadNext => m_PreloadNext;
        public bool EnableDebugDraw => m_EnableDebugDraw;
        public float GlobalUpdateInterval => m_GlobalUpdateInterval;
        public int MaxLODUpdatesPerFrame => m_MaxLODUpdatesPerFrame;
        public bool EnablePerformanceMonitoring => m_EnablePerformanceMonitoring;
        public bool EnableAutoGC => m_EnableAutoGC;
        public float GCInterval => m_GCInterval;
        public int MaxPoolSize => m_MaxPoolSize;
        public float PoolExpireTime => m_PoolExpireTime;
        public bool AutoRelease => autoRelease;
        public float ReleaseDelay => releaseDelay;
        public float Hysteresis => m_Hysteresis;
        
    }
}