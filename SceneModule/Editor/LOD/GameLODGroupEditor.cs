using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameLODGroup))]
public class GameLODGroupEditor : Editor
{
    private GameLODGroup lodGroup;
    private SerializedProperty lodLevelsProperty;
    private bool[] lodFoldouts;

    private static readonly Color[] LODColors = new Color[]
    {
        new Color(0.4f, 0.5f, 0.1f), // LOD0
        new Color(0.2f, 0.3f, 0.5f), // LOD1
        new Color(0.1f, 0.4f, 0.4f), // LOD2
        new Color(0.5f, 0.1f, 0.1f), // LOD3+
    };
    private static readonly Color CulledColor = new Color(0.5f, 0.1f, 0.1f);

    private int draggingIndex = -1;

    void OnEnable()
    {
        lodGroup = (GameLODGroup)target;
        lodLevelsProperty = serializedObject.FindProperty("lodLevels");
        lodFoldouts = new bool[Mathf.Max(1, lodLevelsProperty.arraySize)];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("GameLOD 组", EditorStyles.boldLabel);

        // LOD条形分布可视化
        DrawLODBar();

        EditorGUILayout.Space();

        // LOD层级设置
        EditorGUILayout.LabelField("LOD 层级", EditorStyles.boldLabel);
        DrawLODLevels();

        EditorGUILayout.Space();

        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加LOD层级", GUILayout.Height(24)))
        {
            lodLevelsProperty.arraySize++;
            var newElement = lodLevelsProperty.GetArrayElementAtIndex(lodLevelsProperty.arraySize - 1);
            newElement.FindPropertyRelative("name").stringValue = $"LOD {lodLevelsProperty.arraySize - 1}";
            newElement.FindPropertyRelative("distance").floatValue = 50f * lodLevelsProperty.arraySize;
            lodFoldouts = new bool[lodLevelsProperty.arraySize];
        }
        if (GUILayout.Button("删除最后LOD层级", GUILayout.Height(24)))
        {
            if (lodLevelsProperty.arraySize > 0)
            {
                lodLevelsProperty.arraySize--;
                lodFoldouts = new bool[Mathf.Max(1, lodLevelsProperty.arraySize)];
            }
        }
        if (GUILayout.Button("清除所有LOD", GUILayout.Height(24)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清除所有LOD层级吗？", "确定", "取消"))
            {
                lodLevelsProperty.arraySize = 0;
                lodFoldouts = new bool[1];
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // GameSettings配置按钮
        if (GUILayout.Button("打开GameSettings配置", GUILayout.Height(22)))
        {
            SettingsService.OpenProjectSettings("Game/GameSettings");
        }

        EditorGUILayout.Space();

        // 运行时调试信息
        if (Application.isPlaying)
        {
            DrawRuntimeDebug();
        }
        else
        {
            EditorGUILayout.HelpBox("进入播放模式查看运行时调试信息", MessageType.Info);
        }

        // 编辑器下LOD预览按钮
        #if UNITY_EDITOR
        if (!Application.isPlaying && lodGroup.lodLevels != null && lodGroup.lodLevels.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("LOD 预览", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < lodGroup.lodLevels.Length; i++)
            {
                if (GUILayout.Button($"预览 LOD {i}", GUILayout.Height(24)))
                {
                    lodGroup.EditorPreviewLOD(i);
                }
            }
            if (GUILayout.Button("清空", GUILayout.Height(24)))
            {
                lodGroup.EditorClearLOD();
            }
            EditorGUILayout.EndHorizontal();
        }
        #endif

        serializedObject.ApplyModifiedProperties();

        // 自动刷新
        if (Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
            Repaint();
        }
    }

    private void DrawLODBar()
    {
        if (lodLevelsProperty.arraySize == 0)
            return;

        float barHeight = 24f;
        Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40, barHeight);

        float totalDistance = 0f;
        for (int i = 0; i < lodLevelsProperty.arraySize; i++)
        {
            totalDistance += Mathf.Max(0.01f, lodLevelsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("distance").floatValue);
        }

        float x = rect.x;
        float prevDistance = 0f;
        for (int i = 0; i < lodLevelsProperty.arraySize; i++)
        {
            var lod = lodLevelsProperty.GetArrayElementAtIndex(i);
            float distance = Mathf.Max(0.01f, lod.FindPropertyRelative("distance").floatValue);
            float width = rect.width * (distance / totalDistance);

            Color color = LODColors[Mathf.Min(i, LODColors.Length - 1)];
            EditorGUI.DrawRect(new Rect(x, rect.y, width, barHeight), color);

            // LOD标签
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x + 4, rect.y, width, barHeight), $"{lod.FindPropertyRelative("name").stringValue} ({distance}m)", style);

            // 拖动分隔线
            if (i < lodLevelsProperty.arraySize - 1)
            {
                float handleX = x + width - 4;
                Rect handleRect = new Rect(handleX, rect.y, 8, barHeight);

                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);

                if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
                {
                    draggingIndex = i;
                    Event.current.Use();
                }
                if (draggingIndex == i && Event.current.type == EventType.MouseDrag)
                {
                    float mouseX = Event.current.mousePosition.x;
                    float minX = rect.x + (i == 0 ? 0 : GetDistanceSum(i - 1) / totalDistance * rect.width);
                    float maxX = rect.x + GetDistanceSum(i + 1) / totalDistance * rect.width - 8;
                    mouseX = Mathf.Clamp(mouseX, minX + 8, maxX);

                    float newWidth = mouseX - x;
                    float newDistance = totalDistance * (newWidth / rect.width);
                    lod.FindPropertyRelative("distance").floatValue = Mathf.Max(0.01f, prevDistance + newDistance);

                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseUp && draggingIndex == i)
                {
                    draggingIndex = -1;
                    Event.current.Use();
                }
            }

            x += width;
            prevDistance += distance;
        }
        // 剩余部分为Culled
        if (x < rect.x + rect.width)
        {
            EditorGUI.DrawRect(new Rect(x, rect.y, rect.x + rect.width - x, barHeight), CulledColor);
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x + 4, rect.y, rect.x + rect.width - x, barHeight), "Culled", style);
        }
    }

    // 辅助函数：计算前i个LOD的距离和
    private float GetDistanceSum(int index)
    {
        float sum = 0f;
        for (int i = 0; i <= index && i < lodLevelsProperty.arraySize; i++)
        {
            sum += Mathf.Max(0.01f, lodLevelsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("distance").floatValue);
        }
        return sum;
    }

    private void DrawLODLevels()
    {
        for (int i = 0; i < lodLevelsProperty.arraySize; i++)
        {
            var lod = lodLevelsProperty.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical("box");
            lodFoldouts[i] = EditorGUILayout.Foldout(lodFoldouts[i], $"{lod.FindPropertyRelative("name").stringValue} ({lod.FindPropertyRelative("distance").floatValue}m)", true);
            if (lodFoldouts[i])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lod.FindPropertyRelative("name"), new GUIContent("名称"));
                EditorGUILayout.PropertyField(lod.FindPropertyRelative("distance"), new GUIContent("距离"));
                EditorGUILayout.PropertyField(lod.FindPropertyRelative("assetAddress"), new GUIContent("资源地址"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawRuntimeDebug()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("运行时调试", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        var stats = lodGroup.GetPerformanceStats();
        EditorGUILayout.LabelField($"当前LOD索引: {stats.currentLODIndex}");
        EditorGUILayout.LabelField($"是否正在切换: {stats.isTransitioning}");
        EditorGUILayout.LabelField($"最后检测距离: {stats.lastDistance:F2}");
        EditorGUILayout.LabelField($"已加载LOD数: {stats.loadedLODCount}");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("手动加载LOD", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < lodGroup.lodLevels.Length; i++)
        {
            GUI.color = stats.currentLODIndex == i ? Color.green : Color.white;
            if (GUILayout.Button($"LOD {i}", GUILayout.Height(22)))
            {
                lodGroup.ForceLoadLOD(i);
            }
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
}

// 创建菜单项
public class GameLODMenuItems
{
    [MenuItem("GameObject/GameLOD/创建LOD组", false, 0)]
    public static void CreateLODGroup()
    {
        GameObject go = new GameObject("GameLOD Group");
        go.AddComponent<GameLODGroup>();
        
        // 设置默认LOD层级
        var lodGroup = go.GetComponent<GameLODGroup>();
        lodGroup.lodLevels = new GameLODLevel[]
        {
            new GameLODLevel { name = "LOD 0 (高精度)", distance = 50f },
            new GameLODLevel { name = "LOD 1 (中精度)", distance = 100f },
            new GameLODLevel { name = "LOD 2 (低精度)", distance = 200f }
        };
        
        Selection.activeGameObject = go;
        
        Debug.Log("已创建GameLOD组，请在Inspector中配置LOD层级");
    }
    
    [MenuItem("GameObject/GameLOD/创建LOD管理器", false, 1)]
    public static void CreateLODManager()
    {
        if (GameLODManager.Instance == null)
        {
            GameObject go = new GameObject("GameLOD Manager");
            go.AddComponent<GameLODManager>();
            Selection.activeGameObject = go;
            
            Debug.Log("已创建GameLOD管理器");
        }
        else
        {
            Debug.LogWarning("GameLOD管理器已存在");
            Selection.activeGameObject = GameLODManager.Instance.gameObject;
        }
    }
    
    [MenuItem("Window/GameLOD/性能监视器")]
    public static void ShowPerformanceWindow()
    {
        GameLODPerformanceWindow.ShowWindow();
    }
}

// 性能监视器窗口
public class GameLODPerformanceWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private float refreshRate = 0.5f;
    private double lastRefreshTime;
    
    public static void ShowWindow()
    {
        GetWindow<GameLODPerformanceWindow>("GameLOD性能监视器");
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请在播放模式下使用性能监视器", MessageType.Info);
            return;
        }
        
        // 刷新率设置
        refreshRate = EditorGUILayout.Slider("刷新率(秒)", refreshRate, 0.1f, 2f);
        
        // 检查是否需要刷新
        if (EditorApplication.timeSinceStartup - lastRefreshTime > refreshRate)
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 全局统计
        if (GameLODManager.Instance != null)
        {
            var globalStats = GameLODManager.Instance.GetGlobalStats();
            
            EditorGUILayout.LabelField("全局统计", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"总LOD组数: {globalStats.totalLODGroups}");
            EditorGUILayout.LabelField($"活跃组数: {globalStats.activeLODGroups}");
            EditorGUILayout.LabelField($"帧更新数: {globalStats.frameUpdates}");
            EditorGUILayout.LabelField($"更新间隔: {globalStats.updateInterval:F3}s");
            
            EditorGUILayout.Space();
            
            // 对象池统计
            if (GameLODObjectPool.Instance != null)
            {
                var poolStats = GameLODObjectPool.Instance.GetPoolStats();
                
                EditorGUILayout.LabelField("对象池统计", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"总池数: {poolStats.totalPools}");
                EditorGUILayout.LabelField($"活跃对象: {poolStats.totalActiveObjects}");
                EditorGUILayout.LabelField($"空闲对象: {poolStats.totalIdleObjects}");
                
                // 池详情
                if (poolStats.poolDetails != null && poolStats.poolDetails.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("池详情", EditorStyles.boldLabel);
                    
                    foreach (var detail in poolStats.poolDetails.Values)
                    {
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField($"地址: {detail.address}");
                        EditorGUILayout.LabelField($"活跃: {detail.activeCount} / 空闲: {detail.idleCount} / 最大: {detail.maxSize}");
                        EditorGUILayout.EndVertical();
                    }
                }
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
} 