using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<TeamDescription>))]

namespace StormiumTeam.GameBase
{
	public struct TeamDescription : IEntityDescription
	{
	}
}