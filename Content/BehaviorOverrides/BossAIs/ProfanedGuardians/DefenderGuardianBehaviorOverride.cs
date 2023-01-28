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

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianDefender>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        internal PrimitiveTrailCopy FireDrawer;

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
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[commander.target];
            npc.target = commander.target;

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
                    DoBehavior_SoloHealer(npc, target, ref attackTimer);
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
                DrawFireSuckup(npc, spriteBatch);
                DrawBackglow(npc, spriteBatch, texture);
            }

            // Glow during the healer solo.
            if ((GuardiansAttackType)npc.ai[0] == GuardiansAttackType.SoloHealer)
                DrawBackglow(npc, spriteBatch, texture);
            // Draw the npc.
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);

            // Have an overlay to show the high dr.
            if ((GuardiansAttackType)npc.ai[0] == GuardiansAttackType.SoloHealer)
                DrawDefenseOverlay(npc, spriteBatch, texture);

            // Draw the glowmask over everything
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }

        public Color FireColorFunction(float _) => WayfinderSymbol.Colors[1];

        public void DrawFireSuckup(NPC npc, SpriteBatch spriteBatch)
        {
            FireDrawer ??= new PrimitiveTrailCopy((float completionRatio) => npc.Infernum().ExtraAI[DefenderFireSuckupWidthIndex] * 50f,
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

        public void DrawBackglow(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture)
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

        public void DrawDefenseOverlay(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture)
        {
            spriteBatch.EnterShaderRegion();

            // Initialize the shader.
            Asset<Texture2D> shaderLayer = InfernumTextureRegistry.HolyFireLayer;
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(shaderLayer);
            InfernumEffectsRegistry.RealityTear2Shader.Shader.Parameters["fadeOut"].SetValue(false);

            float sine = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly)) / 2f;
            float opacity = MathHelper.Lerp(0.01f, 0.12f, sine);

            // Draw the wall overlay.
            DrawData wall = new(npcTexture, npc.Center - Main.screenPosition, npc.frame, Color.White * opacity, 0f, npc.frame.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(wall);
            wall.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
        }
    }
}