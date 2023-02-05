using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceNPC = CalamityMod.NPCs.Providence.Providence;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.AttackerGuardianBehaviorOverride;
using InfernumMode.GlobalInstances;
using System;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Terraria.DataStructures;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;
using ReLogic.Content;
using CalamityMod;
using System.Collections.Generic;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianDefender>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        internal PrimitiveTrailCopy FireDrawer;

        internal PrimitiveTrailCopy DashTelegraphDrawer;

        public enum DefenderAttackType
        {
            SpawnEffects,
            FireWalls,

            // Repeating attacks.
            //VerticalCharges,
        }

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            CalamityGlobalNPC.doughnutBossDefender = npc.whoAmI;

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[commander.target];
            npc.target = commander.target;

            // These are inherited from the commander.
            ref float attackState = ref commander.ai[0];
            ref float attackTimer = ref commander.ai[1];
            ref float drawFireSuckup = ref npc.ai[2];
            drawFireSuckup = 0;

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
                    DoBehavior_SoloDefender(npc, target, ref attackTimer, commander);
                    break;
                case GuardiansAttackType.HealerAndDefender:
                    DoBehavior_HealerAndDefender(npc, target, ref attackTimer, commander);
                    break;
            }
            return false;
        }      

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianDefenderGlow").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw the lava absorbing.
            if (npc.ai[2] == 1f)
            {
                DrawFireSuckup(npc);
                DrawBackglow(npc, spriteBatch, texture);
            }

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];

            // Draw a dash telegraph when needed.
            if (commander.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex] == 1)
                DrawDashTelegraph(npc, spriteBatch, commander);

            // Glow during the healer solo.
            if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.SoloHealer || commander.Infernum().ExtraAI[DefenderShouldGlowIndex] == 1)
                DrawBackglow(npc, spriteBatch, texture);
            // Draw the npc.
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);

            // Have an overlay to show the high dr.
            if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.SoloHealer)
                DrawDefenseOverlay(npc, spriteBatch, texture);

            // Draw the glowmask over everything
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }

        public static Color FireColorFunction(float _) => WayfinderSymbol.Colors[1];

        public void DrawFireSuckup(NPC npc)
        {
            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            FireDrawer ??= new PrimitiveTrailCopy((float completionRatio) => commander.Infernum().ExtraAI[DefenderFireSuckupWidthIndex] * 50f,
                FireColorFunction, null, true, InfernumEffectsRegistry.PulsatingLaserVertexShader);

            Vector2 startPos = npc.Center;
            Vector2 endPos = npc.Center + new Vector2(0f, 700f);
            Vector2[] drawPositions = new Vector2[8];
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPositions.Length);

            InfernumEffectsRegistry.PulsatingLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakBubbleGlow);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseColor(WayfinderSymbol.Colors[2]);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.UseSaturation(3f);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["usePulsing"].SetValue(false);
            InfernumEffectsRegistry.PulsatingLaserVertexShader.Shader.Parameters["reverseDirection"].SetValue(true);

            FireDrawer.Draw(drawPositions, -Main.screenPosition, 40);
        }

        public static void DrawBackglow(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture)
        {
            int backglowAmount = 12;
            float sine = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
            float backglowDistance = MathHelper.Lerp(3.5f, 4.5f, sine);
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * backglowDistance;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(npcTexture, npc.Center + backglowOffset - Main.screenPosition, npc.frame, backglowColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);
            }
        }

        public void DrawDashTelegraph(NPC npc, SpriteBatch spriteBatch, NPC commander)
        {
            float opacityScalar = commander.Infernum().ExtraAI[DefenderDashTelegraphOpacityIndex];

            // Don't bother drawing anything if it would not be visible.
            if (opacityScalar == 0)
                return;

            DashTelegraphDrawer ??= new PrimitiveTrailCopy((float completionRatio) => 65f,
                (float completionRatio) => Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.5f) * commander.Infernum().ExtraAI[DefenderDashTelegraphOpacityIndex],
                null, true, InfernumEffectsRegistry.SideStreakVertexShader);

            InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);
            InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.3f);

            Vector2 startPos = npc.Center;
            float distance = 1000f;
            Vector2 direction = npc.DirectionTo(Main.player[npc.target].Center);
            Vector2 endPos = npc.Center + direction * distance;
            Vector2[] drawPositions = new Vector2[8];
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPositions.Length);

            DashTelegraphDrawer.Draw(drawPositions, -Main.screenPosition, 30);

            // Draw arrows.
            Texture2D arrowTexture = InfernumTextureRegistry.Arrow.Value;

            Color drawColor = Color.Orange * opacityScalar;
            drawColor.A = 0;
            Vector2 drawPosition = (startPos + direction * 120f) - Main.screenPosition;
            for (int i = 1; i < 8; i++)
            {
                Vector2 arrowOrigin = arrowTexture.Size() * 0.5f;
                float arrowRotation = direction.ToRotation() + MathHelper.PiOver2;
                float sineValue = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 10.5f - i)) / 2f;
                float finalOpacity = CalamityUtils.SineInOutEasing(sineValue, 1);
                spriteBatch.Draw(arrowTexture, drawPosition, null, drawColor * finalOpacity, arrowRotation, arrowOrigin, 0.75f, SpriteEffects.None, 0f);
                drawPosition += direction * 75f;
            }

        }

        public static void DrawDefenseOverlay(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture)
        {
            spriteBatch.EnterShaderRegion();

            // Initialize the shader.
            Asset<Texture2D> shaderLayer = InfernumTextureRegistry.HolyFireLayer;
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(shaderLayer);
            InfernumEffectsRegistry.RealityTear2Shader.Shader.Parameters["fadeOut"].SetValue(false);

            float sine = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly)) / 2f;
            float opacity = MathHelper.Lerp(0.01f, 0.12f, sine);

            // Draw the overlay.
            DrawData overlay = new(npcTexture, npc.Center - Main.screenPosition, npc.frame, Color.White * opacity, 0f, npc.frame.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(overlay);
            overlay.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
        }
    }
}