namespace SP.Runtime.Core.Services.PoolService
{
	public class ObjectPoolContainer<T>
	{
		public ObjectPoolContainer(T obj)
		{
			Object = obj;
		}

		public bool Used { get; private set; }
		public T Object { get; }

		public void Consume()
		{
			Used = true;
		}

		public void Release()
		{
			Used = false;
		}
	}
}
