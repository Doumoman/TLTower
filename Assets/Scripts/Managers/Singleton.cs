using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generic Singleton base class. Automatically creates and optionally persists the instance.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : Component
{
    protected static T _instance;

    public static bool HasInstance => _instance != null;
    public static T TryGetInstance() => HasInstance ? _instance : null;
    public static T Current => _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                    Create(true);
            }
            return _instance;
        }
    }

    public static void Create()
    {
        Create(true);
    }

    public static void Create(bool dontDestroy)
    {
        if (_instance == null && Application.isPlaying)
        {
            GameObject obj = new GameObject(typeof(T).Name + "_AutoCreated");
            _instance = obj.AddComponent<T>();
            if (dontDestroy)
                DontDestroyOnLoad(obj);
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }

        InitializeSingleton();
    }

    protected virtual void InitializeSingleton()
    {
        if (!Application.isPlaying) return;

        _instance = this as T;
    }

#if UNITY_EDITOR
    // Automatically clean up the auto-created instance in the Editor
    static Singleton()
    {
        EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
    }

    private static void OnEditorPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && _instance != null)
        {
            GameObject obj = _instance.gameObject;
            if (obj.name.EndsWith("_AutoCreated"))
            {
                Object.DestroyImmediate(obj);
                _instance = null;
            }
        }
    }
#endif
}