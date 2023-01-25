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

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianDefender>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        internal PrimitiveTrailCopy FireDrawer;

        public const int FireSuckupWidthIndex = 10;

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
            ref float drawFireSuckup = ref npc.ai[2];
            ref float fireSuckupWidth = ref npc.Infernum().ExtraAI[FireSuckupWidthIndex];

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[commander.target];

            // Reset taking damage.
            npc.dontTakeDamage = false;

            switch ((DefenderAttackType)attackState)
            {
                // They all share the same thing for heading away to the enterance.
                case DefenderAttackType.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;
                case DefenderAttackType.FireWalls:
                    DoBehavior_FireWalls(npc, target, ref attackTimer, commander, ref drawFireSuckup, ref fireSuckupWidth);
                    break;
            }

            attackTimer++;
            return false;
        }

        public void DoBehavior_FireWalls(NPC npc, Player target, ref float attackTimer, NPC commander, ref float drawFireSuckup, ref float fireSuckupWidth)
        {
            // This is basically flappy bird, the attacker spawns fire walls like the pipes that move towards the entrance of the garden.
            ref float lastOffsetY = ref npc.Infernum().ExtraAI[0];
            ref float movedToPosition = ref npc.Infernum().ExtraAI[1];
            float wallCreationRate = 60f;
            drawFireSuckup = 1f;

            // Give the player infinite flight time.
            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(npc.Center) <= 10000f)
                    player.wingTime = player.wingTimeMax;
            }

            // Do not take damage.
            npc.dontTakeDamage = true;

            if (Main.npc.IndexInRange(GlobalNPCOverrides.ProfanedCrystal))
            {
                if (Main.npc[GlobalNPCOverrides.ProfanedCrystal].active)
                {
                    NPC crystal = Main.npc[GlobalNPCOverrides.ProfanedCrystal];
                    Vector2 hoverPosition = crystal.Center + new Vector2(145f, 425f);
                    // Sit still behind and beneath the crystal.
                    if (npc.Distance(hoverPosition) > 7f && movedToPosition == 0f)
                        npc.velocity = npc.SafeDirectionTo(hoverPosition, Vector2.UnitY) * 5f;
                    else
                    {
                        npc.velocity.X = 0f;
                        movedToPosition = 1f;
                        float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                        npc.velocity.Y = sine * 1.5f;
                        npc.spriteDirection = -1;
                    }

                    // Create walls of fire with a random gap in them based off of the last one.
                    if (attackTimer % wallCreationRate == 0f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 velocity = -Vector2.UnitX * 10f;
                        Vector2 baseCenter = crystal.Center + new Vector2(20f, 0f);
                        // Create a random offset.
                        float yRandomOffset;
                        Vector2 previousCenter = baseCenter + new Vector2(0f, lastOffsetY);
                        Vector2 newCenter;
                        int attempts = 0;
                        // Attempt to get one within a certain distance, but give up after 10 attempts.
                        do
                        {
                            yRandomOffset = Main.rand.NextFloat(-600f, 200f);
                            newCenter = baseCenter + new Vector2(0f, yRandomOffset);
                            attempts++;
                        }
                        while (newCenter.Distance(previousCenter) > 400f || attempts < 10);

                        // Set the new random offset as the last one.
                        lastOffsetY = yRandomOffset;
                        Utilities.NewProjectileBetter(newCenter, velocity, ModContent.ProjectileType<HolyFireWall>(), 300, 0);
                        npc.netUpdate = true;

                        // Reset the attack timer, for drawing.
                        attackTimer = 0f;
                    }


                    // If the crystal is shattering, decrease the scale, else increase it.
                    if (crystal.ai[0] == 1f)
                        fireSuckupWidth = MathHelper.Clamp(fireSuckupWidth - 0.1f, 0f, 1f);
                    else
                        fireSuckupWidth = MathHelper.Clamp(fireSuckupWidth + 0.1f, 0f, 1f);
                }

                if ((AttackerGuardianAttackState)npc.ai[0] == AttackerGuardianAttackState.EmpoweringDefender)
                {
                    // Enter the looping attack section of the pattern, and reset the attack timer.
                    attackTimer = 0f;
                    npc.ai[0] = 2f;
                    drawFireSuckup = 0f;
                }
            }
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
                DrawFireSuckup(npc, spriteBatch);

            // Draw the npc.
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, direction, 0f);

            return false;
        }

        public Color FireColorFunction(float completionRatio)
        {
            return WayfinderSymbol.Colors[1];
        }

        public void DrawFireSuckup(NPC npc, SpriteBatch spriteBatch)
        {
            float baseWidth = npc.Infernum().ExtraAI[FireSuckupWidthIndex] * 100f;
            FireDrawer ??= new PrimitiveTrailCopy((float completionRatio) => baseWidth,
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

            FireDrawer.Draw(drawPositions, -Main.screenPosition, 30);

            Texture2D texture = TextureAssets.Npc[npc.type].Value;

            int backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(texture, npc.Center + backglowOffset - Main.screenPosition, npc.frame, backglowColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);
            }
        }
    }
}