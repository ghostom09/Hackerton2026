using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (!GUILayout.Button("Collect All OrderSOs"))
            return;

        var guids = AssetDatabase.FindAssets("t:OrderSO");
        var orders = new List<OrderSO>(guids.Length);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var order = AssetDatabase.LoadAssetAtPath<OrderSO>(path);
            if (order != null)
                orders.Add(order);
        }

        orders.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        var gameManager = (GameManager)target;
        Undo.RecordObject(gameManager, "Collect All OrderSOs");
        gameManager.SetAllOrders(orders.ToArray());
        EditorUtility.SetDirty(gameManager);

        Debug.Log($"[GameManager] OrderSO {orders.Count}개 수집 완료.");
    }
}
