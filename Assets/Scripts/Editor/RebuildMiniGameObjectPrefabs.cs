#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds mini-game stage prefabs as real GameObject hierarchies (no runtime BuildScene).
/// Menu: Hackerton/Rebuild MiniGame Prefabs As GameObjects
/// </summary>
public static class RebuildMiniGameObjectPrefabs
{
    private const string PrefabRoot = "Assets/Prefab";
    private const string SpawnableRoot = "Assets/Prefab/MiniGames/Spawnables";
    private const string ArtRoot = "Assets/Art/MiniGames";
    private const string SquareSpritePath = ArtRoot + "/WhiteSquare.png";
    private const string RebuildVersionKey = "Hackerton.MiniGameRectSpriteRebuild.v9";

    private static Sprite _square;

    [MenuItem("Hackerton/Rebuild MiniGame Prefabs As GameObjects")]
    public static void RebuildAll()
    {
        EnsureFolders();
        _square = EnsureSquareSprite();

        var spawnables = BuildSpawnables();
        BuildAllStages(spawnables);
        FixOrderPrefabRoots();

        EditorPrefs.SetInt(RebuildVersionKey, 9);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RebuildMiniGameObjectPrefabs] 2D 사각형 SpriteRenderer GameObject로 재구성 완료.");
    }

    private static readonly (string order, string prefab)[] OrderPrefabMap =
    {
        ("Order7", "Stage-GasValve"), ("Order8", "Stage-MeteorShoot"), ("Order9", "Stage-PoisonPick"),
        ("Order10", "Stage-RoadCross"), ("Order11", "Stage-FuseBox"), ("Order12", "Stage-ShieldBlock"),
        ("Order13", "Stage-WireCut"), ("Order14", "Stage-LeakPatch"), ("Order15", "Stage-SmokeExit"),
        ("Order16", "Stage-StunShoot"), ("Order17", "Stage-BalanceIce"), ("Order18", "Stage-CprRhythm"),
        ("Order19", "Stage-CatchGlass"), ("Order20", "Stage-LaserMaze"), ("Order21", "Stage-SwingDodge"),
        ("Order22", "Stage-WhackHazard"), ("Order23", "Stage-ElevatorPick"), ("Order24", "Stage-Disinfect"),
        ("Order25", "Stage-TurretDefend"), ("Order26", "Stage-BraceDoor"),
    };

    private static void FixOrderPrefabRoots()
    {
        foreach (var pair in OrderPrefabMap)
        {
            var orderPath = $"Assets/Data/{pair.order}.asset";
            var prefabPath = $"{PrefabRoot}/{pair.prefab}.prefab";
            var order = AssetDatabase.LoadAssetAtPath<OrderSO>(orderPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (order == null || prefab == null)
                continue;

            order.roomPrefab = prefab;
            EditorUtility.SetDirty(order);
        }
    }

    // Force one rebuild when switching to rectangle-only sprites.
    [InitializeOnLoadMethod]
    private static void AutoRebuildIfNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorPrefs.GetInt(RebuildVersionKey, 0) >= 9)
                return;

            var sample = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Stage-GasValve.prefab");
            if (sample == null)
                return;

            RebuildAll();
        };
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Prefab");
        EnsureFolder("Assets/Prefab/MiniGames");
        EnsureFolder(SpawnableRoot);
        EnsureFolder("Assets/Art");
        EnsureFolder(ArtRoot);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Sprite EnsureSquareSprite()
    {
        if (!File.Exists(SquareSpritePath))
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SquareSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SquareSpritePath);
        }

        SetSpriteImport(SquareSpritePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
    }

    private static void SetSpriteImport(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 4f;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private class Spawnables
    {
        public GameObject Cell;
        public GameObject Meteor;
        public GameObject Flash;
        public GameObject Car;
        public GameObject Projectile;
        public GameObject Leak;
        public GameObject Enemy;
        public GameObject Muzzle;
        public GameObject Glass;
        public GameObject Danger;
        public GameObject Safe;
        public GameObject Bullet;
        public GameObject Mist;
        public GameObject GoodFlash;
        public GameObject BadFlash;
    }

    private static Spawnables BuildSpawnables()
    {
        // Spawnables keep sprite on root so runtime scale works.
        return new Spawnables
        {
            Cell = SaveSpawnable("Cell", RectScaled(null, "Cell", Vector2.zero, Vector2.one, new Color(0.75f, 0.75f, 0.78f), 2)),
            Meteor = SaveSpawnable("Meteor", RectScaled(null, "Meteor", Vector2.zero, Vector2.one, new Color(0.85f, 0.35f, 0.12f), 3)),
            Flash = SaveSpawnable("Flash", RectScaled(null, "Flash", Vector2.zero, Vector2.one * 0.45f, Color.yellow, 5)),
            Car = SaveSpawnable("Car", RectScaled(null, "Car", Vector2.zero, new Vector2(1.6f, 0.7f), new Color(0.9f, 0.25f, 0.2f), 3)),
            Projectile = SaveSpawnable("Projectile", RectScaled(null, "Projectile", Vector2.zero, Vector2.one * 0.45f, new Color(0.95f, 0.25f, 0.2f), 3)),
            Leak = SaveSpawnable("Leak", RectScaled(null, "Leak", Vector2.zero, Vector2.one * 0.7f, new Color(0.25f, 0.65f, 0.95f, 0.85f), 4)),
            Enemy = SaveSpawnable("Enemy", RectScaled(null, "Enemy", Vector2.zero, Vector2.one * 0.7f, new Color(0.85f, 0.2f, 0.25f), 3)),
            Muzzle = SaveSpawnable("Muzzle", RectScaled(null, "Muzzle", Vector2.zero, Vector2.one * 0.35f, new Color(1f, 0.9f, 0.3f), 6)),
            Glass = SaveSpawnable("Glass", RectScaled(null, "Glass", Vector2.zero, new Vector2(0.55f, 0.85f), new Color(0.7f, 0.9f, 1f, 0.9f), 3)),
            Danger = SaveSpawnable("DangerTarget", RectScaled(null, "Danger", Vector2.zero, Vector2.one * 0.9f, new Color(0.9f, 0.2f, 0.2f), 3)),
            Safe = SaveSpawnable("SafeTarget", RectScaled(null, "Safe", Vector2.zero, Vector2.one * 0.9f, new Color(0.3f, 0.8f, 0.4f), 3)),
            Bullet = SaveSpawnable("Bullet", RectScaled(null, "Bullet", Vector2.zero, Vector2.one * 0.28f, new Color(1f, 0.9f, 0.3f), 5)),
            Mist = SaveSpawnable("Mist", RectScaled(null, "Mist", Vector2.zero, Vector2.one * 0.5f, new Color(0.5f, 0.9f, 1f, 0.35f), 5)),
            GoodFlash = SaveSpawnable("GoodFlash", RectScaled(null, "GoodFlash", Vector2.zero, new Vector2(1.2f, 0.4f), new Color(0.3f, 0.9f, 0.4f), 5)),
            BadFlash = SaveSpawnable("BadFlash", RectScaled(null, "BadFlash", Vector2.zero, new Vector2(1.2f, 0.4f), new Color(0.9f, 0.25f, 0.2f), 5)),
        };
    }

    private static GameObject SaveSpawnable(string name, GameObject go)
    {
        var path = $"{SpawnableRoot}/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void BuildAllStages(Spawnables s)
    {
        BuildGasValve();
        BuildSnake(s);
        BuildPoison();
        BuildRoad(s);
        BuildFuse();
        BuildSandbag(s);
        BuildWire();
        BuildPipe(s);
        BuildSmoke();
        BuildCrate(s);
        BuildBalance();
        BuildCpr(s);
        BuildDebris(s);
        BuildLaser();
        BuildSwing(s);
        BuildAlarmSimon(s);
        BuildElevator();
        BuildKit(s);
        BuildGap(s);
        BuildBrace();
    }

    private static GameObject Empty(Transform parent, string name)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject NewStageRoot(string prefabName, System.Type stageType)
    {
        var root = new GameObject(prefabName);
        root.AddComponent(stageType);
        return root;
    }

    private static void SaveStage(string prefabName, GameObject root, System.Action<SerializedObject> bind)
    {
        var path = $"{PrefabRoot}/{prefabName}.prefab";

        MonoBehaviour stage = null;
        foreach (var behaviour in root.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name.EndsWith("Stage"))
            {
                stage = behaviour;
                break;
            }
        }

        if (stage != null && bind != null)
        {
            var so = new SerializedObject(stage);
            bind(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void Bind(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null)
            p.objectReferenceValue = value;
    }

    private static void BindArray(SerializedObject so, string prop, Object[] values)
    {
        var p = so.FindProperty(prop);
        if (p == null || !p.isArray)
            return;
        p.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    /// <summary>
    /// Unscaled root + Visual child. Safe to nest other objects under the root
    /// without inheriting sprite scale (fixes broken click/layout).
    /// </summary>
    private static GameObject Rect(Transform parent, string name, Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = _square;
        sr.color = color;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Simple;
        return go;
    }

    /// <summary>Sprite on root with scale=size. Use when runtime code scales the transform.</summary>
    private static GameObject RectScaled(Transform parent, string name, Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _square;
        sr.color = color;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Simple;
        return go;
    }

    private static GameObject Square(Transform parent, string name, Vector2 pos, float size, Color color, int order)
    {
        return Rect(parent, name, pos, Vector2.one * size, color, order);
    }

    private static GameObject SquareScaled(Transform parent, string name, Vector2 pos, float size, Color color, int order)
    {
        return RectScaled(parent, name, pos, Vector2.one * size, color, order);
    }

    private static TextMesh CounterText(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

        var text = go.AddComponent<TextMesh>();
        text.text = "8";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.42f;
        text.fontSize = 64;
        text.color = Color.white;
        go.GetComponent<MeshRenderer>().sortingOrder = 10;
        return text;
    }

    private static SpriteRenderer GetSprite(GameObject go)
    {
        return go != null ? go.GetComponentInChildren<SpriteRenderer>(true) : null;
    }

    private static MiniGameTarget AddTarget(GameObject go, bool isSafe, Vector2 size)
    {
        var t = go.GetComponent<MiniGameTarget>() ?? go.AddComponent<MiniGameTarget>();
        t.IsSafe = isSafe;
        t.HalfSize = size * 0.5f;
        return t;
    }

    private static void BuildGasValve()
    {
        var root = NewStageRoot("Stage-GasValve", typeof(GasValveStage));
        Rect(root.transform, "Floor", Vector2.zero, new Vector2(14f, 8f), new Color(0.18f, 0.2f, 0.22f), -2);
        // The gas starts by covering the full 14 x 8 stage and shrinks at runtime.
        var gas = RectScaled(root.transform, "GasCloud", Vector2.zero, new Vector2(14f, 8f), new Color(0.55f, 0.9f, 0.35f, 0.45f), 0);
        var valve = Square(root.transform, "ValveBody", Vector2.zero, 1.4f, new Color(0.75f, 0.2f, 0.15f), 2);
        Rect(valve.transform, "ValveHandle", new Vector2(0.7f, 0f), new Vector2(0.9f, 0.22f), new Color(0.95f, 0.9f, 0.7f), 3);
        Rect(root.transform, "Pipe", new Vector2(0f, -2.2f), new Vector2(0.55f, 2.4f), new Color(0.45f, 0.48f, 0.5f), 1);
        SaveStage("Stage-GasValve", root, so =>
        {
            Bind(so, "valve", valve.transform);
            Bind(so, "gas", gas.transform);
        });
    }

    private static void BuildSnake(Spawnables s)
    {
        var root = NewStageRoot("Stage-MeteorShoot", typeof(SnakeEscapeStage));
        var bg = Rect(root.transform, "BG", Vector2.zero, new Vector2(14f, 9f), new Color(0.1f, 0.12f, 0.14f), -3);
        var board = Empty(root.transform, "BoardRoot");
        SaveStage("Stage-MeteorShoot", root, so =>
        {
            Bind(so, "boardRoot", board.transform);
            Bind(so, "cellPrefab", s.Cell);
            Bind(so, "bg", GetSprite(bg));
        });
    }

    private static void BuildPoison()
    {
        var root = NewStageRoot("Stage-PoisonPick", typeof(PoisonPickStage));
        Rect(root.transform, "Table", Vector2.zero, new Vector2(12f, 7f), new Color(0.22f, 0.18f, 0.16f), -2);
        Rect(root.transform, "Shelf", new Vector2(0f, -0.8f), new Vector2(10f, 0.35f), new Color(0.4f, 0.28f, 0.18f), 0);
        var bottles = new MiniGameTarget[5];
        var safe = Random.Range(0, 5);
        for (var i = 0; i < 5; i++)
        {
            var isSafe = i == safe;
            var pos = new Vector2(-(5 - 1) * 0.9f + i * 1.8f, 0.4f);
            var color = isSafe ? new Color(0.35f, 0.65f, 0.95f) : new Color(0.65f, 0.2f, 0.65f);
            var body = Rect(root.transform, isSafe ? "SafeBottle" : $"PoisonBottle{i}", pos, new Vector2(0.85f, 1.7f), color, 2);
            Rect(body.transform, "Cap", new Vector2(0f, 1.05f), new Vector2(0.55f, 0.28f), Color.white, 3);
            Rect(body.transform, "Label", new Vector2(0f, -0.15f), new Vector2(0.55f, 0.35f),
                isSafe ? new Color(0.9f, 0.95f, 1f) : new Color(0.2f, 0.85f, 0.25f), 3);
            bottles[i] = AddTarget(body, isSafe, new Vector2(0.85f, 1.7f));
        }
        SaveStage("Stage-PoisonPick", root, so => BindArray(so, "bottles", bottles));
    }

    private static void BuildRoad(Spawnables s)
    {
        var root = NewStageRoot("Stage-RoadCross", typeof(RoadCrossStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(14f, 9f), new Color(0.2f, 0.22f, 0.2f), -4);
        Rect(root.transform, "Road", Vector2.zero, new Vector2(14f, 4.4f), new Color(0.18f, 0.18f, 0.2f), -3);
        Rect(root.transform, "SidewalkBottom", new Vector2(0f, -3.1f), new Vector2(14f, 1.4f), new Color(0.45f, 0.45f, 0.42f), -2);
        Rect(root.transform, "SidewalkTop", new Vector2(0f, 3.1f), new Vector2(14f, 1.4f), new Color(0.45f, 0.55f, 0.4f), -2);
        var goal = Rect(root.transform, "Goal", new Vector2(0f, 3.2f), new Vector2(2.4f, 0.35f), new Color(0.3f, 0.9f, 0.4f), 1);
        for (var i = -2; i <= 2; i++)
            Rect(root.transform, $"LaneMark{i}", new Vector2(i * 2.8f, 0f), new Vector2(1.2f, 0.12f), new Color(0.9f, 0.85f, 0.3f), -1);
        var player = Square(root.transform, "Player", new Vector2(0f, -3.1f), 0.7f, new Color(0.3f, 0.7f, 1f), 5);
        var spawn = Empty(root.transform, "SpawnParent");
        SaveStage("Stage-RoadCross", root, so =>
        {
            Bind(so, "player", player.transform);
            Bind(so, "goal", goal.transform);
            Bind(so, "spawnParent", spawn.transform);
            Bind(so, "carPrefab", s.Car);
        });
    }

    private static void BuildFuse()
    {
        var root = NewStageRoot("Stage-FuseBox", typeof(FuseBoxStage));
        Rect(root.transform, "Wall", Vector2.zero, new Vector2(12f, 8f), new Color(0.2f, 0.22f, 0.25f), -2);
        Rect(root.transform, "Panel", Vector2.zero, new Vector2(7f, 4.5f), new Color(0.32f, 0.34f, 0.3f), 0);
        var colors = new[]
        {
            new Color(0.9f, 0.25f, 0.2f), new Color(0.95f, 0.8f, 0.2f),
            new Color(0.25f, 0.75f, 0.35f), new Color(0.25f, 0.55f, 0.95f)
        };
        var fuses = new MiniGameTarget[4];
        for (var i = 0; i < 4; i++)
        {
            var go = Rect(root.transform, $"Fuse{i}", new Vector2(-2.1f + i * 1.4f, 0.2f), new Vector2(1f, 2.2f), colors[i], 2);
            Rect(go.transform, "Handle", new Vector2(0f, 0.7f), new Vector2(0.55f, 0.25f), new Color(0.85f, 0.85f, 0.85f), 3);
            fuses[i] = AddTarget(go, false, new Vector2(1f, 2.2f));
        }
        SaveStage("Stage-FuseBox", root, so => BindArray(so, "fuses", fuses));
    }

    private static void BuildSandbag(Spawnables s)
    {
        var root = NewStageRoot("Stage-ShieldBlock", typeof(SandbagStackStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 8f), new Color(0.2f, 0.28f, 0.35f), -3);
        var flood = RectScaled(root.transform, "FloodLine", new Vector2(0f, -3f), new Vector2(4.2f, 2.5f),
            new Color(0.2f, 0.45f, 0.85f, 0.55f), -1);
        var board = Empty(root.transform, "BoardRoot");
        SaveStage("Stage-ShieldBlock", root, so =>
        {
            Bind(so, "boardRoot", board.transform);
            Bind(so, "bagPrefab", s.Cell);
            Bind(so, "floodLine", flood.transform);
        });
    }

    private static void BuildWire()
    {
        var root = NewStageRoot("Stage-WireCut", typeof(WireCutStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(12f, 8f), new Color(0.12f, 0.12f, 0.14f), -3);
        Rect(root.transform, "Bomb", Vector2.zero, new Vector2(4.5f, 3.2f), new Color(0.28f, 0.28f, 0.3f), 0);
        var colors = new[]
        {
            new Color(0.9f, 0.2f, 0.2f), new Color(0.25f, 0.75f, 0.35f),
            new Color(0.95f, 0.85f, 0.2f), new Color(0.3f, 0.55f, 0.95f)
        };
        var safe = Random.Range(0, 4);
        var wires = new MiniGameTarget[4];
        for (var i = 0; i < 4; i++)
        {
            var go = Rect(root.transform, $"Wire{i}", new Vector2(0f, 1f - i * 0.7f), new Vector2(3.6f, 0.28f), colors[i], 2);
            wires[i] = AddTarget(go, i == safe, new Vector2(3.6f, 0.28f));
        }
        Rect(root.transform, "HintFrame", new Vector2(0f, -2.2f), new Vector2(1.7f, 0.7f), new Color(0.5f, 0.5f, 0.55f), 2);
        var hint = Rect(root.transform, "Hint", new Vector2(0f, -2.2f), new Vector2(1.4f, 0.45f), colors[safe], 3);
        SaveStage("Stage-WireCut", root, so =>
        {
            BindArray(so, "wires", wires);
            Bind(so, "hintRenderer", GetSprite(hint));
        });
    }

    private static void BuildPipe(Spawnables s)
    {
        var root = NewStageRoot("Stage-LeakPatch", typeof(LeakSlingStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 9f), new Color(0.14f, 0.18f, 0.22f), -4);
        Rect(root.transform, "Wall", new Vector2(0f, 1.2f), new Vector2(12f, 5.2f), new Color(0.28f, 0.3f, 0.32f), -2);
        Rect(root.transform, "Floor", new Vector2(0f, -3.4f), new Vector2(13f, 1.4f), new Color(0.22f, 0.24f, 0.26f), -1);

        var flood = RectScaled(root.transform, "FloodFill", new Vector2(0f, -3.9f), new Vector2(12.5f, 0.15f),
            new Color(0.2f, 0.55f, 0.95f, 0.55f), 1);
        var overflow = Rect(root.transform, "OverflowLine", new Vector2(5.75f, 0f), new Vector2(0.55f, 0.09f),
            new Color(1f, 0.9f, 0f), 3);
        var sling = Square(root.transform, "SlingAnchor", new Vector2(0f, -2.6f), 0.55f, new Color(0.55f, 0.35f, 0.2f), 4);
        Rect(sling.transform, "ForkL", new Vector2(-0.45f, 0.35f), new Vector2(0.18f, 0.7f), new Color(0.45f, 0.3f, 0.18f), 5);
        Rect(sling.transform, "ForkR", new Vector2(0.45f, 0.35f), new Vector2(0.18f, 0.7f), new Color(0.45f, 0.3f, 0.18f), 5);
        var pull = Square(root.transform, "PullMarker", new Vector2(0f, -3.2f), 0.4f, new Color(0.95f, 0.75f, 0.25f), 6);
        var band = RectScaled(root.transform, "BandVisual", new Vector2(0f, -2.9f), new Vector2(0.2f, 0.12f),
            new Color(0.85f, 0.35f, 0.25f), 5);
        var spawn = Empty(root.transform, "SpawnParent");

        SaveStage("Stage-LeakPatch", root, so =>
        {
            Bind(so, "slingAnchor", sling.transform);
            Bind(so, "pullMarker", pull.transform);
            Bind(so, "bandVisual", band.transform);
            Bind(so, "floodFill", flood.transform);
            Bind(so, "overflowLine", overflow.transform);
            Bind(so, "spawnParent", spawn.transform);
            Bind(so, "patchPrefab", s.Flash);
            Bind(so, "leakPrefab", s.Leak);
        });
    }

    private static void BuildSmoke()
    {
        var root = NewStageRoot("Stage-SmokeExit", typeof(SmokeExitStage));
        Rect(root.transform, "Dark", Vector2.zero, new Vector2(14f, 9f), new Color(0.08f, 0.08f, 0.1f), -4);
        for (var i = 0; i < 10; i++)
        {
            var smokePos = new Vector2(Random.Range(-6f, 6f), Random.Range(-3.5f, 3.5f));
            Square(root.transform, $"Smoke{i}", smokePos, Random.Range(1.5f, 3f), new Color(0.35f, 0.35f, 0.38f, 0.35f), -2);
        }
        var hazards = new Transform[6];
        var positions = new[]
        {
            new Vector2(-1f, 0f), new Vector2(1.5f, -1.5f), new Vector2(0f, 1.8f),
            new Vector2(2.5f, 0.5f), new Vector2(-2.5f, 1.2f), new Vector2(0.5f, -2.2f)
        };
        for (var i = 0; i < hazards.Length; i++)
            hazards[i] = Square(root.transform, $"Fire{i}", positions[i], 0.85f, new Color(1f, 0.35f, 0.1f, 0.9f), 2).transform;
        var exit = Rect(root.transform, "Exit", new Vector2(4.5f, 2.6f), new Vector2(1.4f, 1.4f), new Color(0.25f, 0.95f, 0.45f), 3);
        Rect(exit.transform, "ExitSign", new Vector2(0f, 0.9f), new Vector2(1.1f, 0.35f), new Color(0.1f, 0.6f, 0.25f), 4);
        var player = Square(root.transform, "Player", new Vector2(-4.5f, -2.5f), 0.65f, new Color(0.4f, 0.75f, 1f), 5);
        SaveStage("Stage-SmokeExit", root, so =>
        {
            Bind(so, "player", player.transform);
            Bind(so, "exitPoint", exit.transform);
            BindArray(so, "hazards", hazards);
        });
    }

    private static void BuildCrate(Spawnables s)
    {
        var root = NewStageRoot("Stage-StunShoot", typeof(StunShootStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 9f), new Color(0.12f, 0.1f, 0.12f), -3);
        Rect(root.transform, "Arena", Vector2.zero, new Vector2(11f, 7.5f), new Color(0.2f, 0.16f, 0.18f), -2);
        var player = Square(root.transform, "Player", Vector2.zero, 0.7f, new Color(0.35f, 0.75f, 1f), 4);
        var aim = Empty(player.transform, "AimPivot");
        Rect(aim.transform, "Barrel", new Vector2(0.55f, 0f), new Vector2(0.7f, 0.18f),
            new Color(1f, 0.85f, 0.3f), 5);
        var spawn = Empty(root.transform, "SpawnParent");
        SaveStage("Stage-StunShoot", root, so =>
        {
            Bind(so, "player", player.transform);
            Bind(so, "aimPivot", aim.transform);
            Bind(so, "spawnParent", spawn.transform);
            Bind(so, "bulletPrefab", s.Bullet);
            Bind(so, "enemyPrefab", s.Enemy);
        });
    }

    private static void BuildBalance()
    {
        var root = NewStageRoot("Stage-BalanceIce", typeof(BalanceIceStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 8f), new Color(0.55f, 0.75f, 0.9f), -3);
        var platform = Rect(root.transform, "Ice", Vector2.zero, new Vector2(5f, 0.55f), new Color(0.75f, 0.9f, 1f), 0);
        var safe = Rect(platform.transform, "Safe", new Vector2(0f, 0.7f), new Vector2(1.1f, 0.25f), new Color(0.2f, 0.85f, 0.45f), 1);
        var marker = Square(root.transform, "Marker", new Vector2(0f, 0.55f), 0.55f, new Color(1f, 0.85f, 0.2f), 3);
        SaveStage("Stage-BalanceIce", root, so =>
        {
            Bind(so, "platform", platform.transform);
            Bind(so, "safeZone", safe.transform);
            Bind(so, "marker", marker.transform);
        });
    }

    private static void BuildCpr(Spawnables s)
    {
        var root = NewStageRoot("Stage-CprRhythm", typeof(CprRhythmStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(12f, 8f), new Color(0.18f, 0.18f, 0.22f), -3);
        Rect(root.transform, "Bar", Vector2.zero, new Vector2(7f, 0.55f), new Color(0.35f, 0.35f, 0.4f), 0);
        var left = Empty(root.transform, "BarLeft");
        left.transform.localPosition = new Vector3(-3.2f, 0f, 0f);
        var right = Empty(root.transform, "BarRight");
        right.transform.localPosition = new Vector3(3.2f, 0f, 0f);
        var zoneX = Mathf.Lerp(-3.2f, 3.2f, (0.62f + 0.82f) * 0.5f);
        var zone = Rect(root.transform, "Zone", new Vector2(zoneX, 0f), new Vector2(7f * 0.2f, 0.7f), new Color(0.25f, 0.8f, 0.4f, 0.85f), 1);
        var needle = Rect(root.transform, "Needle", Vector2.zero, new Vector2(0.18f, 1.1f), Color.white, 3);
        Square(root.transform, "Chest", new Vector2(0f, -2.2f), 1.6f, new Color(0.95f, 0.75f, 0.7f), 0);
        var spawn = Empty(root.transform, "SpawnParent");
        SaveStage("Stage-CprRhythm", root, so =>
        {
            Bind(so, "needle", needle.transform);
            Bind(so, "barLeft", left.transform);
            Bind(so, "barRight", right.transform);
            Bind(so, "zone", zone.transform);
            Bind(so, "spawnParent", spawn.transform);
            Bind(so, "goodFlashPrefab", s.GoodFlash);
            Bind(so, "badFlashPrefab", s.BadFlash);
        });
    }

    private static void BuildDebris(Spawnables s)
    {
        var root = NewStageRoot("Stage-CatchGlass", typeof(DebrisPaddleStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 9f), new Color(0.2f, 0.18f, 0.16f), -3);
        var ground = Rect(root.transform, "GroundLine", new Vector2(0f, -3.6f), new Vector2(13f, 0.25f), new Color(0.35f, 0.3f, 0.28f), 0);
        var paddle = Rect(root.transform, "Paddle", new Vector2(0f, -3f), new Vector2(1.8f, 0.35f), new Color(0.3f, 0.8f, 0.45f), 4);
        var spawn = Empty(root.transform, "SpawnParent");
        SaveStage("Stage-CatchGlass", root, so =>
        {
            Bind(so, "paddle", paddle.transform);
            Bind(so, "spawnParent", spawn.transform);
            Bind(so, "debrisPrefab", s.Cell);
            Bind(so, "groundLine", ground.transform);
        });
    }

    private static void BuildLaser()
    {
        var root = NewStageRoot("Stage-LaserMaze", typeof(LaserMazeStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 9f), new Color(0.08f, 0.08f, 0.12f), -3);
        var exit = Rect(root.transform, "Exit", new Vector2(5f, 2.8f), new Vector2(1.3f, 1.3f), new Color(0.25f, 0.95f, 0.45f), 2);
        var player = Square(root.transform, "Player", new Vector2(-5f, -2.8f), 0.6f, new Color(0.4f, 0.75f, 1f), 4);
        var specs = new (Vector2 pos, Vector2 size)[]
        {
            (new Vector2(-2f, 0f), new Vector2(0.2f, 3.2f)),
            (new Vector2(0.5f, -1.5f), new Vector2(3.2f, 0.2f)),
            (new Vector2(2.5f, 1.2f), new Vector2(0.2f, 3.2f)),
            (new Vector2(-0.8f, 2f), new Vector2(3.2f, 0.2f)),
            (new Vector2(1.2f, -2.6f), new Vector2(0.2f, 3.2f)),
        };
        var lasers = new SpriteRenderer[specs.Length];
        for (var i = 0; i < specs.Length; i++)
            lasers[i] = GetSprite(Rect(root.transform, $"Laser{i}", specs[i].pos, specs[i].size, new Color(1f, 0.15f, 0.2f, 0.9f), 1));
        SaveStage("Stage-LaserMaze", root, so =>
        {
            Bind(so, "player", player.transform);
            Bind(so, "exitPoint", exit.transform);
            BindArray(so, "lasers", lasers);
        });
    }

    private static void BuildSwing(Spawnables s)
    {
        var root = NewStageRoot("Stage-SwingDodge", typeof(KnifeHitStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 9f), new Color(0.16f, 0.14f, 0.12f), -3);
        Rect(root.transform, "Floor", new Vector2(0f, -3.6f), new Vector2(13f, 1.2f), new Color(0.28f, 0.24f, 0.2f), -1);
        var log = Square(root.transform, "Log", new Vector2(0f, 1.4f), 2.8f, new Color(0.55f, 0.35f, 0.18f), 2);
        Rect(log.transform, "Core", Vector2.zero, new Vector2(0.7f, 0.7f), new Color(0.35f, 0.22f, 0.1f), 3);
        Rect(log.transform, "Mark", new Vector2(0f, 1.1f), new Vector2(0.35f, 0.55f), new Color(0.75f, 0.5f, 0.25f), 4);
        var ready = Rect(root.transform, "ReadyKnife", new Vector2(0f, -3.2f), new Vector2(0.22f, 0.95f),
            new Color(0.85f, 0.85f, 0.9f), 5);
        var knifeCount = CounterText(root.transform, "KnifeCountText", new Vector2(0f, 3.6f));
        var spawn = Empty(root.transform, "SpawnParent");
        SaveStage("Stage-SwingDodge", root, so =>
        {
            Bind(so, "log", log.transform);
            Bind(so, "readyKnife", ready.transform);
            Bind(so, "spawnParent", spawn.transform);
            Bind(so, "knifePrefab", s.Bullet);
            Bind(so, "knifeCountText", knifeCount);
        });
    }

    private static void BuildAlarmSimon(Spawnables s)
    {
        var root = NewStageRoot("Stage-WhackHazard", typeof(HazardWhackStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(12f, 8f), new Color(0.14f, 0.12f, 0.1f), -3);
        Rect(root.transform, "Table", new Vector2(0f, -0.2f), new Vector2(10f, 5.2f), new Color(0.32f, 0.24f, 0.16f), -1);

        var holes = new Transform[6];
        for (var i = 0; i < 6; i++)
        {
            var col = i % 3;
            var row = i / 3;
            var hole = Rect(root.transform, $"Hole{i}", new Vector2(-2.6f + col * 2.6f, 1.1f - row * 2.2f),
                new Vector2(1.6f, 0.7f), new Color(0.12f, 0.1f, 0.08f), 1);
            holes[i] = hole.transform;
        }

        RectScaled(root.transform, "ProgressTrack", new Vector2(0f, -3.4f), new Vector2(4.2f, 0.25f),
            new Color(0.2f, 0.18f, 0.16f), 1);
        var fill = RectScaled(root.transform, "ProgressFill", new Vector2(-2f, -3.4f), new Vector2(0.08f, 0.22f),
            new Color(0.95f, 0.4f, 0.25f), 2);

        SaveStage("Stage-WhackHazard", root, so =>
        {
            BindArray(so, "holes", holes);
            Bind(so, "progressFill", fill.transform);
            Bind(so, "hazardPrefab", s.Danger);
        });
    }

    private static void BuildElevator()
    {
        var root = NewStageRoot("Stage-ElevatorPick", typeof(ElevatorPickStage));
        Rect(root.transform, "Lobby", Vector2.zero, new Vector2(13f, 8f), new Color(0.2f, 0.22f, 0.25f), -3);
        Rect(root.transform, "Floor", new Vector2(0f, -3.2f), new Vector2(13f, 1.2f), new Color(0.35f, 0.35f, 0.38f), -2);
        var safe = Random.Range(0, 3);
        var elevators = new MiniGameTarget[3];
        for (var i = 0; i < 3; i++)
        {
            var isSafe = i == safe;
            var color = isSafe ? new Color(0.55f, 0.7f, 0.85f) : new Color(0.55f, 0.25f, 0.25f);
            var go = Rect(root.transform, $"Elevator{i}", new Vector2(-3.2f + i * 3.2f, 0f), new Vector2(2.2f, 3.6f), color, 1);
            Rect(go.transform, "Panel", new Vector2(0.7f, 0.2f), new Vector2(0.25f, 0.8f), new Color(0.15f, 0.15f, 0.18f), 2);
            Rect(go.transform, "Light", new Vector2(0f, 1.5f), new Vector2(0.45f, 0.25f),
                isSafe ? new Color(0.3f, 0.95f, 0.4f) : new Color(0.95f, 0.25f, 0.2f), 3);
            elevators[i] = AddTarget(go, isSafe, new Vector2(2.2f, 3.6f));
        }
        SaveStage("Stage-ElevatorPick", root, so => BindArray(so, "elevators", elevators));
    }

    private static void BuildKit(Spawnables s)
    {
        var root = NewStageRoot("Stage-Disinfect", typeof(GermShakeStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(12f, 8f), new Color(0.12f, 0.16f, 0.18f), -3);
        var surface = Square(root.transform, "Surface", Vector2.zero, 3.2f, new Color(0.95f, 0.85f, 0.75f), 1);
        Rect(surface.transform, "PalmLine", new Vector2(0f, -0.4f), new Vector2(1.8f, 0.12f),
            new Color(0.85f, 0.7f, 0.6f), 2);
        var germRoot = Empty(surface.transform, "GermRoot");
        RectScaled(root.transform, "ProgressTrack", new Vector2(0f, -3.3f), new Vector2(4.2f, 0.25f),
            new Color(0.2f, 0.22f, 0.24f), 1);
        var fill = RectScaled(root.transform, "ProgressFill", new Vector2(-2f, -3.3f), new Vector2(0.08f, 0.22f),
            new Color(0.4f, 0.9f, 0.45f), 2);
        SaveStage("Stage-Disinfect", root, so =>
        {
            Bind(so, "surface", surface.transform);
            Bind(so, "germRoot", germRoot.transform);
            Bind(so, "progressFill", fill.transform);
            Bind(so, "germPrefab", s.Mist);
        });
    }

    private static void BuildGap(Spawnables s)
    {
        var root = NewStageRoot("Stage-TurretDefend", typeof(GapJumpStage));
        Rect(root.transform, "BG", Vector2.zero, new Vector2(13f, 9f), new Color(0.12f, 0.14f, 0.16f), -3);
        var ground = Empty(root.transform, "GroundRoot");
        var player = Square(root.transform, "Player", new Vector2(-5.5f, -1.2f), 0.55f, new Color(0.35f, 0.75f, 1f), 6);
        var goal = Square(root.transform, "Goal", new Vector2(5.8f, -1.2f), 0.9f, new Color(0.3f, 0.9f, 0.45f), 5);
        SaveStage("Stage-TurretDefend", root, so =>
        {
            Bind(so, "player", player.transform);
            Bind(so, "groundRoot", ground.transform);
            Bind(so, "segmentPrefab", s.Cell);
            Bind(so, "goal", goal.transform);
        });
    }

    private static void BuildBrace()
    {
        var root = NewStageRoot("Stage-BraceDoor", typeof(BraceDoorStage));
        Rect(root.transform, "Hall", Vector2.zero, new Vector2(12f, 8f), new Color(0.16f, 0.16f, 0.18f), -3);
        Rect(root.transform, "Frame", Vector2.zero, new Vector2(3.2f, 4.4f), new Color(0.4f, 0.3f, 0.22f), 0);
        var door = Rect(root.transform, "Door", Vector2.zero, new Vector2(2.6f, 4f), new Color(0.55f, 0.4f, 0.28f), 2);
        Rect(door.transform, "Handle", new Vector2(0.8f, 0f), new Vector2(0.25f, 0.55f), new Color(0.85f, 0.75f, 0.3f), 3);
        Rect(root.transform, "BarBG", new Vector2(0f, -3.2f), new Vector2(6.2f, 0.45f), new Color(0.25f, 0.25f, 0.28f), 1);
        var bar = RectScaled(root.transform, "BraceBar", new Vector2(-3f, -3.2f), new Vector2(0.2f, 0.35f), new Color(0.3f, 0.85f, 0.45f), 2);
        SaveStage("Stage-BraceDoor", root, so =>
        {
            Bind(so, "door", door.transform);
            Bind(so, "braceBar", bar.transform);
        });
    }
}
#endif
