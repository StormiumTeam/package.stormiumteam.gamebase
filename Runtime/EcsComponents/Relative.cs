using Unity.Entities;

namespace EcsComponents
{
    public struct Relative<T> : IComponentData
    {
        public Entity Target;
    }
}