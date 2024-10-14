using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	[SerializeField] bool dontDestroyOnLoad;

	private static T instance;
	public static T Instance => instance;
	private void Awake()
	{
		var t = typeof(T).Name;
		if (instance != null)
			Destroy(gameObject);
		else
		{
			instance = this as T;
			OnAwake();
			if (dontDestroyOnLoad)
				DontDestroyOnLoad(gameObject);
		}
	}

	protected virtual void OnAwake() { }
}
