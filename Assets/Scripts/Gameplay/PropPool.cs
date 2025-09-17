using System.Collections.Generic;
using UnityEngine;

public class PropPool : MonoBehaviour
{
    [SerializeField] private PropView propPrefab;
    [SerializeField] private int initialPoolSize = 50;
    
    private Queue<PropView> _pool = new Queue<PropView>();
    
    private void Start()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var prop = Instantiate(propPrefab, transform);
            prop.gameObject.SetActive(false);
            _pool.Enqueue(prop);
        }
    }

    public PropView Get()
    {
        if (_pool.Count > 0)
        {
            var prop = _pool.Dequeue();
            prop.gameObject.SetActive(true);
            return prop;
        }
        
        return Instantiate(propPrefab, transform);
    }
    
    public void Return(PropView prop)
    {
        prop.gameObject.SetActive(false);
        _pool.Enqueue(prop);
    }
}