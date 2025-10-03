using UnityEngine;
using System;

public class CookiesInventory : MonoBehaviour
{
    public static CookiesInventory Instance { get; private set; }
    public int cookies = 0;

    public event Action<int> OnChanged; // ★ 变化事件

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        OnChanged?.Invoke(cookies);
    }

    public void Add(int v)
    {
        cookies += v;
        OnChanged?.Invoke(cookies);
    }

    public bool Spend(int v)
    {
        if (cookies < v) return false;
        cookies -= v;
        OnChanged?.Invoke(cookies);
        return true;
    }
}
