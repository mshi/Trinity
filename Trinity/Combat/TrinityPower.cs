﻿using System;
using Trinity.Combat.Abilities;
using Zeta.Common;
using Zeta.Game.Internals.Actors;
namespace Trinity
{
    /// <summary>
    /// TrinityPower - used when picking a power and where/how to use it
    /// </summary>
    public class TrinityPower : IEquatable<TrinityPower>
    {
        // 100 == 10 tps or 1/10th a second
        // 66 == 15 tps or 1/15th a second
        // 50 = 20 tps or 1/20th a second
        // 20 == 50 tps or 1/50th a second
        private const int _tickTimeMs = 66;        

        public SNOPower SNOPower { get; set; }
        /// <summary>
        /// The minimum distance from the target (Position or Unit) we should be in before using this power
        /// </summary>
        public float MinimumRange { get; set; }
        /// <summary>
        /// For position based spells (non-Unit)
        /// </summary>
        public Vector3 TargetPosition { get; set; }
        /// <summary>
        /// Always the CurrentDynamicWorldID
        /// </summary>
        public int TargetDynamicWorldId { get; set; }
        /// <summary>
        /// The Unit RActorGUID that we want to target
        /// </summary>
        public int TargetACDGUID { get; set; }
        /// <summary>
        /// The number of 1/10th second intervals we should wait before casting this power
        /// </summary>
        public float WaitTicksBeforeUse { get; set; }
        /// <summary>
        /// The number of 1/10th second intervals we should wait after casting this power
        /// </summary>
        public float WaitTicksAfterUse { get; set; }
        /// <summary>
        /// Whether or not we should wait for the player animation to complete after casting this power
        /// </summary>
        public bool WaitForAnimationFinished { get; set; }
        /// <summary>
        /// The DateTime when the power was assigned
        /// </summary>
        public DateTime PowerAssignmentTime { get; set; }
        /// <summary>
        /// Returns the DateTime the power was last used <seealso cref="CacheData.AbilityLastUsed"/>
        /// </summary>
        public DateTime PowerLastUsedTime
        {
            get
            {
                if (CacheData.AbilityLastUsed.ContainsKey(this.SNOPower))
                    return CacheData.AbilityLastUsed[this.SNOPower];
                else
                    return DateTime.MinValue;
            }
        }

        /// <summary>
        /// The minimum delay we should wait before using a power
        /// </summary>
        public double WaitBeforeUseDelay
        {
            get
            {
                return WaitTicksBeforeUse * _tickTimeMs;
            }
        }

        /// <summary>
        /// The minimum delay in millseconds we should wait after using a power
        /// </summary>
        public double WaitAfterUseDelay
        {
            get
            {
                return WaitTicksAfterUse * _tickTimeMs;
            }
        }

        /// <summary>
        /// Gets the milliseconds since the power was assigned
        /// </summary>
        /// <returns></returns>
        public double TimeSinceAssigned
        {
            get
            {
                return DateTime.UtcNow.Subtract(PowerAssignmentTime).TotalMilliseconds;
            }
        }

        /// <summary>
        /// Gets the millseconds since the power was last used
        /// </summary>
        /// <returns></returns>
        public double TimeSinceUse
        {
            get
            {
                return Trinity.TimeSinceUse(this.SNOPower);
            }
        }

        /// <summary>
        /// Returns True when we bot should be waiting before using a power
        /// </summary>
        public bool ShouldWaitBeforeUse
        {
            get
            {
                // if the number of milliseconds since we assigned it is less than the number of ticks*100 we should wait
                return TimeSinceAssigned < WaitBeforeUseDelay;
            }
        }

        /// <summary>
        /// Returns true when the bot should be waiting after using a power
        /// </summary>
        public bool ShouldWaitAfterUse
        {
            get
            {
                // if the number of millseconds since we used it is more than the number of ticks*100 we should wait
                return TimeSinceUse < WaitAfterUseDelay;
            }
        }

