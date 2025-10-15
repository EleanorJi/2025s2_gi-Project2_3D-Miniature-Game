using System.Collections.Generic;
using UnityEngine;

public class SimpleInfoSession : MonoBehaviour
{
    public static SimpleInfoSession I
    {
        get
        {
            if (_i == null)
            {
                var go = new GameObject("~SimpleInfoSession");
                _i = go.AddComponent<SimpleInfoSession>();
                DontDestroyOnLoad(go);
            }
            return _i;
        }
    }
    static SimpleInfoSession _i;

    HashSet<string> shown = new HashSet<string>(); // 本次运行已展示过的ID

    public bool Has(string id) => !string.IsNullOrEmpty(id) && shown.Contains(id);
    public void MarkShown(string id)
    {
        if (!string.IsNullOrEmpty(id)) shown.Add(id);
    }
}
