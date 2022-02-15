using UnityEngine;

public class ValueSetter : MonoBehaviour
{
    [SerializeField] internal BackPack Pack;
    [SerializeField] internal ZNetView m_nview;
    
    
    private void Awake()
    {
        Pack.originalDrop = this.gameObject;
        Pack.ItemDataref = gameObject.GetComponent<ItemDrop>();
    }
    
    public string mGuid = System.Guid.NewGuid().ToString();
}
