using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Core.GlobalInstances.Systems;
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
            HoverAndFireDeathray,
            EmpoweringDefender,
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
                case AttackerGuardianAttackState.HoverAndFireDeathray:
                    DoBehavior_HoverAndFireDeathray(npc, target, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.EmpoweringDefender:
                    DoBehavior_EmpowerDefender(npc, target, ref attackTimer);
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

            // If we are the commander, spawn in the pushback fire wall.
            if (npc.type == ModContent.NPCType<ProfanedGuardianCommander>())
            {
                if (attackTimer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = new(target.Center.X + 800f, npc.Center.Y);
                    Vector2 finalPosition = new(WorldSaveSystem.ProvidenceDoorXPosition - 7104f, target.Center.Y);
                    float distance = (spawnPosition - finalPosition).Length();
                    float x = distance / HolyPushbackWall.Lifetime;
                    Vector2 velocity = new(-x, 0f);
                    Utilities.NewProjectileBetter(spawnPosition, velocity, ModContent.ProjectileType<HolyPushbackWall>(), 300, 0f);
                }
            }

            if (npc.WithinRange(positionToGoTo, 20f))
            {
                npc.velocity = Vector2.Zero;
                // Go to the initial attack and reset the attack timer.
                SelectNewAttack(npc, ref attackTimer);
            }
        }

        public void DoBehavior_HoverAndFireDeathray(NPC npc, Player target, ref float attackTimer)
        {
            float deathrayFireRate = 120;
            float initialDelay = 400;

            // Do not take damage.
            npc.dontTakeDamage = true;

            // If time to fire, the target is close enough and the pushback wall is not present.
            if (attackTimer % deathrayFireRate == 0 && target.WithinRange(npc.Center, 6200f) && attackTimer >= initialDelay)
            {
                // Fire deathray.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
                    Utilities.NewProjectileBetter(npc.Center, aimDirection, ModContent.ProjectileType<HolyAimedDeathrayTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);
                }
            }           
        }

        public void DoBehavior_EmpowerDefender(NPC npc, Player target, ref float attackTimer)
        {
            // The commander doesnt directly attack here, instead visibly "empowering" the defender guardian,
            // as it attacks you instead.
            ref float defenderGlowScalar = ref npc.Infernum().ExtraAI[0];
            float glowInterpolant = MathHelper.Clamp(attackTimer / 60f, 0f, 1f);
            defenderGlowScalar = MathHelper.Lerp(0f, 1f, glowInterpolant);
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

        public static void SelectNewAttack(NPC npc, ref float attackTimer)
        {
            // Reset the first 5 extra ai slots. These are used for per attack information.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0;

            // Reset the attack timer.
            attackTimer = 0;
            // The attack cycle is relatively linear, so advance the current attack by one.
            npc.ai[0]++;
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
            SpriteEffects direction = npc.rotation is < MathHelper.PiOver2 or > MathHelper.Pi + MathHelper.PiOver2 ? SpriteEffects.None : SpriteEffects.FlipVertically;
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
            if (Main.netMode != NetmodeID.Server)
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
