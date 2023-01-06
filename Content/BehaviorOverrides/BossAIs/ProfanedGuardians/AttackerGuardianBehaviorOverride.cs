using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Armor.Silva;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public enum AttackerGuardianAttackState
        {
            SpawnEffects,
            Phase1FireWallsAndBeam,
            DeathAnimation
        }

        public static int TotalRemaininGuardians =>
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianDefender>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianHealer>()).ToInt();

        public const int BrightnessWidthFactorIndex = 5;

        public const float ImmortalUntilPhase2LifeRatio = 0.75f;

        public const float Phase2LifeRatio = 0.6f;

        public const float Phase3LifeRatio = 0.45f;

        public const float Phase4LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianCommander>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ImmortalUntilPhase2LifeRatio,
            Phase4LifeRatio
        };

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.doughnutBoss = npc.whoAmI;

            // Summon the defender and healer guardian.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[1] == 0f)
            {
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianDefender>());
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianHealer>());
                npc.localAI[1] = 1f;
            }

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Despawn if no valid target exists.
            npc.timeLeft = 3600;
            Player target = Main.player[npc.target];
            if (!target.active || target.dead)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -20f, 6f);
                if (npc.timeLeft < 180)
                    npc.timeLeft = 180;
                if (!npc.WithinRange(target.Center, 2000f) || target.dead)
                    npc.active = false;
                return false;
            }

            // Don't take damage if other guardianas are around.
            npc.dontTakeDamage = false;
            if (TotalRemaininGuardians >= 2f)
                npc.dontTakeDamage = true;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.ai[3];
            ref float shouldHandsBeInvisibleFlag = ref npc.localAI[2];

            // Draw things 
            shouldHandsBeInvisibleFlag = 0f;

            // Do attacks.
            switch ((AttackerGuardianAttackState)attackState)
            {
                case AttackerGuardianAttackState.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.Phase1FireWallsAndBeam:
                    DoBehavior_Phase1FireWallsAndBeam(npc, target, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.DeathAnimation:
                    DoBehavior_DeathAnimation(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float attackTimer)
        {
            float inertia = 20f;
            float flySpeed = 25f;

            Vector2 positionToGoTo = new(WorldSaveSystem.ProvidenceDoorXPosition - 1000f, npc.Center.Y);
            // This is the ideal velocity it would have
            Vector2 idealVelocity = npc.SafeDirectionTo(positionToGoTo) * flySpeed;
            // And this is the actual velocity, using inertia and its existing one.
            npc.velocity = (npc.velocity * (inertia - 1f) + idealVelocity) / inertia;

            if (npc.WithinRange(positionToGoTo, 20f))
            {
                npc.velocity = Vector2.Zero;
                // Go to the initial attack and reset the attack timer.
                npc.ai[0] = 1;
                attackTimer = 0;
            }
        }

        public void DoBehavior_Phase1FireWallsAndBeam(NPC npc, Player target, ref float attackTimer)
        {
            ref float lastOffsetY = ref npc.Infernum().ExtraAI[0];
            float wallCreationRate = 60;

            // Give the player infinite flight time.
            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(npc.Center) <= 10000f)
                    player.wingTime = player.wingTimeMax;
            }

            // Create walls of fire with a random gap in them based off of the last one.
            if (attackTimer % wallCreationRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = -Vector2.UnitX * 10;

                // Create a random offset.
                float yRandomOffset;
                Vector2 previousCenter = npc.Center + new Vector2(0, lastOffsetY);
                Vector2 newCenter;
                int attempts = 0;
                // Attempt to get one within a certain distance, but give up after 10 attempts.
                do
                {
                    yRandomOffset = Main.rand.NextFloat(-600, 200);
                    newCenter = npc.Center + new Vector2(0, yRandomOffset);
                    attempts++;
                }
                while (newCenter.Distance(previousCenter) > 400 || attempts < 10);

                // Set the new random offset as the last one.
                lastOffsetY = yRandomOffset;
                Utilities.NewProjectileBetter(newCenter, velocity, ModContent.ProjectileType<HolyFireWall>(), 300, 0);
                npc.netUpdate = true;
            }

            // End attack when the player is close enough to the guardian.
            if (target.WithinRange(npc.Center, 100f))
            {
                // Switch to next attack.
            }
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float attackTimer)
        {
            int widthExpandDelay = 90;
            int firstExpansionTime = 20;
            int secondExpansionDelay = 1;
            int secondExpansionTime = 132;
            ref float fadeOutFactor = ref npc.Infernum().ExtraAI[0];
            ref float brightnessWidthFactor = ref npc.Infernum().ExtraAI[BrightnessWidthFactorIndex];

            // Slow to a screeching halt.
            npc.velocity *= 0.9f;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Close the boss bar.
            npc.Calamity().ShouldCloseHPBar = true;

            if (attackTimer == widthExpandDelay + firstExpansionTime - 10f)
                SoundEngine.PlaySound(ProvidenceBoss.HolyRaySound with { Volume = 3f, Pitch = 0.4f });

            // Determine the brightness width factor.
            float expansion1 = Utils.GetLerpValue(widthExpandDelay, widthExpandDelay + firstExpansionTime, attackTimer, true) * 0.9f;
            float expansion2 = Utils.GetLerpValue(0f, secondExpansionTime, attackTimer - widthExpandDelay - firstExpansionTime - secondExpansionDelay, true) * 3.2f;
            brightnessWidthFactor = expansion1 + expansion2;
            fadeOutFactor = Utils.GetLerpValue(0f, -25f, attackTimer - widthExpandDelay - firstExpansionTime - secondExpansionDelay - secondExpansionTime, true);

            // Fade out over time.
            npc.Opacity = Utils.GetLerpValue(3f, 1.9f, brightnessWidthFactor, true);

            // Disappear and drop loot.
            if (attackTimer >= widthExpandDelay + firstExpansionTime + secondExpansionDelay + secondExpansionTime)
            {
                npc.life = 0;
                npc.Center = target.Center;
                npc.checkDead();
                npc.active = false;
            }
        }
        #endregion AI and Behaviors

        #region Draw Effects
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            int afterimageCount = 7;
            float brightnessWidthFactor = npc.Infernum().ExtraAI[BrightnessWidthFactorIndex];
            float fadeToBlack = Utils.GetLerpValue(1.84f, 2.66f, brightnessWidthFactor, true);
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianCommanderGlow").Value;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = npc.frame.Size() * 0.5f;

            // Draw the pillar of light behind the guardian when ready.
            if (brightnessWidthFactor > 0f)
            {
                if (!Main.dedServ)
                {
                    if (!Filters.Scene["CrystalDestructionColor"].IsActive())
                        Filters.Scene.Activate("CrystalDestructionColor");

                    Filters.Scene["CrystalDestructionColor"].GetShader().UseColor(Color.Orange.ToVector3());
                    Filters.Scene["CrystalDestructionColor"].GetShader().UseIntensity(Utils.GetLerpValue(0.96f, 1.92f, brightnessWidthFactor, true) * 0.9f);
                }

                Vector2 lightPillarPosition = npc.Center - Main.screenPosition + Vector2.UnitY * 3000f;
                for (int i = 0; i < 16; i++)
                {
                    float intensity = MathHelper.Clamp(brightnessWidthFactor * 1.1f - i / 15f, 0f, 1f);
                    Vector2 lightPillarOrigin = new(TextureAssets.MagicPixel.Value.Width / 2f, TextureAssets.MagicPixel.Value.Height);
                    Vector2 lightPillarScale = new((float)Math.Sqrt(intensity + i) * brightnessWidthFactor * 200f, 6f);
                    Color lightPillarColor = new Color(0.7f, 0.55f, 0.38f, 0f) * intensity * npc.Infernum().ExtraAI[0] * 0.4f;
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, lightPillarPosition, null, lightPillarColor, 0f, lightPillarOrigin, lightPillarScale, 0, 0f);
                }
            }

            // Draw afterimages of the commander.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageDrawColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageDrawColor * (1f - fadeToBlack), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            // Draw back afterimages, indicating that the guardian is fading away into ashes.
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            float radius = Utils.Remap(npc.Opacity, 1f, 0f, 0f, 55f);
            if (radius > 0.5f && npc.ai[0] == (int)AttackerGuardianAttackState.DeathAnimation)
            {
                for (int i = 0; i < 24; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * radius;
                    Color backimageColor = Color.Black;
                    backimageColor.A = (byte)MathHelper.Lerp(164f, 0f, npc.Opacity);
                    spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, backimageColor * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            spriteBatch.Draw(texture, drawPosition, npc.frame, Color.Lerp(npc.GetAlpha(lightColor), Color.Black * npc.Opacity, fadeToBlack), npc.rotation, origin, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, Color.Lerp(Color.White, Color.Black, fadeToBlack) * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }
        #endregion Draw Effects

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Reset the crystal shader. This is necessary since the vanilla values are only stored once.
            Filters.Scene["CrystalDestructionColor"].GetShader().UseColor(1f, 0f, 0.75f);

            // Just die as usual if the Profaned Guardian is killed during the death animation. This is done so that Cheat Sheet and other butcher effects can kill it quickly.
            if (npc.ai[0] == (int)AttackerGuardianAttackState.DeathAnimation)
                return true;

            npc.ai[0] = (int)AttackerGuardianAttackState.DeathAnimation;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Get rid of the silly hands.
            int handID = ModContent.NPCType<EtherealHand>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == handID && Main.npc[i].active)
                {
                    Main.npc[i].active = false;
                    Main.npc[i].netUpdate = true;
                }
            }

            npc.life = npc.lifeMax;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Stay away from those energy fields! Being too close to them will hurt you!";
            yield return n => "Going in a tight circular pattern helps with the attacker guardian's spears!";
        }
        #endregion Tips
    }
}
