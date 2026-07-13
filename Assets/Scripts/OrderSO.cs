using UnityEngine;

[CreateAssetMenu(fileName = "Order", menuName = "Scriptable Objects/Order")]
public class OrderSO : ScriptableObject
{
    [TextArea(3, 10)]
    public string orderDialog;
    public GameObject roomPrefab;
    public float time;
}
