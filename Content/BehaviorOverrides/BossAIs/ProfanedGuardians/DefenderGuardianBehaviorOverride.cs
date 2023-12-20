using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianDefender>();

        public override int? NPCTypeToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        internal PrimitiveTrailCopy FireDrawer;

        internal PrimitiveTrailCopy DashTelegraphDrawer;

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 228;
            npc.height = 166;
            npc.scale = 1f;
            npc.defense = 50;
            npc.DR_NERD(0.4f);
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
            ref float drawDashTelegraph = ref commander.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex];
            drawDashTelegraph = 0;

            // Reset taking damage.
            npc.dontTakeDamage = false;
            // Don't deal damage by default.
            npc.damage = 0;
            npc.chaseable = true;

            // If the healer is dead, resume taking damage.
            if (CalamityGlobalNPC.doughnutBossHealer == -1 && !Main.npc.Any(h => h.active && h.type == HealerType))
                npc.Calamity().DR = 0.35f;
            else
            {
                // Have very high DR.
                npc.Calamity().DR = 0.9999f;
                npc.lifeRegen = 1000000;
                npc.chaseable = false;

                // Don't take damage if low enough to avoid being killable first somehow.
                if ((float)npc.life / npc.lifeMax <= 0.8f)
                    npc.dontTakeDamage = true;
            }

            if (commander.Infernum().ExtraAI[DefenderHasBeenYeetedIndex] == 1f)
            {
                DoBehavior_DefenderYeetEffects(npc, target, ref attackTimer, commander);
                return false;
            }

            switch ((GuardiansAttackType)attackState)
            {
                case GuardiansAttackType.SpawnEffects:
                    // Go straight to this so the walls sync.
                    DoBehavior_FlappyBird(npc, target, ref attackTimer, commander);
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

                case GuardiansAttackType.HealerDeathAnimation:
                    DoBehavior_HealerDeathAnimation(npc, target, ref attackTimer, commander);
                    break;

                case GuardiansAttackType.SpearDashAndGroundSlam:
                    DoBehavior_SpearDashAndGroundSlam(npc, target, ref attackTimer, commander);
                    break;

                case GuardiansAttackType.CrashRam:
                    DoBehavior_CrashRam(npc, target, ref attackTimer, commander);
                    break;

                case GuardiansAttackType.FireballBulletHell:
                    DoBehavior_FireballBulletHell(npc, target, ref attackTimer, commander);
                    break;

                case GuardiansAttackType.DefenderDeathAnimation:
                    DoBehavior_DefenderDeathAnimation(npc, target, ref attackTimer, commander);
                    break;
            }
            return false;
        }

        public static void DoBehavior_DefenderYeetEffects(NPC npc, Player target, ref float attackTimer, NPC commander)
        {
            ref float localAttackTimer = ref npc.Infernum().ExtraAI[0];
            ref float substate = ref npc.Infernum().ExtraAI[1];

            ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];

            npc.Calamity().ShouldCloseHPBar = true;
            npc.damage = 0;
            switch (substate)
            {
                case 0:
                    // Create particles to indicate the sudden speed.
                    if (Main.rand.NextBool())
                    {
                        Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                        Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                        GeneralParticleHandler.SpawnParticle(energyLeak);
                    }

                    shieldStatus = (float)DefenderShieldStatus.ActiveAndStatic;

                    if ((Collision.SolidCollision(npc.Center, npc.width, npc.height) && npc.Center.Y > target.Center.Y) || localAttackTimer >= 120f)
                    {
                        // Play a loud explosion + hurt sound and screenshake to give the impact power.
                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 4f }, target.Center);
                        SoundEngine.PlaySound(npc.HitSound.Value with { Volume = 6f }, target.Center);

                        DoPhaseTransitionEffects(commander, 1);

                        if (CalamityConfig.Instance.Screenshake)
                        {
                            target.Infernum_Camera().CurrentScreenShakePower = 20f;
                            ScreenEffectSystem.SetBlurEffect(npc.Center, 2f, 60);
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                            {
                                explosion.ModProjectile<HolySunExplosion>().MaxRadius = 300f;
                            });
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<HolySunExplosion>(), SunExplosionDamage, 0f);
                        }

                        for (int i = 0; i < 100; i++)
                        {
                            Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height);
                            Vector2 velocity = npc.SafeDirectionTo(position) * Main.rand.NextFloat(1.5f, 2f);
                            Particle ashes = new MediumMistParticle(position, velocity, WayfinderSymbol.Colors[1], Color.Gray, Main.rand.NextFloat(0.75f, 0.95f), 400, Main.rand.NextFloat(-0.05f, 0.05f));
                            GeneralParticleHandler.SpawnParticle(ashes);
                        }

                        MoonlordDeathDrama.RequestLight(1f, target.Center);

                        // Create a bunch of rock particles to indicate a heavy impact.
                        Vector2 impactCenter = npc.Center;
                        for (int j = 0; j < 50; j++)
                        {
                            Particle rock = new ProfanedRockParticle(impactCenter, -Vector2.UnitY.RotatedByRandom(Tau) * Main.rand.NextFloat(3f, 6f), Color.White, Main.rand.NextFloat(0.85f, 1.15f), 120, Main.rand.NextFloat(0f, 0.2f), false);
                            GeneralParticleHandler.SpawnParticle(rock);
                        }
                        substate++;
                        localAttackTimer = 0f;
                    }
                    break;

                case 1:
                    if (localAttackTimer == 30f)
                        typeof(MoonlordDeathDrama).GetField("whitening", Utilities.UniversalBindingFlags).SetValue(null, 1f);

                    npc.Opacity = 0f;

                    if (localAttackTimer > 45f)
                    {
                        SelectNewAttack(commander, ref attackTimer, (float)GuardiansAttackType.LargeGeyserAndCharge);
                        commander.Infernum().ExtraAI[CommanderAttackCyclePositionIndex] = 1f;

                        HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ProfanedDefenderTip1");
                        npc.life = 0;
                        npc.NPCLoot();
                        npc.active = false;
                    }
                    break;
            }
            localAttackTimer++;
        }

        #region Drawing
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
            if (CalamityGlobalNPC.doughnutBoss == -1)
                return;

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            FireDrawer ??= new PrimitiveTrailCopy((float completionRatio) => commander.Infernum().ExtraAI[DefenderFireSuckupWidthIndex] * 50f,
                FireColorFunction, null, true, InfernumEffectsRegistry.PulsatingLaserVertexShader);

            Vector2 startPos = npc.Center + new Vector2(-26, 0);
            Vector2 endPos = startPos + new Vector2(0f, 700f);
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
            float sine = (1f + Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
            float backglowDistance = Lerp(3.5f, 4.5f, sine);
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * backglowDistance;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(npcTexture, npc.Center + backglowOffset - Main.screenPosition, npc.frame, backglowColor * npc.Opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);
            }
        }

        public static Color DashTelegraphColor() => Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.5f) * 0.75f;

        public void DrawDashTelegraph(NPC npc, SpriteBatch spriteBatch, NPC commander)
        {
            float opacityScalar = commander.Infernum().ExtraAI[DefenderDashTelegraphOpacityIndex];

            // Don't bother drawing anything if it would not be visible.
            if (opacityScalar == 0)
                return;

            DashTelegraphDrawer ??= new PrimitiveTrailCopy(c => 65f,
                c => DashTelegraphColor(),
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
                float arrowRotation = direction.ToRotation() + PiOver2;
                float sineValue = (1f + Sin(Main.GlobalTimeWrappedHourly * 10.5f - i)) / 2f;
                float finalOpacity = CalamityUtils.SineInOutEasing(sineValue, 1);
                spriteBatch.Draw(arrowTexture, drawPosition, null, drawColor * finalOpacity, arrowRotation, arrowOrigin, 0.75f, SpriteEffects.None, 0f);
                drawPosition += direction * 75f;
            }

        }

        public static void DrawDefenseOverlay(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture)
        {
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.EnterShaderRegion();

            // Initialize the shader.
            Asset<Texture2D> shaderLayer = InfernumTextureRegistry.HolyFireLayer;
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(shaderLayer);
            InfernumEffectsRegistry.RealityTear2Shader.Shader.Parameters["fadeOut"].SetValue(false);

            float sine = (1f + Sin(Main.GlobalTimeWrappedHourly)) / 2f;
            float opacity = Lerp(0.06f, 0.12f, sine);

            // Draw the overlay.
            DrawData overlay = new(npcTexture, npc.Center - Main.screenPosition, npc.frame, Color.White * opacity, 0f, npc.frame.Size() * 0.5f, 1f, direction, 0);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(overlay);
            overlay.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
        }
        #endregion

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            DespawnTransitionProjectiles();
            SelectNewAttack(commander, ref commander.ai[1], (float)GuardiansAttackType.DefenderDeathAnimation);
            npc.life = npc.lifeMax;
            npc.netUpdate = true;
            return false;
        }
        #endregion
    }
}
