using UnityEngine;

public class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // Public static property that other scripts will use to access the instance
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        // 1. Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            // 2. If it does and it's not this one, destroy the duplicate
            Destroy(gameObject);
        }
        else
        {
            // 3. Set the instance and make the GameObject persistent
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}