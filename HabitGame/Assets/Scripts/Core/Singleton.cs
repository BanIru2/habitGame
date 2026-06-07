using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    private static bool isQuitting = false;

    public static T Instance
    {
        get
        {
            if (isQuitting) return null;

            if (instance == null)
                SetupInstance();

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit() => isQuitting = true;

    protected virtual void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private static void SetupInstance()
    {
        instance = FindObjectOfType<T>();
        if (instance != null) return;

        var go = new GameObject(typeof(T).Name);
        instance = go.AddComponent<T>();
        DontDestroyOnLoad(go);
    }
}