using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public static partial class GuardianComboAttackManager
    {
        #region Enums
        public enum GuardiansAttackType
        {
            // Initial attacks.
            SpawnEffects,
            FlappyBird,

            // All 3 combo attacks.
            SoloHealer,
            SoloDefender,
            HealerAndDefender,

            HealerDeathAnimation,

            // Commander and Defender combo attacks
            SpearDashAndGroundSlam,
            CrashRam,
            FireballBulletHell,

            DefenderDeathAnimation,

            // Commander solo attacks.
            LargeGeyserAndCharge,
            DogmaLaserBall,
            BerdlySpears,
            SpearSpinThrow,
            RiftFireCharges,

            CommanderDeathAnimation
        }

        public enum DefenderShieldStatus
        {
            Inactive,
            ActiveAndAiming,
            ActiveAndStatic,
            MarkedForRemoval
        }
        #endregion

        #region Attack Cycles
        public static List<GuardiansAttackType> CommanderAttackCycle => new()
        {
            GuardiansAttackType.LargeGeyserAndCharge,
            GuardiansAttackType.DogmaLaserBall,
            GuardiansAttackType.SpearSpinThrow,
            GuardiansAttackType.RiftFireCharges,
            GuardiansAttackType.BerdlySpears,
            GuardiansAttackType.DogmaLaserBall,
            GuardiansAttackType.LargeGeyserAndCharge,
            GuardiansAttackType.RiftFireCharges,
            GuardiansAttackType.SpearSpinThrow,
            GuardiansAttackType.DogmaLaserBall,
            GuardiansAttackType.BerdlySpears
        };
        #endregion

        #region Fields And Properties
        public static Vector2 CrystalPosition => WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(6740f, 1500f);

        public static Vector2 CenterOfGarden => CrystalPosition + new Vector2(-2370f, -150f);

        public static Rectangle ShardUseisAllowedArea
        {
            get
            {
                Vector2 proviArenaPos = new(WorldSaveSystem.ProvidenceArena.X * 16f, WorldSaveSystem.ProvidenceArena.Y * 16f);
                Vector2 leftPos = proviArenaPos + new Vector2(850f, 1125f);
                Vector2 rightPos = proviArenaPos + new Vector2(1275f, 1735f);
                return new Rectangle((int)leftPos.X, (int)leftPos.Y, (int)rightPos.X - (int)leftPos.X, (int)rightPos.Y - (int)leftPos.Y);
            }
        }

        private static Vector2 leftHandPosition = Vector2.Zero;
        private static Vector2 rightHandPosition = Vector2.Zero;

        /// <summary>
        /// Set this as an offset vector from the commander center.
        /// </summary>
        public static Vector2 LeftHandPosition
        {
            get => leftHandPosition;
            set => leftHandPosition = value;
        }

        /// <summary>
        /// Set this as an offset vector from the commander center.
        /// </summary>
        public static Vector2 RightHandPosition
        {
            get => rightHandPosition;
            set => rightHandPosition = value;
        }

        public static int CommanderType => ModContent.NPCType<ProfanedGuardianCommander>();
        public static int DefenderType => ModContent.NPCType<ProfanedGuardianDefender>();
        public static int HealerType => ModContent.NPCType<ProfanedGuardianHealer>();

        public static Vector2 CommanderStartingHoverPosition => CrystalPosition + new Vector2(600f, -65f);
        public static Vector2 DefenderStartingHoverPosition => CrystalPosition + new Vector2(185f, 475f);
        public static Vector2 HealerStartingHoverPosition => CrystalPosition + new Vector2(200f, -65f);

        // Damage fields.
        public const int ProfanedRockDamage = 200;
        public const int MagicShotDamage = 220;
        public const int HolySpearDamage = 250;
        public const int LingeringFireDamage = 275;
        public const int CommanderSpearDamage = 300;
        public const int HolyFireBeamDamage = 375;
        public const int LavaPillarDamage = 450;
        #endregion

        #region Indexes
        public const int DefenderFireSuckupWidthIndex = 10;
        public const int HealerConnectionsWidthScaleIndex = 11;
        public const int DefenderShouldGlowIndex = 12;
        public const int DefenderDrawDashTelegraphIndex = 13;
        public const int DefenderDashTelegraphOpacityIndex = 14;
        public const int CommanderMovedToTriplePositionIndex = 15;
        /// <summary>
        /// 0 = shield needs to spawn, 1 = shield is spawned and should aim at the player, 2 = shield is spawned and should stop aiming, 3 = shield should die.
        /// </summary>
        public const int DefenderShieldStatusIndex = 16;
        public const int DefenderFireAfterimagesIndex = 17;
        public const int CommanderBlenderShouldFadeOutIndex = 18;
        public const int CommanderAngerGlowAmountIndex = 19;
        /// 0 = spear needs to spawn, 1 = spear is spawned and should aim at the player, 2 = spear is spawned and should stop aiming, 3 = spear should die.
        public const int CommanderSpearStatusIndex = 20;
        public const int CommanderSpearRotationIndex = 21;
        /// <summary>
        /// Handled entirely by the commander. Do not touch
        /// </summary>
        public const int CommanderSpearSmearOpacityIndex = 22;
        // Reset by the commander every frame.
        public const int CommanderDrawSpearSmearIndex = 22;
        public const int CommanderFireAfterimagesIndex = 23;
        public const int CommanderFireAfterimagesLengthIndex = 24;

        public const int CommanderDrawBlackBarsIndex = 25;
        public const int CommanderBlackBarsRotationIndex = 26;
        /// <summary>
        /// Reset by the commander every frame based on the draw index.
        /// </summary>
        public const int CommanderBlackBarsOpacityIndex = 27;

        // Hand stuff
        public const int LeftHandIndex = 28;
        public const int RightHandIndex = 29;
        public const int LeftHandXIndex = 30;
        public const int LeftHandYIndex = 31;
        public const int RightHandXIndex = 32;
        public const int RightHandYIndex = 33;

        public const int DefenderHasBeenYeetedIndex = 34;

        public const int CommanderHasSpawnedBlenderAlreadyIndex = 35;

        /// <summary>
        /// Set by the dogma fireball once, then reset once read by the commander.
        /// </summary>
        public const int CommanderDogmaFireballHasBeenYeetedIndex = 36;
        /// <summary>
        /// This does not reset automatically, it must be end up at 0
        /// </summary>
        public const int CommanderSpearPositionOffsetIndex = 37;

        public const int CommanderBlenderBackglowOpacityIndex = 38;

        /// <summary>
        /// Reset by the commander every frame.
        /// </summary>
        public const int HandsShouldUseNotDefaultPositionIndex = 39;

        /// <summary>
        /// Handled entirely by the commander
        /// </summary>
        public const int FireBorderInterpolantIndex = 40;

        public const int FireBorderShouldDrawIndex = 41;

        public const int CommanderBrightnessWidthFactorIndex = 50;

        public const int CommanderAttackCyclePositionIndex = 51;
        #endregion
    }
}
