using System;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.core
{
    public struct StActionContainer : IBufferElementData
    {
        public Entity Target;

        public StActionContainer(Entity actionTarget)
        {
            Target = actionTarget;
        }

        public bool TargetValid()
        {
            return World.Active.GetExistingManager<EntityManager>().Exists(Target);
        }
    }

    public struct StActionTag : IComponentData
    {
        public int ActionTypeIndex;
        
        public StActionTag(int actionTypeIndex)
        {
            ActionTypeIndex = actionTypeIndex;
        }

        public Type GetActionType()
        {
            return TypeManager.GetType(ActionTypeIndex);
        }
    }

    /// <summary>
    /// (Recommended) Use it if you don't want the order of the action container
    /// </summary>
    public struct StActionSlot : IComponentData
    {
        public int Value;

        public StActionSlot(int slot)
        {
            Value = slot;
        }

        public bool IsValid()
        {
            return Value >= 0;
        }

        public bool IsHidden()
        {
            return Value == -1;
        }
    }

    public struct StActionAmmo : IComponentData
    {
        public int Value;
        public int Usage;
        public int Max;

        public StActionAmmo(int usage, int max)
        {
            Value = 0;
            Usage = usage;
            Max = max;
        }
        
        public StActionAmmo(int usage, int max, int value)
        {
            Value      = value;
            Usage = usage;
            Max   = max;
        }

        public int GetRealAmmo()
        {
            if (Max <= 0)
                return 0;
            if (Usage <= 0)
                return 1;
            
            var usage = math.max(Usage, 1);
            var max = math.max(Max, 1);

            return max / usage;
        }

        public void IncreaseFromDelta(int deltaTick)
        {
            Value += deltaTick;
            Value = math.clamp(Value, 0, Max);
        }

        public void ModifyAmmo(int newVal)
        {
            Value = math.clamp(newVal, 0, Max);
        }
    }

    public struct StActionCooldown : IComponentData
    {
        public int StartTick;
        public int Cooldown;

        public StActionCooldown(int startTick)
        {
            StartTick = startTick;
            Cooldown = -1;
        }

        public StActionCooldown(int startTick, int cooldown) : this(startTick)
        {
            Cooldown = cooldown;
        }

        public bool CooldownFinished(int tick)
        {
            return StartTick <= 0 || tick > StartTick + Cooldown;
        }
    }

    public struct StActionSimpleFire : IComponentData
    {
    }

    public struct StActionDualSwitch : IComponentData
    {
        public Entity PrimaryTarget;
        public Entity SecondaryTarget;

        public Entity this[int index] => index == 1 ? SecondaryTarget : PrimaryTarget;

        public StActionDualSwitch(Entity primary, Entity secondary)
        {
            PrimaryTarget = primary;
            SecondaryTarget = secondary;
        }
    }
}