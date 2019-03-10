using Unity.Entities;

namespace package.stormium.core
{
    public interface IProjectile
    {
        
    }

    public struct ProjectileTag : IComponentData
    {
        
    }

    public struct ProjectileOwner : IComponentData
    {
        public Entity Target;

        public ProjectileOwner(Entity entity)
        {
            Target = entity;
        }

        public bool TargetValid()
        {
            return World.Active.GetExistingManager<EntityManager>().Exists(Target);
        }
    }

    public struct ProjectileSpeed : IComponentData
    {
        public float Value;

        public ProjectileSpeed(float value)
        {
            Value = value;
        }
    }

    public struct ProjectileCmdVelocityChange : IComponentData
    {
        
    }

    public struct ProjectileCmdVelocityBump : IComponentData
    {
        
    }

    public struct ProjectileCmdOnHit : IComponentData
    {
        
    }

    public struct ProjectileCmdOnDirectHit : IComponentData
    {
        
    }
}