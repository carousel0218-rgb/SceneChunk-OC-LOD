using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class LODGroupBatchConverterEditor : EditorWindow
{
    string sourcePath = "Assets/Res/Scenes/WorldScene/Big_Obj";
    string targetPath = "Assets/Res/Scenes/LOD";

    [MenuItem("Tools/LODGroup批量转换工具")]
    public static void ShowWindow()
    {
        GetWindow<LODGroupBatchConverterEditor>("LODGroup批量转换工具");
    }

    void OnGUI()
    {
        GUILayout.Label("批量LODGroup转换", EditorStyles.boldLabel);
        sourcePath = EditorGUILayout.TextField("源路径", sourcePath);
        targetPath = EditorGUILayout.TextField("目标路径", targetPath);

        if (GUILayout.Button("批量转换为GameLODGroup"))
        {
            ConvertToGameLODGroup();
        }
        if (GUILayout.Button("还原为原生LODGroup"))
        {
            RestoreToLODGroup();
        }
    }

    void ConvertToGameLODGroup()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { sourcePath });
        int count = 0;
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bool changed = false;

            // 处理所有LODGroup
            var lodGroups = instance.GetComponentsInChildren<LODGroup>(true);
            foreach (var lodGroup in lodGroups)
            {
                var lods = lodGroup.GetLODs();
                List<GameLODLevel> lodLevels = new List<GameLODLevel>();

                // 以“原预制体名_原LODGroup名”为子文件夹
                string groupFolderName = prefab.name + "_" + lodGroup.gameObject.name;
                string groupFolder = Path.Combine(targetPath, groupFolderName).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(groupFolder))
                {
                    if (!AssetDatabase.IsValidFolder(targetPath))
                        AssetDatabase.CreateFolder("Assets", targetPath.Substring("Assets/".Length));
                    AssetDatabase.CreateFolder(targetPath, groupFolderName);
                }

                for (int i = 0; i < lods.Length; i++)
                {
                    var renderers = lods[i].renderers;
                    if (renderers == null || renderers.Length == 0) continue;

                    for (int j = 0; j < renderers.Length; j++)
                    {
                        var renderer = renderers[j];
                        var go = renderer.gameObject;

                        // 生成新预制体
                        string lodPrefabName = $"{prefab.name}_LOD{i}_{go.name}.prefab";
                        string lodPrefabPath = Path.Combine(groupFolder, lodPrefabName).Replace("\\", "/");
                        GameObject lodInstance = Instantiate(go);
                        PrefabUtility.SaveAsPrefabAsset(lodInstance, lodPrefabPath);
                        DestroyImmediate(lodInstance);

                        // 记录LODLevel
                        GameLODLevel level = new GameLODLevel();
                        level.name = $"LOD{i}_{go.name}";
                        level.assetAddress = lodPrefabPath;
                        level.distance = GetLODDistance(lodGroup, i);
                        lodLevels.Add(level);
                    }
                }

                // 替换为GameLODGroup
                var gameLODGroup = lodGroup.gameObject.AddComponent<GameLODGroup>();
                gameLODGroup.lodLevels = lodLevels.ToArray();
                
                // var ocObj = lodGroup.GetComponent<OCObject>();
                // if (ocObj != null)
                //     DestroyImmediate(ocObj);
                // 移除LODGroup和OCObject
                DestroyImmediate(lodGroup);

                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                count++;
            }
            DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"转换完成，共处理{count}个预制体。", "确定");
    }

    float GetLODDistance(LODGroup group, int index)
    {
        var lods = group.GetLODs();
        if (index < 0 || index >= lods.Length) return 50f;
        // 你可以自定义距离算法
        return 50f * (1f - lods[index].screenRelativeTransitionHeight);
    }

    void RestoreToLODGroup()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { targetPath });
        int count = 0;
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bool changed = false;

            var gameLODGroups = instance.GetComponentsInChildren<GameLODGroup>(true);
            foreach (var gameLODGroup in gameLODGroups)
            {
                // 还原LODGroup
                var lodGroup = gameLODGroup.gameObject.AddComponent<LODGroup>();
                var lodLevels = gameLODGroup.lodLevels;
                List<LOD> lods = new List<LOD>();
                List<GameObject> createdObjs = new List<GameObject>();
                for (int i = 0; i < lodLevels.Length; i++)
                {
                    var level = lodLevels[i];
                    GameObject lodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(level.assetAddress);
                    if (lodPrefab == null) continue;
                    GameObject lodObj = Instantiate(lodPrefab, gameLODGroup.transform);
                    lodObj.name = $"Restored_{level.name}";
                    var renderer = lodObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        float screenPercent = 1f - (level.distance / 50f); // 还原算法需和上面一致
                        lods.Add(new LOD(screenPercent, new Renderer[] { renderer }));
                        createdObjs.Add(lodObj);
                    }
                }
                lodGroup.SetLODs(lods.ToArray());
                lodGroup.RecalculateBounds();

                DestroyImmediate(gameLODGroup);

                // 清理OCObject
                var ocObj = lodGroup.GetComponent<OCObject>();
                if (ocObj != null)
                    DestroyImmediate(ocObj);

                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                count++;
            }
            DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"还原完成，共处理{count}个预制体。", "确定");
    }
}