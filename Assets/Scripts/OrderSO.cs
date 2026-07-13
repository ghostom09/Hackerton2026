using UnityEngine;

public class OrderSO : ScriptableObject
{
    public string orderName;
    public GameObject roomPrefab;
    
    [TextArea(3, 8)]
    public string requestDialogue;
}
