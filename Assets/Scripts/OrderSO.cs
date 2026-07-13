using UnityEngine;

[CreateAssetMenu(fileName = "Order", menuName = "Scriptable Objects/Order")]
public class OrderSO : ScriptableObject
{
    public string orderName;
    public GameObject roomPrefab;
    
    [TextArea(3, 8)]
    public string requestDialogue;
}
