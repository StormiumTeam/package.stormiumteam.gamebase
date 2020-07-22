using System.Collections.Generic;
using StormiumTeam.GameBase.Utility.Modules;
using Unity.Jobs;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace StormiumTeam.GameBase.Modules
{
	public sealed class AsyncOperationModule : BaseSystemModule
	{
		public          List<BaseHandleDataPair> Handles;
		public override ModuleUpdateType         UpdateType => ModuleUpdateType.MainThread;

		protected override void OnEnable()
		{
			Handles = new List<BaseHandleDataPair>(8);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
		}

		protected override void OnDisable()
		{
			Handles.Clear();
		}

		public void Add<THandle, TData>(AsyncOperationHandle<THandle> handle, TData data)
			where TData : struct
		{
			Handles.Add(new HandleDataPair<THandle, TData>
			{
				Handle = handle,
				Data   = data
			});
		}

		public HandleDataPair<THandle, TData> Get<THandle, TData>(int index)
			where TData : struct
		{
			return (HandleDataPair<THandle, TData>) Handles[index];
		}

		public class BaseHandleDataPair
		{
			public AsyncOperationHandle Handle;
		}

		public class HandleDataPair<THandle, TData> : BaseHandleDataPair
			where TData : struct
		{
			public TData                         Data;
			public AsyncOperationHandle<THandle> Generic => Handle.Convert<THandle>();

			public void Deconstruct(out AsyncOperationHandle<THandle> handle, out TData data)
			{
				handle = Generic;
				data   = Data;
			}
		}
	}
}