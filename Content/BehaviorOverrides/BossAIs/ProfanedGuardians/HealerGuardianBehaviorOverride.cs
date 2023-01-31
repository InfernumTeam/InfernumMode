using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HealerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianHealer>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        public enum HealerAttackType
        {
            SpawnEffects,
            SitAndMaintainFrontShield,
            SitAndShieldCommander
        }

        internal PrimitiveTrailCopy ShieldEnergyDrawer;

        #region AI
        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            // Declare us as the healer.
            CalamityGlobalNPC.doughnutBossHealer = npc.whoAmI;

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[commander.target];
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float drawShieldConnections = ref npc.ai[2];
            ref float connectionsWidthScale = ref npc.Infernum().ExtraAI[HealerConnectionsWidthScaleIndex];

            npc.damage = 0;
            npc.target = commander.target;
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();

            // Reset taking damage.
            npc.dontTakeDamage = false;

            switch ((GuardiansAttackType)attackState)
            {
                case GuardiansAttackType.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;
                case GuardiansAttackType.FlappyBird:
                    DoBehavior_FlappyBird(npc, target, ref attackTimer, commander);
                    break;
                case GuardiansAttackType.SoloHealer:
                    DoBehavior_SoloHealer(npc, target, ref attackTimer, commander);
                    break;
                case GuardiansAttackType.SoloDefender:
                    DoBehavior_SoloDefender(npc, target, ref attackTimer);
                    break;
                case GuardiansAttackType.HealerAndDefender:
                    DoBehavior_HealerAndDefender(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianHealerGlow").Value;
            Texture2D glowmask2 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianHealerGlow2").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // If maintaining the front shield or shielding the commander, glow.
            if ((HealerAttackType)npc.ai[0] is HealerAttackType.SitAndMaintainFrontShield or HealerAttackType.SitAndShieldCommander)
                DrawNPCBackglow(npc, spriteBatch, texture, direction);

            // Draw the npc.
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask2, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, direction, 0f);
            // If shield connections should be drawn.
            if (npc.ai[2] == 1f)
                DrawShieldConnections(npc);
            return false;
        }

        public static void DrawNPCBackglow(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture, SpriteEffects direction)
        {
            int backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = MagicCrystalShot.ColorSet[0];
                backglowColor.A = 0;
                spriteBatch.Draw(npcTexture, npc.Center + backglowOffset - Main.screenPosition, npc.frame, backglowColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);
            }
        }

        public static Color EnergyColorFunction(float completionRatio) => MagicCrystalShot.ColorSet[0];

        public void DrawShieldConnections(NPC npc)
        {
            ShieldEnergyDrawer ??= new PrimitiveTrailCopy((float _) => npc.Infernum().ExtraAI[HealerConnectionsWidthScaleIndex] * 20f, EnergyColorFunction, null, true, InfernumEffectsRegistry.PulsatingLaserVertexShader);

            if (!Main.npc.IndexInRange(GlobalNPCOverrides.ProfanedCrystal) && !Main.npc[GlobalNPCOverrides.ProfanedCrystal].active)
                return;

            NPC crystal = Main.npc[GlobalNPCOverrides.ProfanedCrystal];

            Vector2 startPos = npc.TopLeft + new Vector2(16f, 60f);
            Vector2 endPos = crystal.Top;

            InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBubbleGlow);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(Color.Lerp(MagicCrystalShot.ColorSet[0], Color.White, 0.1f));
            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(2.5f);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(true);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["reverseDirection"].SetValue(false);


            Vector2[] drawPositions = new Vector2[8];
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPositions.Length);

            ShieldEnergyDrawer.Draw(drawPositions, -Main.screenPosition, 30);
            endPos = crystal.Bottom;
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPositions.Length);
            ShieldEnergyDrawer.Draw(drawPositions, -Main.screenPosition, 30);

            // Draw a glow over the crystal.
            Texture2D glowTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = startPos - Main.screenPosition;
            Vector2 glowOrigin = glowTexture.Size() * 0.5f;
            Color baseColor = MagicCrystalShot.ColorSet[0];
            baseColor.A = 0;
            Color modifiedColor = Color.Lerp(Color.Transparent, baseColor, npc.Infernum().ExtraAI[HealerConnectionsWidthScaleIndex]);
            float scaleSine = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly)) / 2f;
            float glowScale = MathHelper.Lerp(1.1f, 1.15f, scaleSine);
            Color finalColor = Color.Lerp(modifiedColor, new(1f, 1f, 1f, 0f), scaleSine);
            Main.EntitySpriteDraw(glowTexture, drawPosition, null, finalColor, 0f, glowOrigin, 1f, SpriteEffects.None, 0);

            // Draw two spinning gleams over the crystal.
            Texture2D gleamTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Vector2 gleamOrigin = gleamTexture.Size() * 0.5f;       
            float gleamScale = MathHelper.Lerp(2.2f, 2.4f, scaleSine);
            float gleamRotation = MathHelper.Pi * Main.GlobalTimeWrappedHourly * 2f;
            Main.spriteBatch.Draw(gleamTexture, drawPosition, null, finalColor, gleamRotation, gleamOrigin, gleamScale, 0, 0f);
            Main.spriteBatch.Draw(gleamTexture, drawPosition, null, finalColor, -gleamRotation, gleamOrigin, gleamScale, 0, 0f);
        }
        #endregion
    }
}