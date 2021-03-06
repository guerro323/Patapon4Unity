using System;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Worlds.Components;

namespace PataNext.CoreAbilities.Mixed
{
    public static class SimpleAttackAbility
    {
        public interface IState : IComponentData
        {
            // Can only attack if Cooldown is passed, and if there are no delay before next attack
            public TimeSpan AttackStart { get; set; }

            // prev: HasThrown, HasSlashed, ...
            public bool DidAttack { get; set; }

            /// <summary>
            /// Cooldown before waiting for the next attack
            /// </summary>
            public TimeSpan Cooldown { get; set; }
        }

        public interface ISettings : IComponentData
        {
            /// <summary>
            /// Delay before the attack (does not include <see cref="Cooldown"/>)
            /// </summary>
            public TimeSpan DelayBeforeAttack { get; }

            /// <summary>
            /// Delay after the attack (does not include <see cref="Cooldown"/>)
            /// </summary>
            public TimeSpan PauseAfterAttack { get; }
        }

        public static void StopAttack<TState>(this ref TState state) where TState : struct, IState
        {
            state.AttackStart = default;
            state.DidAttack   = false;
        }

        public static bool TriggerAttack<TState>(this ref TState state, in WorldTime worldTime) where TState : struct, IState
        {
            if (state.AttackStart == TimeSpan.Zero && state.Cooldown <= TimeSpan.Zero)
            {
                state.AttackStart = worldTime.Total;
                state.DidAttack   = false;
                return true;
            }

            return false;
        }

        public static bool CanAttackThisFrame<TState, TSettings>(this ref TState state, in TSettings settings, in TimeSpan currentTime, TimeSpan cooldown)
            where TState : struct, IState
            where TSettings : struct, ISettings
        {
            if (currentTime > state.AttackStart.Add(settings.DelayBeforeAttack) && !state.DidAttack)
            {
                state.Cooldown  = cooldown;
                state.DidAttack = true;
                return true;
            }

            return false;
        }

        public static bool IsAttackingAndUpdate<TState, TSettings>(this ref TState state, in TSettings settings, in TimeSpan currentTime)
            where TState : struct, IState
            where TSettings : struct, ISettings
        {
            if (state.AttackStart != default)
            {
                if (state.AttackStart.Add(settings.DelayBeforeAttack + settings.PauseAfterAttack) <= currentTime)
                {
                    state.AttackStart = default;
                }

                return true;
            }

            return false;
        }
    }

    [Obsolete]
    public interface ISimpleAttackAbility : IComponentData
    {
        // Can only attack if Cooldown is passed, and if there are no delay before next attack
        public TimeSpan AttackStart { get; set; }

        // prev: HasThrown, HasSlashed, ...
        public bool DidAttack { get; set; }

        /// <summary>
        /// Cooldown before waiting for the next attack
        /// </summary>
        public TimeSpan Cooldown { get; set; }

        /// <summary>
        /// Delay before the attack (does not include <see cref="Cooldown"/>)
        /// </summary>
        public TimeSpan DelayBeforeAttack { get; }

        /// <summary>
        /// Delay after the attack (does not include <see cref="Cooldown"/>)
        /// </summary>
        public TimeSpan PauseAfterAttack { get; }
    }

    [Obsolete]
    public static class SimpleAttackAbilityExtensions
    {
        public static void StopAttack<T>(this ref T impl) where T : struct, ISimpleAttackAbility
        {
            impl.AttackStart = default;
            impl.DidAttack   = false;
        }

        public static bool TriggerAttack<T>(this ref T impl, in WorldTime worldTime) where T : struct, ISimpleAttackAbility
        {
            if (impl.AttackStart == TimeSpan.Zero && impl.Cooldown <= TimeSpan.Zero)
            {
                impl.AttackStart = worldTime.Total;
                impl.DidAttack   = false;
                return true;
            }

            return false;
        }

        public static bool CanAttackThisFrame<T>(this ref T impl, in TimeSpan currentTime, TimeSpan cooldown) where T : struct, ISimpleAttackAbility
        {
            if (currentTime > impl.AttackStart.Add(impl.DelayBeforeAttack) && !impl.DidAttack)
            {
                impl.Cooldown  = cooldown;
                impl.DidAttack = true;
                return true;
            }

            return false;
        }

        public static bool IsAttackingAndUpdate<T>(this ref T impl, in TimeSpan currentTime) where T : struct, ISimpleAttackAbility
        {
            if (impl.AttackStart != default)
            {
                if (impl.AttackStart.Add(impl.DelayBeforeAttack + impl.PauseAfterAttack) <= currentTime)
                {
                    impl.AttackStart = default;
                }

                return true;
            }

            return false;
        }
    }
}