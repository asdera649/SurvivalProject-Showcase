using System;
using System.Collections.Generic;

namespace SP.Runtime.Core.Services.PoolService
{
	public class ObjectPool<T>
	{
		private readonly List<ObjectPoolContainer<T>> _objectPoolContainers;
		private readonly Dictionary<T, ObjectPoolContainer<T>> _lookup;
		private readonly Func<T> _factoryFunc;
		
		private readonly bool _dontCreateNew;
		private int _lastIndex;

		public ObjectPool(Func<T> factoryFunc, int size, bool dontCreateNew)
		{
			_objectPoolContainers = new List<ObjectPoolContainer<T>>(size);
			_lookup = new Dictionary<T, ObjectPoolContainer<T>>(size);
			_factoryFunc = factoryFunc;
			
			_dontCreateNew = dontCreateNew;

			Warm(size);
		}

		private void Warm(int size)
		{
			for (var i = 0; i < size; i++)
			{
				CreateContainer();
			}
		}

		private ObjectPoolContainer<T> CreateContainer()
		{
			var container = new ObjectPoolContainer<T>(_factoryFunc());
			
			_objectPoolContainers.Add(container);
			
			return container;
		}

		public bool TryGetItem(out T output)
		{
			output = default;
			
			ObjectPoolContainer<T> container = null;
			
			foreach (var unused in _objectPoolContainers)
			{
				_lastIndex++;

				if (_lastIndex > _objectPoolContainers.Count - 1)
				{
					_lastIndex = 0;
				}

				if (!_objectPoolContainers[_lastIndex].Used)
				{
					container = _objectPoolContainers[_lastIndex];
					break;
				}
			}

			if (container == null && !_dontCreateNew)
			{
				container = CreateContainer();
			}

			if (container != null)
			{
				container.Consume();
				
				_lookup.Add(container.Object, container);
				
				output = container.Object;
			}

			return output != null;
		}

		public void ReleaseItem(T obj)
		{
			if (!_lookup.ContainsKey(obj))
			{
				return;
			}
			
			_lookup[obj].Release();
			_lookup.Remove(obj);
		}
	}
}
