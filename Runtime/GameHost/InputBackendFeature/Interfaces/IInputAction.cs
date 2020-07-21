using RevolutionSnapshot.Core.Buffers;
using Unity.Entities;

namespace GameHost.InputBackendFeature.Interfaces
{
	public interface IInputAction : IComponentData
	{
		void Serialize(ref   DataBufferWriter buffer);
		void Deserialize(ref DataBufferReader buffer);
	}
}