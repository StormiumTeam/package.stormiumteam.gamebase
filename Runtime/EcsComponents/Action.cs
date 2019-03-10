using System;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.StormiumTeam.GameBase
{
    public struct ActionContainer : IBufferElementData
    {
        public Entity Target;

        public ActionContainer(Entity actionTarget)
        {
            Target = actionTarget;
        }

        public bool TargetValid()
        {
            return World.Active.GetExistingManager<EntityManager>().Exists(Target);
        }
    }

    public struct ActionTag : IComponentData
    {
        public int ActionTypeIndex;
        
        public ActionTag(int actionTypeIndex)
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
    public struct ActionSlot : IComponentData
    {
        public int Value;

        public ActionSlot(int slot)
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

    public struct ActionAmmo : IComponentData
    {
        public int Value;
        public int Usage;
        public int Max;

        public ActionAmmo(int usage, int max)
        {
            Value = 0;
            Usage = usage;
            Max = max;
        }
        
        public ActionAmmo(int usage, int max, int value)
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

    public struct ActionCooldown : IComponentData
    {
        public int StartTick;
        public int Cooldown;

        public ActionCooldown(int startTick)
        {
            StartTick = startTick;
            Cooldown = -1;
        }

        public ActionCooldown(int startTick, int cooldown) : this(startTick)
        {
            Cooldown = cooldown;
        }

        public bool CooldownFinished(int tick)
        {
            return StartTick <= 0 || tick > StartTick + Cooldown;
        }
    }

    public struct ActionSimpleFire : IComponentData
    {
    }

    public struct ActionDualSwitch : IComponentData
    {
        public Entity PrimaryTarget;
        public Entity SecondaryTarget;

        public Entity this[int index] => index == 1 ? SecondaryTarget : PrimaryTarget;

        public ActionDualSwitch(Entity primary, Entity secondary)
        {
            PrimaryTarget = primary;
            SecondaryTarget = secondary;
        }
    }
}