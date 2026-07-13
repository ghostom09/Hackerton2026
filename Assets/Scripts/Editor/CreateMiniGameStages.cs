#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateMiniGameStages
{
    private const string PrefabFolder = "Assets/Prefab";
    private const string DataFolder = "Assets/Data";

    private static readonly StageDef[] Stages =
    {
        new("Stage-GasValve", "Order7", typeof(GasValveStage), 8f,
            "<type charDelay=0.06><rumble strength=2><color r=255 g=180 b=80>가스 밸브를 잠그세요!</color></rumble></type>"),
        new("Stage-MeteorShoot", "Order8", typeof(SnakeEscapeStage), 20f,
            "<type charDelay=0.06><jump height=5 speed=2.5><color r=255 g=120 b=60>터널에서 키트를 모으며 탈출하세요!</color></jump></type>"),
        new("Stage-PoisonPick", "Order9", typeof(PoisonPickStage), 8f,
            "<type charDelay=0.06><wiggle speed=5 angle=8><color r=120 g=220 b=255>오염수를 피하고 안전한 물을 고르세요</color></wiggle></type>"),
        new("Stage-RoadCross", "Order10", typeof(RoadCrossStage), 12f,
            "<type charDelay=0.06><bounce amplitude=5 frequency=4><color r=255 g=220 b=80>차량을 피해 길을 건너세요</color></bounce></type>"),
        new("Stage-FuseBox", "Order11", typeof(FuseBoxStage), 12f,
            "<type charDelay=0.06><pulse speed=3 intensity=0.12><color r=255 g=230 b=100>정전! 퓨즈 순서를 기억하세요</color></pulse></type>"),
        new("Stage-ShieldBlock", "Order12", typeof(SandbagStackStage), 20f,
            "<type charDelay=0.06><rumble strength=1><color r=180 g=220 b=255>모래주머니로 홍수를 막으세요!</color></rumble></type>"),
        new("Stage-WireCut", "Order13", typeof(WireCutStage), 7f,
            "<type charDelay=0.06><jitter amount=2><color r=255 g=90 b=90>폭발물! 힌트 색 전선을 자르세요</color></jitter></type>"),
        new("Stage-LeakPatch", "Order14", typeof(LeakSlingStage), 20f,
            "<type charDelay=0.06><drip><color r=80 g=180 b=255>새총으로 패치를 날려 누수를 막으세요!</color></drip></type>"),
        new("Stage-SmokeExit", "Order15", typeof(SmokeExitStage), 12f,
            "<type charDelay=0.06><spin><color r=120 g=255 b=140>연기 속을 지나 비상구로 탈출하세요</color></spin></type>"),
        new("Stage-StunShoot", "Order16", typeof(StunShootStage), 20f,
            "<type charDelay=0.06><zoom><color r=255 g=100 b=100>조준해서 접근하는 위험을 기절시키세요!</color></zoom></type>"),
        new("Stage-BalanceIce", "Order17", typeof(BalanceIceStage), 10f,
            "<type charDelay=0.06><wiggle speed=5 angle=8><color r=180 g=230 b=255>빙판에서 균형을 유지하세요</color></wiggle></type>"),
        new("Stage-CprRhythm", "Order18", typeof(CprRhythmStage), 12f,
            "<type charDelay=0.06><pulse speed=3 intensity=0.12><color r=255 g=120 b=140>심폐소생! 리듬에 맞춰 누르세요</color></pulse></type>"),
        new("Stage-CatchGlass", "Order19", typeof(DebrisPaddleStage), 20f,
            "<type charDelay=0.06><bounce amplitude=5 frequency=4><color r=160 g=220 b=255>낙하물을 그물로 받아내세요!</color></bounce></type>"),
        new("Stage-LaserMaze", "Order20", typeof(LaserMazeStage), 12f,
            "<type charDelay=0.06><flicker><color r=255 g=80 b=100>위험 레이저를 피해 탈출하세요</color></flicker></type>"),
        new("Stage-SwingDodge", "Order21", typeof(KnifeHitStage), 25f,
            "<type charDelay=0.06><swing><color r=220 g=200 b=120>회전하는 통나무에 검을 꽂으세요!</color></swing></type>"),
        new("Stage-WhackHazard", "Order22", typeof(HazardWhackStage), 25f,
            "<type charDelay=0.06><jitter><color r=255 g=90 b=90>튀어나오는 위험을 빠르게 쳐잡으세요!</color></jitter></type>"),
        new("Stage-ElevatorPick", "Order23", typeof(ElevatorPickStage), 7f,
            "<type charDelay=0.06><blink><color r=120 g=255 b=160>안전한 엘리베이터를 고르세요</color></blink></type>"),
        new("Stage-Disinfect", "Order24", typeof(KitCollectStage), 25f,
            "<type charDelay=0.06><drip><color r=100 g=210 b=255>키트를 모으고 위험을 피하세요!</color></drip></type>"),
        new("Stage-TurretDefend", "Order25", typeof(GapJumpStage), 20f,
            "<type charDelay=0.06><zoom><color r=255 g=160 b=60>무너지는 다리를 뛰어 건너세요</color></zoom></type>"),
        new("Stage-BraceDoor", "Order26", typeof(BraceDoorStage), 10f,
            "<type charDelay=0.06><rumble><color r=220 g=180 b=100>침입을 막고 문을 버텨내세요!</color></rumble></type>"),
    };

    [MenuItem("Hackerton/Create MiniGame Stages (Order7-26)")]
    public static void CreateAll()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(DataFolder);

        foreach (var stage in Stages)
            CreateStage(stage);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CollectIntoGameManager();
        RebuildMiniGameObjectPrefabs.RebuildAll();
        Debug.Log("[CreateMiniGameStages] Order7~26 스테이지 생성 및 GameObject 프리팹 구성 완료.");
    }

    private static void CreateStage(StageDef stage)
    {
        var prefabPath = $"{PrefabFolder}/{stage.PrefabName}.prefab";
        var orderPath = $"{DataFolder}/{stage.OrderName}.asset";

        var root = new GameObject(stage.PrefabName);
        root.AddComponent(stage.StageType);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        var order = AssetDatabase.LoadAssetAtPath<OrderSO>(orderPath);
        if (order == null)
        {
            order = ScriptableObject.CreateInstance<OrderSO>();
            AssetDatabase.CreateAsset(order, orderPath);
        }

        order.orderDialog = stage.Dialog;
        order.roomPrefab = prefab;
        order.time = stage.Time;
        EditorUtility.SetDirty(order);
    }

    private static void CollectIntoGameManager()
    {
        var guids = AssetDatabase.FindAssets("t:OrderSO");
        var orders = new System.Collections.Generic.List<OrderSO>(guids.Length);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var order = AssetDatabase.LoadAssetAtPath<OrderSO>(path);
            if (order != null)
                orders.Add(order);
        }

        orders.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        var managers = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var manager in managers)
        {
            Undo.RecordObject(manager, "Collect MiniGame Orders");
            manager.SetAllOrders(orders.ToArray());
            EditorUtility.SetDirty(manager);
        }

        // Also patch scene assets if GameManager lives only in a scene
        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (var sceneGuid in sceneGuids)
        {
            // Orders are collected at runtime via FindAssets when user presses Collect;
            // scene objects already updated above if loaded.
        }

        Debug.Log($"[CreateMiniGameStages] OrderSO {orders.Count}개 수집 대상.");
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

    private readonly struct StageDef
    {
        public readonly string PrefabName;
        public readonly string OrderName;
        public readonly System.Type StageType;
        public readonly float Time;
        public readonly string Dialog;

        public StageDef(string prefabName, string orderName, System.Type stageType, float time, string dialog)
        {
            PrefabName = prefabName;
            OrderName = orderName;
            StageType = stageType;
            Time = time;
            Dialog = dialog;
        }
    }
}
#endif
