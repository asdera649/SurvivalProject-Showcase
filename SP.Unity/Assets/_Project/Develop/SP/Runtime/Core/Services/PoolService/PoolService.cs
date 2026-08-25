using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SP.Runtime.Core.Services.PoolService
{
	public class PoolService : MonoBehaviour
	{
		#region Structs
		
		[Serializable]
		public class PoolElement
		{
			[SerializeField] private Object _prefab;
			public Object Prefab => _prefab;
			
			[SerializeField] private int _size;
			public int Size => _size;
			
			[SerializeField] private bool _dontCreateNew;
			public bool DontCreateNew => _dontCreateNew;
		}
		
		#endregion

		[Header("Settings")]
		[SerializeField] private PoolElement[] _poolElements;
		
		private readonly Dictionary<Object, ObjectPool<Object>> _prefabLookup = new();
		private readonly Dictionary<Object, ObjectPool<Object>> _instanceLookup = new(); 

		private void Awake()
		{
			Initialize();
		}

		private void Initialize()
		{
			foreach (var e in _poolElements)
			{
				if (_prefabLookup.ContainsKey(e.Prefab))
				{
					Debug.LogWarning("Pool for prefab " + e.Prefab.name + " has already been created");
					continue;
				}

				var pool = new ObjectPool<Object>(() => InstantiatePrefab(e.Prefab), e.Size, e.DontCreateNew);
				
				_prefabLookup[e.Prefab] = pool;
			}
		}

		public bool TrySpawnObject<T>(T prefab, Vector3 position, Quaternion rotation, out T output) where T: Object
		{
			output = null;

			if (_prefabLookup.TryGetValue(prefab, out var pool))
			{
				if (pool.TryGetItem(out var item))
				{
					output = (T)item;
					
					if (TryGetGameObject(item, out var gameObj))
					{
						gameObj.transform.SetPositionAndRotation(position, rotation);
						gameObj.SetActive(true);
					}

					_instanceLookup.Add(item, pool);
				}
			}

			return output != null;
		}

		public void ReleaseObject(Object releasedObject)
		{
			if (!_instanceLookup.ContainsKey(releasedObject))
			{
				return;
			}

			if (TryGetGameObject(releasedObject, out var gameObj))
			{
				gameObj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
				gameObj.SetActive(false);
			}

			_instanceLookup[releasedObject].ReleaseItem(releasedObject);
			_instanceLookup.Remove(releasedObject);
		}
		
		#region Utilities
		
		private T InstantiatePrefab<T>(T prefab) where T: Object
		{
			var output = Instantiate(prefab, transform, true);
			
			output.name = prefab.name;

			if (TryGetGameObject(output, out var gameObj))
			{
				gameObj.transform.localPosition = Vector3.zero;
				gameObj.SetActive(false);
			}

			return output;
		}
		
		private bool TryGetGameObject(Object obj, out GameObject output)
		{
			output = null;
                    
			if (obj is GameObject gameObj)
			{
				output = gameObj;
			}
			else if (obj is Component component)
			{
				output = component.gameObject;
			}

			return output != null;
		}
		
		#endregion
	}
}


