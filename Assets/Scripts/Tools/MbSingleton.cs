using UnityEngine;

public abstract class MbSingleton<T> : NetDataHandler where T : MbSingleton<T>
{
    private static T m_Instance = null;

    public static T instance
    {
        get
        {
            if (m_Instance == null && !appQuiting)
            {
                m_Instance = GameObject.FindObjectOfType(typeof(T)) as T;
                if (m_Instance == null)
                {
                    print(typeof(T).Name + " got instance");
                    m_Instance = new GameObject("Singleton of " + typeof(T).ToString(), typeof(T)).GetComponent<T>();
                    //DontDestroyOnLoad(m_Instance.gameObject);
                    //Init();
                }

            }
            return m_Instance;
        }
    }

    protected void Awake()
    {
        //		print(this.GetType().Name + " Awake");
        if (m_Instance == null)
        {
            m_Instance = this as T;

        }
        DontDestroyOnLoad(this.gameObject);
        Init();
    }

    public virtual void Init() { }

    static bool appQuiting;
    protected virtual void OnApplicationQuit()
    {
        m_Instance = null;
        appQuiting = true;
    }
}