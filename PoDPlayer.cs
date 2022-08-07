using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Biomes;
using InfernumMode.Dusts;
using InfernumMode.MachineLearning;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode
{
    public class PoDPlayer : ModPlayer
    {
        public int MadnessTime;
        public bool RedElectrified = false;
        public bool ShadowflameInferno = false;
        public bool DarkFlames = false;
        public bool Madness = false;
        public float CurrentScreenShakePower;
        public float MusicMuffleFactor;

        public bool HatGirl;

        public bool HatGirlShouldGiveAdvice;

        public float MadnessInterpolant => MathHelper.Clamp(MadnessTime / 600f, 0f, 1f);

        public Vector2 ScreenFocusPosition;
        public float ScreenFocusInterpolant = 0f;

        private MLAttackSelector thanatosLaserTypeSelector = null;
        private MLAttackSelector aresSpecialAttackTypeSelector = null;
        private MLAttackSelector twinsSpecialAttackTypeSelector = null;
        public MLAttackSelector ThanatosLaserTypeSelector
        {
            get
            {
                if (thanatosLaserTypeSelector is null)
                    thanatosLaserTypeSelector = new MLAttackSelector(3, "ThanatosLaser");
                return thanatosLaserTypeSelector;
            }
            set => thanatosLaserTypeSelector = value;
        }
        public MLAttackSelector AresSpecialAttackTypeSelector
        {
            get
            {
                if (aresSpecialAttackTypeSelector is null)
                    aresSpecialAttackTypeSelector = new MLAttackSelector(2, "AresSpecialAttack");
                return aresSpecialAttackTypeSelector;
            }
            set => twinsSpecialAttackTypeSelector = value;
        }
        public MLAttackSelector TwinsSpecialAttackTypeSelector
        {
            get
            {
                if (twinsSpecialAttackTypeSelector is null)
                    twinsSpecialAttackTypeSelector = new MLAttackSelector(2, "TwinsSpecialAttack");
                return twinsSpecialAttackTypeSelector;
            }
            set => twinsSpecialAttackTypeSelector = value;
        }

        public bool ZoneProfaned => Player.InModBiome(ModContent.GetInstance<ProfanedTempleBiome>());

        #region Nurse Cheese Death
        public override bool ModifyNurseHeal(NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText)
        {
            if (InfernumMode.CanUseCustomAIs && CalamityPlayer.areThereAnyDamnBosses)
            {
                chatText = "I cannot help you. Good luck.";
                return false;
            }
            return true;
        }
        #endregion Nurse Cheese Death
        #region Reset Effects
        public override void ResetEffects()
        {
            RedElectrified = false;
            ShadowflameInferno = false;
            DarkFlames = false;
            Madness = false;
            HatGirl = false;
            ScreenFocusInterpolant = 0f;
            MusicMuffleFactor = 0f;
        }
        #endregion
        #region Update Dead
        public override void UpdateDead()
        {
            RedElectrified = false;
            ShadowflameInferno = false;
            DarkFlames = false;
            Madness = false;
            MadnessTime = 0;

            if (WorldSaveSystem.InfernumMode)
                Player.respawnTimer = Utils.Clamp(Player.respawnTimer - 1, 0, 3600);
        }
        #endregion
        #region Pre Hurt
        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource, ref int cooldownCounter)
        {
            if (InfernumMode.CanUseCustomAIs && CalamityGlobalNPC.adultEidolonWyrmHead >= 0 && Main.npc[CalamityGlobalNPC.adultEidolonWyrmHead].Calamity().CurrentlyEnraged)
                damage = (int)MathHelper.Max(5500f / (1f - Player.endurance + 1e-6f), damage);
            return true;
        }
        #endregion Pre Hurt
        #region Pre Kill
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (damage == 10.0 && hitDirection == 0 && damageSource.SourceOtherIndex == 8)
            {
                if (RedElectrified)
                    damageSource = PlayerDeathReason.ByCustomReason($"{Player.name} could not withstand the red lightning.");
                if (DarkFlames)
                    damageSource = PlayerDeathReason.ByCustomReason($"{Player.name} was incinerated by ungodly fire.");
                if (Madness)
                    damageSource = PlayerDeathReason.ByCustomReason($"{Player.name} went mad.");
            }
            return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genGore, ref damageSource);
        }
        #endregion
        #region Kill
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            ExoMechManagement.RecordAttackDeath(Player);
        }
        #endregion Kill
        #region Life Regen
        public override void UpdateLifeRegen()
        {
            void causeLifeRegenLoss(int regenLoss)
            {
                if (Player.lifeRegen > 0)
                    Player.lifeRegen = 0;
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= regenLoss;
            }
            if (RedElectrified)
                causeLifeRegenLoss(Player.controlLeft || Player.controlRight ? 64 : 16);

            if (ShadowflameInferno)
                causeLifeRegenLoss(23);
            if (DarkFlames)
            {
                causeLifeRegenLoss(30);
                Player.statDefense -= 8;
            }
            if (Madness)
                causeLifeRegenLoss(50);
            MadnessTime = Utils.Clamp(MadnessTime + (Madness ? 1 : -8), 0, 660);
        }
        #endregion
        #region Screen Shaking
        public override void ModifyScreenPosition()
        {
            if (ScreenFocusInterpolant > 0f && InfernumConfig.Instance.BossIntroductionAnimationsAreAllowed)
            {
                Vector2 idealScreenPosition = ScreenFocusPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                Main.screenPosition = Vector2.Lerp(Main.screenPosition, idealScreenPosition, ScreenFocusInterpolant);
            }

            if (CurrentScreenShakePower > 0f)
                CurrentScreenShakePower = Utils.Clamp(CurrentScreenShakePower - 0.2f, 0f, 15f);
            else
                return;

            if (!CalamityConfig.Instance.Screenshake)
                return;

            Main.screenPosition += Main.rand.NextVector2CircularEdge(CurrentScreenShakePower, CurrentScreenShakePower);
        }
        #endregion
        #region Saving and Loading
        public override void SaveData(TagCompound tag)/* tModPorter Suggestion: Edit tag parameter instead of returning new TagCompound */
        {
            ThanatosLaserTypeSelector?.Save(tag);
            AresSpecialAttackTypeSelector?.Save(tag);
            TwinsSpecialAttackTypeSelector?.Save(tag);
        }

        public override void LoadData(TagCompound tag)
        {
            ThanatosLaserTypeSelector = MLAttackSelector.Load(tag, "ThanatosLaser");
            AresSpecialAttackTypeSelector = MLAttackSelector.Load(tag, "AresSpecialAttack");
            TwinsSpecialAttackTypeSelector = MLAttackSelector.Load(tag, "TwinsSpecialAttack");
        }
        #endregion Saving and Loading
        #region Misc Effects
        public override void PostUpdateMiscEffects()
        {
            if (Player.mount.Active && Player.mount.Type == MountID.Slime && NPC.AnyNPCs(InfernumMode.CalamityMod.Find<ModNPC>("DesertScourgeHead").Type) && InfernumMode.CanUseCustomAIs)
            {
                Player.mount.Dismount(Player);
            }

            // Ensure that Revengeance Mode is always active while Infernum is active.
            if (WorldSaveSystem.InfernumMode && !CalamityWorld.revenge)
                CalamityWorld.revenge = true;

            // I said FUCK OFF.
            bool stupidDifficultyIsActive = Main.masterMode || Main.getGoodWorld;
            if (WorldSaveSystem.InfernumMode && stupidDifficultyIsActive)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetcodeHandler.SyncInfernumActivity(Main.myPlayer);
                WorldSaveSystem.InfernumMode = false;
            }

            if (ShadowflameInferno)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust shadowflame = Dust.NewDustDirect(Player.position, Player.width, Player.height, 28);
                    shadowflame.velocity = Player.velocity.SafeNormalize(Vector2.UnitX * Player.direction);
                    shadowflame.velocity = shadowflame.velocity.RotatedByRandom(0.4f) * -Main.rand.NextFloat(2.5f, 5.4f);
                    shadowflame.scale = Main.rand.NextFloat(0.95f, 1.3f);
                    shadowflame.noGravity = true;
                }
            }

            if (DarkFlames)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust shadowflame = Dust.NewDustDirect(Player.position, Player.width, Player.height, ModContent.DustType<RavagerMagicDust>());
                    shadowflame.velocity = Player.velocity.SafeNormalize(Vector2.UnitX * Player.direction);
                    shadowflame.velocity = shadowflame.velocity.RotatedByRandom(0.4f) * -Main.rand.NextFloat(2.5f, 5.4f);
                    shadowflame.velocity += Main.rand.NextVector2Circular(3f, 3f);
                    shadowflame.scale = Main.rand.NextFloat(0.95f, 1.25f);
                    shadowflame.noGravity = true;
                }
            }
        }
        #endregion
    }
}