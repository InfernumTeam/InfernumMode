using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.Items.Armor;
using CalamityMod.NPCs;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Buffs;
using InfernumMode.Dusts;
using InfernumMode.MachineLearning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode
{
    public class PoDPlayer : ModPlayer
    {
        public bool RedElectrified = false;
        public bool ShadowflameInferno = false;
        public bool DarkFlames = false;
        public float CurrentScreenShakePower;
        public float MusicMuffleFactor;

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

        public static bool ApplyEarlySpeedNerfs => InfernumMode.CalamityMod.Version < new Version("1.5.0.004");

        #region Skies
        public override void UpdateBiomeVisuals()
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            bool useFolly = NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("Bumblefuck")) && (Main.npc[NPC.FindFirstNPC(InfernumMode.CalamityMod.NPCType("Bumblefuck"))].Infernum().ExtraAI[8] > 0f);
            player.ManageSpecialBiomeVisuals("InfernumMode:Dragonfolly", useFolly);

            if (!BossRushEvent.BossRushActive)
            {
                int hiveMindID = InfernumMode.CalamityMod.NPCType("HiveMind");
                int hiveMind = NPC.FindFirstNPC(hiveMindID);
                NPC hiveMindNPC = hiveMind >= 0 ? Main.npc[hiveMind] : null;
                bool useHIV = hiveMindNPC != null && (hiveMindNPC.Infernum().ExtraAI[10] == 1f || hiveMindNPC.life < hiveMindNPC.lifeMax * 0.2f);
                player.ManageSpecialBiomeVisuals("InfernumMode:HiveMind", useHIV);

                bool useDeus = NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("AstrumDeusHeadSpectral"));
                player.ManageSpecialBiomeVisuals("InfernumMode:Deus", useDeus);

                int oldDukeID = ModContent.NPCType<OldDuke>();
                int oldDuke = NPC.FindFirstNPC(oldDukeID);
                NPC oldDukeNPC = oldDuke >= 0 ? Main.npc[oldDuke] : null;
                bool useOD = oldDukeNPC != null && oldDukeNPC.Infernum().ExtraAI[6] >= 2f;
                player.ManageSpecialBiomeVisuals("InfernumMode:OldDuke", useOD);
            }
        }
        #endregion
        #region Reset Effects
        public override void ResetEffects()
        {
            RedElectrified = false;
            ShadowflameInferno = false;
            DarkFlames = false;
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

            if (PoDWorld.InfernumMode)
                player.respawnTimer = Utils.Clamp(player.respawnTimer - 1, 0, 3600);
        }
        #endregion
        #region Pre Hurt
        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (InfernumMode.CanUseCustomAIs && CalamityGlobalNPC.adultEidolonWyrmHead >= 0 && Main.npc[CalamityGlobalNPC.adultEidolonWyrmHead].Calamity().CurrentlyEnraged)
                damage = (int)MathHelper.Max(5500f / (1f - player.endurance + 1e-6f), damage);
            return base.PreHurt(pvp, quiet, ref damage, ref hitDirection, ref crit, ref customDamage, ref playSound, ref genGore, ref damageSource);
        }
        #endregion Pre Hurt
        #region Pre Kill
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (damage == 10.0 && hitDirection == 0 && damageSource.SourceOtherIndex == 8)
            {
                if (RedElectrified)
                    damageSource = PlayerDeathReason.ByCustomReason($"{player.name} could not withstand the red lightning.");
                if (DarkFlames)
                    damageSource = PlayerDeathReason.ByCustomReason($"{player.name} was incinerated by ungodly fire.");
            }
            return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genGore, ref damageSource);
        }
        #endregion
        #region Kill
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            ExoMechManagement.RecordAttackDeath(player);
        }
        #endregion Kill
        #region Life Regen
        public override void UpdateLifeRegen()
        {
            void causeLifeRegenLoss(int regenLoss)
            {
                if (player.lifeRegen > 0)
                    player.lifeRegen = 0;
                player.lifeRegenTime = 0;
                player.lifeRegen -= regenLoss;
            }
            if (RedElectrified)
                causeLifeRegenLoss(player.controlLeft || player.controlRight ? 64 : 16);

            if (ShadowflameInferno)
                causeLifeRegenLoss(23);
            if (DarkFlames)
            {
                causeLifeRegenLoss(30);
                player.statDefense -= 8;
            }
        }
        #endregion
        #region Drawing

        public static readonly PlayerLayer RedLightningEffect = new PlayerLayer("CalamityMod", "MiscEffectsBack", PlayerLayer.MiscEffectsBack, drawInfo =>
        {
            if (drawInfo.shadow != 0f || !drawInfo.drawPlayer.Infernum().RedElectrified)
                return;

            Texture2D texture2D2 = Main.glowMaskTexture[25];
            int frame = drawInfo.drawPlayer.miscCounter / 5;
            for (int l = 0; l < 2; l++)
            {
                frame %= 7;
                Player player = drawInfo.drawPlayer;
                SpriteEffects spriteEffects = drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (frame > 1 && frame < 5)
                {
                    Color lightningColor = Color.Red;
                    lightningColor.A = 0;

                    Rectangle frameRectangle = new Rectangle(0, frame * texture2D2.Height / 7, texture2D2.Width, texture2D2.Height / 7);
                    Vector2 fuck = new Vector2((int)(drawInfo.position.X - Main.screenPosition.X - (player.bodyFrame.Width / 2) + (player.width / 2)), (int)(drawInfo.position.Y - Main.screenPosition.Y + player.height - player.bodyFrame.Height + 4f)) + player.bodyPosition + player.bodyFrame.Size() * 0.5f;
                    DrawData lightningEffect = new DrawData(texture2D2, fuck, frameRectangle, lightningColor, player.bodyRotation, frameRectangle.Size() * 0.5f, 1f, spriteEffects, 0);
                    Main.playerDrawData.Add(lightningEffect);
                }
                frame += 3;
            }
        });

        public override void ModifyDrawLayers(List<PlayerLayer> layers)
        {
            layers.Add(RedLightningEffect);
        }

        #endregion
        #region Screen Shaking
        public override void ModifyScreenPosition()
        {
            if (ScreenFocusInterpolant > 0f)
            {
                Vector2 idealScreenPosition = ScreenFocusPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                Main.screenPosition = Vector2.Lerp(Main.screenPosition, idealScreenPosition, ScreenFocusInterpolant);
            }

            if (CurrentScreenShakePower > 0f)
                CurrentScreenShakePower = Utils.Clamp(CurrentScreenShakePower - 0.2f, 0f, 15f);
            else
                return;

            if (CalamityConfig.Instance.DisableScreenShakes)
                return;

            Main.screenPosition += Main.rand.NextVector2CircularEdge(CurrentScreenShakePower, CurrentScreenShakePower);
        }
        #endregion
        #region Saving and Loading
        public override TagCompound Save()
        {
            TagCompound tag = new TagCompound();
            ThanatosLaserTypeSelector?.Save(tag);
            AresSpecialAttackTypeSelector?.Save(tag);
            TwinsSpecialAttackTypeSelector?.Save(tag);
            return tag;
        }

        public override void Load(TagCompound tag)
        {
            ThanatosLaserTypeSelector = MLAttackSelector.Load(tag, "ThanatosLaser");
            AresSpecialAttackTypeSelector = MLAttackSelector.Load(tag, "AresSpecialAttack");
            TwinsSpecialAttackTypeSelector = MLAttackSelector.Load(tag, "TwinsSpecialAttack");
        }
        #endregion Saving and Loading
        #region Misc Effects
        public void MakeAnxious(int time)
        {
            int deleteIndex = Player.MaxBuffs - 1;
            int[] buffsToSkip = new int[]
            {
                BuffID.ChaosState,
                BuffID.PotionSickness,
                BuffID.ManaSickness,
                ModContent.BuffType<ManaBurn>()
            };
            while (buffsToSkip.Contains(player.buffType[deleteIndex]) && deleteIndex > 0)
                deleteIndex--;

            player.DelBuff(deleteIndex);
            player.AddBuff(ModContent.BuffType<Anxiety>(), time);
        }
        public override void PostUpdateMiscEffects()
        {
            NPC.MoonLordCountdown = 0;
            if (player.mount.Active && player.mount.Type == Mount.Slime && NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("DesertScourgeHead")))
            {
                player.mount.Dismount(player);
            }
            if (PoDWorld.InfernumMode && !CalamityWorld.revenge)
            {
                CalamityWorld.revenge = true;
            }
            if (PoDWorld.InfernumMode && CalamityWorld.DoGSecondStageCountdown > 600)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active &&
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("Signus") ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("StormWeaverHead")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("StormWeaverBody")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("StormWeaverTail")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("StormWeaverNakedHead")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("StormWeaverNakedBody")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("StormWeaverNakedTail")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("CeaselessVoid")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("DarkEnergy")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("DarkEnergy2")) ||
                        (Main.npc[i].type == InfernumMode.CalamityMod.NPCType("DarkEnergy3"))))
                    {
                        Main.npc[i].active = false;
                    }
                }
                CalamityWorld.DoGSecondStageCountdown = 599;
            }

            if (ShadowflameInferno)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust shadowflame = Dust.NewDustDirect(player.position, player.width, player.height, 28);
                    shadowflame.velocity = player.velocity.SafeNormalize(Vector2.UnitX * player.direction);
                    shadowflame.velocity = shadowflame.velocity.RotatedByRandom(0.4f) * -Main.rand.NextFloat(2.5f, 5.4f);
                    shadowflame.scale = Main.rand.NextFloat(0.95f, 1.3f);
                    shadowflame.noGravity = true;
                }
            }

            if (DarkFlames)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust shadowflame = Dust.NewDustDirect(player.position, player.width, player.height, ModContent.DustType<RavagerMagicDust>());
                    shadowflame.velocity = player.velocity.SafeNormalize(Vector2.UnitX * player.direction);
                    shadowflame.velocity = shadowflame.velocity.RotatedByRandom(0.4f) * -Main.rand.NextFloat(2.5f, 5.4f);
                    shadowflame.velocity += Main.rand.NextVector2Circular(3f, 3f);
                    shadowflame.scale = Main.rand.NextFloat(0.95f, 1.25f);
                    shadowflame.noGravity = true;
                }
            }
        }
        #endregion
        #region Fuck
        public override void UpdateEquips(ref bool wallSpeedBuff, ref bool tileSpeedBuff, ref bool tileRangeBuff)
        {
            if (!ApplyEarlySpeedNerfs)
                return;

            if (player.armor[0].type == ModContent.ItemType<AuricTeslaCuisses>())
                player.moveSpeed -= 0.1f;
            if (player.armor[0].type == ModContent.ItemType<AuricTeslaPlumedHelm>())
                player.moveSpeed -= 0.15f;
        }
        #endregion
    }
}