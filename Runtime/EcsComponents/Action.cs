using System;
using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase
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
            return World.Active.EntityManager.Exists(Target);
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
        /// <summary>
        /// Indicate if the weapon use energy instead of standard ammunition.
        /// Energy: Automatic reload over time, 'Value' 'Usage' 'Max' use time based values.
        /// Standard Ammunition: Need to be manually reloaded by the user, 'Value' 'Usage' 'Max' use concrete values.
        /// </summary>
        /// <example>
        /// Energy:
        /// - Usage = 1000
        /// - Max = 2000
        /// >> You'll need to wait 2 seconds for this weapon to charge, firing it will remove 50% of the weapon ammo.
        ///
        /// Standard:
        /// - Usage: 1
        /// - Max: 2
        /// >> You'll need to fully reload it to use it, once it's done, you'll get 2 uses of it, then you'll need to reload it again.
        /// </example>
        public bool IsEnergyBased;

        /// <summary>
        /// (Usable if it's not energy based) If enabled, instead of waiting for the weapon to be reloaded fully,
        /// you are able to reload per each ammo.
        /// </summary>
        public bool ReloadPerRound;

        /// <summary>
        /// Are we currently reloading? Automatically set to false if the weapon is energy based (<see cref="IsEnergyBased"/>)
        /// </summary>
        public bool IsReloading;
        public int TimeToReload;
        
        public int Value;
        public int Usage;
        public int Max;

        public ActionAmmo(int usage, int max, int value)
        {
            Value = value;
            Usage = usage;
            Max   = max;
            
            IsEnergyBased  = true;
            ReloadPerRound = false;
            IsReloading    = false;
            TimeToReload   = 0;
        }

        public ActionAmmo(int usage, int max) : this(usage, max, 0)
        {
        }

        // GetShootLeft + IsReloading
        public bool CanShoot()
        {
            var left = GetShootLeft();
            if (left < 0)
                return false;

            if (!IsEnergyBased && ReloadPerRound && IsReloading && Usage != Max)
                return false;

            return true;
        }

        public int GetMaxShoot()
        {
            if (Max <= 0)
                return 0;
            if (Usage <= 0)
                return 1;
            
            var usage = math.max(Usage, 1);
            var max = math.max(Max, 1);

            return max / usage;
        }
        
        public int GetShootLeft()
        {
            if (Max <= 0 || Value <= 0)
                return 0;
            if (Usage <= 0)
                return 1;
            
            var usage = math.max(Usage, 1);
            var value   = math.max(Value, 1);

            return value / usage;
        }

        public void IncreaseFromDelta(int delta)
        {
            Value += delta;
            Value = math.clamp(Value, 0, Max);
        }

        public void ModifyAmmo(int newVal)
        {
            Value = math.clamp(newVal, 0, Max);
        }
    }

    public struct ActionCooldown : IComponentData
    {
        public UTick StartTick;
        public uint Cooldown;

        public ActionCooldown(UTick startTick)
        {
            StartTick = startTick;
            Cooldown = 0;
        }

        public ActionCooldown(UTick startTick, uint cooldown) : this(startTick)
        {
            Cooldown = cooldown;
        }

        public bool CooldownFinished(UTick tick)
        {
            return StartTick <= 0 || tick > StartTick + UTick.MsToTickNextFrame(tick, Cooldown);
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