        public TrinityPower()
        {
            this.PowerAssignmentTime = DateTime.UtcNow;

            // default values
            SNOPower = SNOPower.None;
            MinimumRange = 0f;
            TargetPosition = Vector3.Zero;
            TargetDynamicWorldId = -1;
            TargetACDGUID = -1;
            WaitTicksBeforeUse = V.F("Combat.DefaultTickPreDelay");
            WaitTicksAfterUse = V.F("Combat.DefaultTickPostDelay");
            WaitForAnimationFinished = false;
        }

        /// <summary>
        /// Create a TrinityPower for self cast
        /// </summary>
        /// <param name="snoPower"></param>
        /// <param name="minimumRange"></param>
        /// <param name="targetRActorGUID"></param>
        public TrinityPower(SNOPower snoPower)
        {
            SNOPower = snoPower;
            MinimumRange = 0f;
            TargetPosition = Vector3.Zero;
            TargetDynamicWorldId = CombatBase.Player.WorldDynamicID;
            TargetACDGUID = -1;
            WaitTicksBeforeUse = V.F("Combat.DefaultTickPreDelay");
            WaitTicksAfterUse = V.F("Combat.DefaultTickPostDelay");
            WaitForAnimationFinished = true;
            PowerAssignmentTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Create a TrinityPower for self cast
        /// </summary>
        /// <param name="snoPower"></param>
        /// <param name="minimumRange"></param>
        /// <param name="targetRActorGUID"></param>
        public TrinityPower(SNOPower snoPower, int waitTicksBeforeuse, int waitTicksAfterUse)
        {
            SNOPower = snoPower;
            MinimumRange = 0f;
            TargetPosition = Vector3.Zero;
            TargetDynamicWorldId = CombatBase.Player.WorldDynamicID;
            TargetACDGUID = -1;
            WaitTicksBeforeUse = V.F("Combat.DefaultTickPreDelay");
            WaitTicksAfterUse = V.F("Combat.DefaultTickPostDelay");
            WaitForAnimationFinished = true;
            PowerAssignmentTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Create a TrinityPower for use on a specific target
        /// </summary>
        /// <param name="snoPower"></param>
        /// <param name="minimumRange"></param>
        /// <param name="targetACDGuid"></param>
        public TrinityPower(SNOPower snoPower, float minimumRange, int targetACDGuid)
        {
            SNOPower = snoPower;
            MinimumRange = minimumRange;
            TargetPosition = Vector3.Zero;
            TargetDynamicWorldId = CombatBase.Player.WorldDynamicID;
            TargetACDGUID = targetACDGuid;
            WaitTicksBeforeUse = V.F("Combat.DefaultTickPreDelay");
            WaitTicksAfterUse = V.F("Combat.DefaultTickPostDelay");
            WaitForAnimationFinished = true;
            PowerAssignmentTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Create a TrinityPower for generic use with a range
        /// </summary>
        /// <param name="snoPower"></param>
        /// <param name="minimumRange"></param>
        /// <param name="targetRActorGUID"></param>
        public TrinityPower(SNOPower snoPower, float minimumRange)
        {
            SNOPower = snoPower;
            MinimumRange = minimumRange;
            TargetPosition = Vector3.Zero;
            TargetDynamicWorldId = CombatBase.Player.WorldDynamicID;
            TargetACDGUID = -1;
            WaitTicksBeforeUse = V.F("Combat.DefaultTickPreDelay");
            WaitTicksAfterUse = V.F("Combat.DefaultTickPostDelay");
            WaitForAnimationFinished = true;
            PowerAssignmentTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Create a TrinityPower for use at a specific location
        /// </summary>
        /// <param name="snoPower"></param>
        /// <param name="minimumRange"></param>
        /// <param name="targetRActorGUID"></param>
        public TrinityPower(SNOPower snoPower, float minimumRange, Vector3 position)
        {
            SNOPower = snoPower;
            MinimumRange = minimumRange;
            TargetPosition = position;
            TargetDynamicWorldId = CombatBase.Player.WorldDynamicID;
            TargetACDGUID = -1;
            WaitTicksBeforeUse = V.F("Combat.DefaultTickPreDelay");
            WaitTicksAfterUse = V.F("Combat.DefaultTickPostDelay");
            WaitForAnimationFinished = true;
            PowerAssignmentTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrinityPower" /> class.
        /// </summary>
        /// <param name="snoPower">The SNOPower to be used</param>
        /// <param name="minimumRange">The minimum range required from the Position or Target to be used</param>
        /// <param name="position">The Position to use the power at</param>
        /// <param name="targetDynamicWorldId">Usually the CurrentDynamicWorlID</param>
        /// <param name="targetACDGUID">The Unit we are targetting</param>
        /// <param name="waitTicksBeforeUse">The number of "ticks" to wait before using a power - logically 1/10th of a second</param>
        /// <param name="waitTicksAfterUse">The number of "ticks" to wait after using a power - logically 1/10th of a second</param>
        public TrinityPower(SNOPower snoPower, float minimumRange, Vector3 position, int targetDynamicWorldId, int targetACDGUID, float waitTicksBeforeUse, float waitTicksAfterUse)
        {
            SNOPower = snoPower;
            MinimumRange = minimumRange;
            TargetPosition = position;
            TargetDynamicWorldId = targetDynamicWorldId;
            TargetACDGUID = targetACDGUID;
            WaitTicksBeforeUse = waitTicksBeforeUse;
            WaitTicksAfterUse = waitTicksAfterUse;
            WaitForAnimationFinished = true;
            PowerAssignmentTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrinityPower" /> class.
        /// </summary>
        /// <param name="snoPower">The SNOPower to be used</param>
        /// <param name="minimumRange">The minimum range required from the Position or Target to be used</param>
        /// <param name="position">The Position to use the power at</param>
        /// <param name="targetDynamicWorldId">Usually the CurrentDynamicWorlID</param>
        /// <param name="targetACDGUID">The Unit we are targetting</param>
        /// <param name="waitTicksBeforeUse">The number of "ticks" to wait before using a power - logically 1/10th of a second</param>
        /// <param name="waitTicksAfterUse">The number of "ticks" to wait after using a power - logically 1/10th of a second</param>
        /// <param name="waitForAnimationFinished">Force the bot to wait for casting animation to complete after using</param>
        public TrinityPower(SNOPower snoPower, float minimumRange, Vector3 position, int targetDynamicWorldId, int targetACDGUID, float waitTicksBeforeUse, float waitTicksAfterUse, bool waitForAnimationFinished)
        {
            SNOPower = snoPower;
            MinimumRange = minimumRange;
            TargetPosition = position;
            TargetDynamicWorldId = targetDynamicWorldId;
            TargetACDGUID = targetACDGUID;
            WaitTicksBeforeUse = waitTicksBeforeUse;
            WaitTicksAfterUse = waitTicksAfterUse;
            WaitForAnimationFinished = waitForAnimationFinished;
            PowerAssignmentTime = DateTime.UtcNow;
        }


        public bool Equals(TrinityPower other)
        {
            return this.SNOPower == other.SNOPower &&
                this.TargetPosition == other.TargetPosition &&
                this.TargetACDGUID == other.TargetACDGUID &&
                this.WaitAfterUseDelay == other.WaitAfterUseDelay &&
                this.TargetDynamicWorldId == other.TargetDynamicWorldId &&
                this.MinimumRange == other.MinimumRange;
        }

        public override string ToString()
        {
            return
            String.Format("power={0} pos={1} acdGuid={2} preWait={3} postWait={4} animWait={5} timeSinceAssigned={6} timeSinceUse={7} range={8}",
                    this.SNOPower,
                    this.TargetPosition,
                    this.TargetACDGUID,
                    this.WaitTicksBeforeUse,
                    this.WaitTicksAfterUse,
                    this.WaitForAnimationFinished,
                    this.TimeSinceAssigned,
                    this.TimeSinceUse,
                    this.MinimumRange);
        }

        public static int MillisecondsToTickDelay(int milliseconds)
        {
            var timesPerSecond = 1000 / milliseconds;
            var totalTPS = 1000 / _tickTimeMs;

            return totalTPS / (1000 / milliseconds);
        }
    }
}
