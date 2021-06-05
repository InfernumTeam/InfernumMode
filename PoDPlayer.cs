using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode
{
    public class PoDPlayer : ModPlayer
    {
        public bool RedElectrified = false;
        public bool ShadowflameInferno = false;
        public float CurrentScreenShakePower;

        public Vector2 ScreenFocusPosition;
        public float ScreenFocusInterpolant = 0f;

        #region Skies
        public override void UpdateBiomeVisuals()
        {
            if (!PoDWorld.InfernumMode)
                return;

            bool useHIV = NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("HiveMindP2")) && (Main.npc[NPC.FindFirstNPC(InfernumMode.CalamityMod.NPCType("HiveMindP2"))].Infernum().ExtraAI[10] == 1f || Main.npc[NPC.FindFirstNPC(InfernumMode.CalamityMod.NPCType("HiveMindP2"))].life < Main.npc[NPC.FindFirstNPC(InfernumMode.CalamityMod.NPCType("HiveMindP2"))].lifeMax * 0.2f);
            player.ManageSpecialBiomeVisuals("FuckYouMode:HiveMind", useHIV);

            bool useFolly = NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("Bumblefuck")) && (Main.npc[NPC.FindFirstNPC(InfernumMode.CalamityMod.NPCType("Bumblefuck"))].Infernum().ExtraAI[8] > 0f);
            player.ManageSpecialBiomeVisuals("FuckYouMode:Dragonfolly", useFolly);
        }
        #endregion
        #region Reset Effects
        public override void ResetEffects()
        {
            RedElectrified = false;
            ShadowflameInferno = false;
            ScreenFocusInterpolant = 0f;
        }
        #endregion
        #region Update Dead
        public override void UpdateDead()
        {
            RedElectrified = false;
            ShadowflameInferno = false;

            if (PoDWorld.InfernumMode)
                player.respawnTimer = Utils.Clamp(player.respawnTimer - 8, 0, 3600);
        }
        #endregion
        #region Pre Kill
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (damage == 10.0 && hitDirection == 0 && damageSource.SourceOtherIndex == 8)
            {
                if (RedElectrified)
                    damageSource = PlayerDeathReason.ByCustomReason($"{player.name} could not withstand the red lightning.");
            }
            return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genGore, ref damageSource);
        }
        #endregion
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
                causeLifeRegenLoss(40);
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
                CurrentScreenShakePower = Utils.Clamp(CurrentScreenShakePower - 0.2f, 0f, 50f);
            else
                return;

            Main.screenPosition += Main.rand.NextVector2CircularEdge(CurrentScreenShakePower, CurrentScreenShakePower);
        }
		#endregion
		#region Misc Effects
		public override void PostUpdateMiscEffects()
        {
            NPC.MoonLordCountdown = 0;
            if (player.mount.Active && player.mount.Type == Mount.Slime && NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("DesertScourgeHead")))
            {
                player.mount.Dismount(player);
            }
            if (PoDWorld.InfernumMode && !CalamityWorld.death)
            {
                CalamityWorld.death = true;
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
        }
        #endregion
    }
}