using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace StormiumTeam.GameBase
{
	public interface IFilter
	{
		BufferFromEntity<CollideWith> CollideWithFromEntity { get; set; }
		NativeArray<Entity>           Targets               { get; set; }
		PhysicsWorld                  PhysicsWorld          { get; set; }
	}

	public interface ICollectorFilter
	{
		bool Filter(int rbIndex);
	}

	public class Filter
	{
		
	}
}