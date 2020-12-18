using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using StormiumTeam.GameBase.Utility.Modules;
using Unity.Jobs;

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

		public bool Add<THandle, TData>(UniTask<THandle> handle, TData data)
			where TData : struct
		{
			Handles.Add(new HandleDataPair<THandle, TData>
			{
				Handle = handle.AsTask(),
				Data   = data
			});

			return true;
		}

		public HandleDataPair<THandle, TData> Get<THandle, TData>(int index)
			where TData : struct
		{
			return (HandleDataPair<THandle, TData>) Handles[index];
		}

		public class BaseHandleDataPair
		{
			public Task Handle;
		}

		public class HandleDataPair<THandle, TData> : BaseHandleDataPair
			where TData : struct
		{
			public TData         Data;
			public Task<THandle> Generic => (Task<THandle>) Handle;

			public void Deconstruct(out Task<THandle> handle, out TData data)
			{
				handle = Handle != null ? Generic : default;
				data   = Data;
			}
		}
	}
}