// Assets/Editor/SceneSnapshotExporter.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSnapshotExporter
{
    [MenuItem("Tools/Snapshot/Export Active Scene (JSON)")]
    public static void ExportActiveScene()
    {
        ExportScenes(new[] { SceneManager.GetActiveScene() });
    }

    [MenuItem("Tools/Snapshot/Export All Loaded Scenes (JSON)")]
    public static void ExportAllLoadedScenes()
    {
        var scenes = new List<Scene>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded) scenes.Add(s);
        }
        ExportScenes(scenes.ToArray());
    }

    private static void ExportScenes(Scene[] scenes)
    {
        if (scenes == null || scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Snapshot Export", "No loaded scenes found.", "OK");
            return;
        }

        string defaultName = scenes.Length == 1 ? scenes[0].name : "LoadedScenes";
        string path = EditorUtility.SaveFilePanel(
            "Export Scene Snapshot JSON",
            Application.dataPath,
            $"SceneSnapshot_{defaultName}_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            "json"
        );

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var snapshot = BuildSnapshot(scenes);
            string json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(path, json);
            Debug.Log($"[Snapshot] Exported to: {path}");
            EditorUtility.RevealInFinder(path);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Snapshot Export Failed", ex.Message, "OK");
        }
    }

    // =============================
    // Snapshot model (JsonUtility friendly)
    // =============================

    [Serializable]
    private class Snapshot
    {
        public string unityVersion;
        public string exportTimeUtc;
        public SnapshotScene[] scenes;
        public SnapshotObject[] objects;
        public SnapshotComponent[] components;
        public SnapshotReference[] references;
    }

    [Serializable]
    private class SnapshotScene
    {
        public string name;
        public string path;
        public bool isActive;
    }

    [Serializable]
    private class SnapshotObject
    {
        public int id;
        public string name;
        public bool activeSelf;
        public bool activeInHierarchy;
        public string tag;
        public int layer;
        public string layerName;
        public bool isStatic;
        public int sceneIndex;
        public int parentId;          // -1 if root
        public int[] childIds;
        public TransformData transform;
        public int[] componentIds;    // indexes into components[]
        public string prefabAssetPath; // if prefab instance
        public string prefabStatus;    // Connected/Disconnected/Missing/NotAPrefab
    }

    [Serializable]
    private class TransformData
    {
        public float[] localPosition;
        public float[] localRotationEuler;
        public float[] localScale;
        public float[] worldPosition;
        public float[] worldRotationEuler;
        public float[] lossyScale;
    }

    [Serializable]
    private class SnapshotComponent
    {
        public int id;
        public int ownerObjectId;
        public string type;          // e.g., UnityEngine.BoxCollider
        public string assembly;      // e.g., UnityEngine.CoreModule
        public bool enabled;         // if Behaviour
        public string dataJson;      // serialized key-values (flat)
        public string note;          // if skipped / errors
    }

    [Serializable]
    private class SnapshotReference
    {
        public int fromComponentId;
        public string fieldPath;     // e.g. "doorTarget" or "ui.titleText"
        public int toObjectId;       // if references a scene object
        public string toAssetPath;   // if references an asset
        public string toType;
    }

    // =============================
    // Build snapshot
    // =============================

    private static Snapshot BuildSnapshot(Scene[] scenes)
    {
        var snapshot = new Snapshot
        {
            unityVersion = Application.unityVersion,
            exportTimeUtc = DateTime.UtcNow.ToString("o"),
        };

        snapshot.scenes = new SnapshotScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            snapshot.scenes[i] = new SnapshotScene
            {
                name = scenes[i].name,
                path = scenes[i].path,
                isActive = scenes[i] == SceneManager.GetActiveScene()
            };
        }

        // Collect all objects in scenes (including inactive)
        var allRoots = new List<GameObject>();
        for (int si = 0; si < scenes.Length; si++)
        {
            var roots = scenes[si].GetRootGameObjects();
            allRoots.AddRange(roots);
        }

        // Map Unity instanceIDs -> stable incremental ids
        var objIdMap = new Dictionary<int, int>(capacity: 8192);
        var objects = new List<SnapshotObject>(capacity: 8192);

        // Traverse
        int nextObjId = 1;
        foreach (var root in allRoots)
        {
            Traverse(root, ref nextObjId, objIdMap, objects, scenes);
        }

        // Components
        var comps = new List<SnapshotComponent>(capacity: 16384);
        var refs = new List<SnapshotReference>(capacity: 8192);

        int nextCompId = 1;
        var compIdMap = new Dictionary<UnityEngine.Object, int>(new UnityObjectRefComparer());

        foreach (var obj in objects)
        {
            var go = EditorUtility.InstanceIDToObject(objIdMapReverseLookup(objIdMap, obj.id)) as GameObject;
            if (go == null) continue;

            var goComps = go.GetComponents<Component>();
            var compIds = new List<int>(goComps.Length);

            foreach (var c in goComps)
            {
                if (c == null) continue; // missing script
                int cid = nextCompId++;
                compIdMap[c] = cid;

                var sc = new SnapshotComponent
                {
                    id = cid,
                    ownerObjectId = obj.id,
                    type = c.GetType().FullName,
                    assembly = c.GetType().Assembly.GetName().Name,
                    enabled = (c is Behaviour b) ? b.enabled : true
                };

                // Serialize fields
                try
                {
                    SerializeComponent(c, sc, objIdMap, refs, cid);
                }
                catch (Exception ex)
                {
                    sc.note = "SerializeComponent failed: " + ex.GetType().Name + " " + ex.Message;
                }

                comps.Add(sc);
                compIds.Add(cid);
            }

            obj.componentIds = compIds.ToArray();
        }

        snapshot.objects = objects.ToArray();
        snapshot.components = comps.ToArray();
        snapshot.references = refs.ToArray();

        return snapshot;
    }

    private static void Traverse(GameObject go, ref int nextObjId, Dictionary<int, int> objIdMap, List<SnapshotObject> objects, Scene[] scenes)
    {
        if (go == null) return;

        int instanceId = go.GetInstanceID();
        if (!objIdMap.ContainsKey(instanceId))
        {
            int sid = nextObjId++;
            objIdMap[instanceId] = sid;

            int sceneIndex = FindSceneIndex(go.scene, scenes);

            var so = new SnapshotObject
            {
                id = sid,
                name = go.name,
                activeSelf = go.activeSelf,
                activeInHierarchy = go.activeInHierarchy,
                tag = SafeGetTag(go),
                layer = go.layer,
                layerName = LayerMask.LayerToName(go.layer),
                isStatic = go.isStatic,
                sceneIndex = sceneIndex,
                parentId = -1,
                childIds = Array.Empty<int>(),
                componentIds = Array.Empty<int>(),
                transform = CaptureTransform(go.transform),
            };

            // Prefab info
            var status = PrefabUtility.GetPrefabInstanceStatus(go);
            so.prefabStatus = status.ToString();
            if (status != PrefabInstanceStatus.NotAPrefab)
            {
                var asset = PrefabUtility.GetCorrespondingObjectFromSource(go);
                so.prefabAssetPath = asset ? AssetDatabase.GetAssetPath(asset) : "";
            }
            else
            {
                so.prefabAssetPath = "";
            }

            objects.Add(so);
        }

        // Recurse children
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i).gameObject;
            Traverse(child, ref nextObjId, objIdMap, objects, scenes);
        }

        // After children mapped, fill parent/children ids
        var myId = objIdMap[go.GetInstanceID()];
        var myObj = objects.Find(o => o.id == myId);

        var parent = go.transform.parent;
        myObj.parentId = parent ? objIdMap[parent.gameObject.GetInstanceID()] : -1;

        var childIds = new int[go.transform.childCount];
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i).gameObject;
            childIds[i] = objIdMap[child.GetInstanceID()];
        }
        myObj.childIds = childIds;

        // Write back (struct-like list element update)
        int idx = objects.FindIndex(o => o.id == myObj.id);
        objects[idx] = myObj;
    }

    private static int FindSceneIndex(Scene s, Scene[] scenes)
    {
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i] == s) return i;
        }
        return -1;
    }

    private static string SafeGetTag(GameObject go)
    {
        try { return go.tag; } catch { return "Untagged"; }
    }

    private static TransformData CaptureTransform(Transform t)
    {
        var td = new TransformData
        {
            localPosition = Vec3(t.localPosition),
            localRotationEuler = Vec3(t.localEulerAngles),
            localScale = Vec3(t.localScale),
            worldPosition = Vec3(t.position),
            worldRotationEuler = Vec3(t.eulerAngles),
            lossyScale = Vec3(t.lossyScale)
        };
        return td;
    }

    private static float[] Vec3(Vector3 v) => new[] { v.x, v.y, v.z };

    private static void SerializeComponent(Component c, SnapshotComponent sc, Dictionary<int, int> objIdMap, List<SnapshotReference> refs, int fromCompId)
    {
        // We want: serialize public + [SerializeField] private fields (flat), avoid deep cycles
        var dict = new List<KV>();

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = c.GetType();
        var fields = type.GetFields(flags);

        foreach (var f in fields)
        {
            if (f.IsStatic) continue;
            if (f.IsNotSerialized) continue;

            bool isPublic = f.IsPublic;
            bool hasSerializeField = f.GetCustomAttribute<SerializeField>() != null;
            if (!isPublic && !hasSerializeField) continue;

            object val;
            try { val = f.GetValue(c); }
            catch { continue; }

            // UnityEngine.Object refs
            if (val is UnityEngine.Object uo)
            {
                AddReference(refs, fromCompId, f.Name, uo, objIdMap);
                dict.Add(new KV { k = f.Name, v = DescribeUnityObject(uo) });
                continue;
            }

            // Simple types
            if (val == null)
            {
                dict.Add(new KV { k = f.Name, v = "null" });
                continue;
            }

            var vt = val.GetType();
            if (vt == typeof(string) || vt.IsPrimitive || vt.IsEnum)
            {
                dict.Add(new KV { k = f.Name, v = val.ToString() });
            }
            else if (vt == typeof(Vector2))
            {
                var v2 = (Vector2)val;
                dict.Add(new KV { k = f.Name, v = $"{v2.x},{v2.y}" });
            }
            else if (vt == typeof(Vector3))
            {
                var v3 = (Vector3)val;
                dict.Add(new KV { k = f.Name, v = $"{v3.x},{v3.y},{v3.z}" });
            }
            else if (vt == typeof(Color))
            {
                var col = (Color)val;
                dict.Add(new KV { k = f.Name, v = $"{col.r},{col.g},{col.b},{col.a}" });
            }
            else
            {
                // Fallback: type name only (keeps export stable)
                dict.Add(new KV { k = f.Name, v = $"<{vt.Name}>" });
            }
        }

        sc.dataJson = JsonUtility.ToJson(new KVList { items = dict.ToArray() }, true);

        // Missing script detection (component type exists but script missing appears as null component; handled earlier)
        if (c is Renderer r)
        {
            // Add material refs as assets
            try
            {
                var mats = r.sharedMaterials;
                if (mats != null)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        AddReference(refs, fromCompId, $"sharedMaterials[{i}]", mats[i], objIdMap);
                    }
                }
            }
            catch { }
        }
    }

    [Serializable]
    private class KV { public string k; public string v; }

    [Serializable]
    private class KVList { public KV[] items; }

    private static void AddReference(List<SnapshotReference> refs, int fromComponentId, string fieldPath, UnityEngine.Object uo, Dictionary<int, int> objIdMap)
    {
        if (uo == null) return;

        var sr = new SnapshotReference
        {
            fromComponentId = fromComponentId,
            fieldPath = fieldPath,
            toObjectId = 0,
            toAssetPath = "",
            toType = uo.GetType().FullName
        };

        // Scene object reference
        if (uo is GameObject go)
        {
            if (objIdMap.TryGetValue(go.GetInstanceID(), out int oid))
                sr.toObjectId = oid;
            refs.Add(sr);
            return;
        }
        if (uo is Component comp)
        {
            var g = comp.gameObject;
            if (objIdMap.TryGetValue(g.GetInstanceID(), out int oid))
                sr.toObjectId = oid;
            refs.Add(sr);
            return;
        }

        // Asset reference
        string ap = AssetDatabase.GetAssetPath(uo);
        if (!string.IsNullOrEmpty(ap))
        {
            sr.toAssetPath = ap;
        }
        refs.Add(sr);
    }

    private static string DescribeUnityObject(UnityEngine.Object uo)
    {
        if (uo == null) return "null";
        string ap = AssetDatabase.GetAssetPath(uo);
        if (!string.IsNullOrEmpty(ap))
            return $"{uo.name} (Asset:{ap})";
        if (uo is GameObject go)
            return $"{go.name} (SceneObject)";
        if (uo is Component c)
            return $"{c.GetType().Name} on {c.gameObject.name} (SceneObject)";
        return $"{uo.name} ({uo.GetType().Name})";
    }

    // Helper: reverse lookup id -> instanceId (only used for components stage)
    private static int objIdMapReverseLookup(Dictionary<int, int> objIdMap, int objectId)
    {
        foreach (var kv in objIdMap)
            if (kv.Value == objectId) return kv.Key;
        return 0;
    }

    private class UnityObjectRefComparer : IEqualityComparer<UnityEngine.Object>
    {
        public bool Equals(UnityEngine.Object x, UnityEngine.Object y) => x == y;
        public int GetHashCode(UnityEngine.Object obj) => obj ? obj.GetInstanceID() : 0;
    }
}