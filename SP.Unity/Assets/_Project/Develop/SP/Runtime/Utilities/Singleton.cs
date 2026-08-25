using UnityEngine;

namespace SP.Runtime.Utilities
{
	public class Singleton<T> : MonoBehaviour where T : Singleton<T>
	{
		private static T _instance;
		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					if (FindObjectsOfType(typeof(T)) is T[] instances && instances.Length != 0)
					{
						if (instances.Length == 1)
						{
							_instance = instances[0];
							_instance.gameObject.name = typeof(T).Name;
							
							return _instance;
						}
						else
						{
							Debug.LogError("Class " + typeof(T).Name + " exists multiple times in violation of singleton pattern. Destroying all copies");
							
							for (var i = instances.Length - 1; i >= 0; i--)
							{
								Destroy(instances[i]);
							}
						}
					}
					
					// var go = new GameObject(typeof(T).Name, typeof(T));
					// _instance = go.GetComponent<T>();
					// DontDestroyOnLoad(go);
				}
				
				return _instance;
			}
		}
	}
}