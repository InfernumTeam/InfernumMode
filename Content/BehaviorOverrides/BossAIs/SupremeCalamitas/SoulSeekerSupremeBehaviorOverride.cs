using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Particles;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.SupremeCalamitasBehaviorOverride;
using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SoulSeekerSupremeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SoulSeekerSupreme>();

        public override bool PreAI(NPC npc)
        {
            // Die if SCal is no longer present.
            if (CalamityGlobalNPC.SCal < 0 || !SCal.active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            npc.target = SCal.target;

            bool outerSeeker = npc.ai[2] == 1f;
            bool canFire = SCal.Infernum().ExtraAI[1] == 1f;
            Player target = Main.player[npc.target];
            Vector2 eyePosition = npc.Center + new Vector2(npc.direction == 1 ? 40f : -36f, 16f);
            ref float spinOffsetAngle = ref npc.ai[1];
            ref float attackTimer = ref npc.Infernum().ExtraAI[0];
            ref float dealDamage = ref npc.Infernum().ExtraAI[1];

            // Initialize the turn rotation.
            if (npc.localAI[0] == 0f)
            {
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(npc.position, npc.width, npc.height, (int)CalamityDusts.Brimstone, 0f, 0f, 100, default, 2f);
                npc.ai[1] = npc.ai[0];
                npc.localAI[0] = 1f;
            }

            // Increase DR if the target leaves SCal's arena.
            npc.Calamity().DR = SoulSeekerSupreme.NormalDR;
            if (Enraged)
                npc.Calamity().DR = 0.99999f;

            // Pick a target if the current one is invalid.
            if (npc.target < 0 || npc.target == Main.maxPlayers || target.dead || !target.active)
                npc.TargetClosest();

            // Pick a target if the current one is too far away.
            if (!npc.WithinRange(target.Center, CalamityGlobalNPC.CatchUpDistance200Tiles))
                npc.TargetClosest();

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

            // Disable natural knockback resistence. Apparently this is something that Calamity never disabled?
            npc.knockBackResist = 0f;

            // Deal damage if appropriate.
            npc.damage = dealDamage > 0f ? npc.defDamage : 0;

            // Spin around SCal's arena.
            Vector2 arenaCenter = SCal.Infernum().Arena.Center.ToVector2();
            npc.Center = arenaCenter - ToRadians(spinOffsetAngle).ToRotationVector2() * (outerSeeker ? 1000f : 500f);

            npc.direction = Sign(npc.SafeDirectionTo(target.Center).X);

            // Begin to disappear if SCal isn't doing the seekers attack anymore.
            npc.dontTakeDamage = true;
            if (SCal.ai[0] != (int)SCalAttackType.SummonSeekers)
            {
                npc.Opacity -= 0.1f;
                if (npc.Opacity <= 0f)
                    npc.active = false;

                npc.dontTakeDamage = true;
                return false;
            }

            // Spin around.
            //spinOffsetAngle += outerSeeker.ToDirectionInt() * 0.5f;

            // Release semi-inaccurate bombs towards the target.
            if (canFire)
                attackTimer++;

            if (dealDamage > 0f)
            {
                spinOffsetAngle += outerSeeker.ToDirectionInt() * 0.5f;
                npc.dontTakeDamage = false;
            }

            if (attackTimer >= 10f && dealDamage <= 4)
            {
                if (dealDamage == 0)
                {
                    SoundEngine.PlaySound(SoundID.DD2_WyvernScream with { Pitch = -0.2f, Volume = 1.2f }, npc.Center);
                    for (int i = 0; i < 19; i++)
                    {
                        Vector2 firePosition = npc.Center + Main.rand.NextVector2Circular(30, 30);
                        Color fireColor = Color.Lerp(Color.OrangeRed, Color.IndianRed, Main.rand.NextFloat(0.3f, 0.7f));
                        float fireScale = Main.rand.NextFloat(0.7f, 0.7f + 0.3f);
                        float fireRotationSpeed = Main.rand.NextFloat(-0.05f, 0.05f);

                        var particle = new HeavySmokeParticle(firePosition, Vector2.Zero, fireColor, 35, fireScale, 1, fireRotationSpeed, true, 0f, true);
                        GeneralParticleHandler.SpawnParticle(particle);
                    }
                }
                dealDamage++;
            }

            Rectangle screen = new((int)(Main.screenPosition.X - 50), (int)(Main.screenPosition.Y - 50), Main.screenWidth + 100, Main.screenHeight + 100);

            if (attackTimer % 136f >= 75f && attackTimer % 136f < 135f && screen.Contains(npc.Center.ToPoint()))
            {
                Color lightColor = Color.Lerp(Color.OrangeRed, Color.IndianRed, Main.rand.NextFloat(0f, 0.5f));

                if (Main.rand.NextBool())
                    lightColor = Color.Lerp(lightColor, Color.White, 0.6f);

                if (attackTimer % 2 == 0)
                {
                    int lightLifetime = Main.rand.Next(10, 14);
                    float squishFactor = 1.5f;
                    float scale = 0.2f;
                    Vector2 lightSpawnPosition = eyePosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f, 130f);

                    Vector2 lightPosition = eyePosition - npc.Center + arenaCenter - ToRadians(spinOffsetAngle + outerSeeker.ToDirectionInt() * 3.5f).ToRotationVector2() * (outerSeeker ? 1000f : 500f);

                    Vector2 lightVelocity = (lightPosition - lightSpawnPosition) / lightLifetime * 1.1f;


                    SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, scale, lightColor, lightLifetime, 1f, squishFactor, squishFactor * 4f);
                    GeneralParticleHandler.SpawnParticle(light);
                }

                // Create energy sparks at the center.
                Vector2 sparkPosition = eyePosition - npc.Center + arenaCenter - ToRadians(spinOffsetAngle + outerSeeker.ToDirectionInt() * 0.5f).ToRotationVector2() * (outerSeeker ? 1000f : 500f);
                CritSpark spark = new(sparkPosition, Main.rand.NextVector2Circular(8f, 8f), Color.IndianRed, Color.OrangeRed, 2f, 6, 0.01f, 7.5f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (attackTimer % 136f == 135f && !target.WithinRange(npc.Center, 400f))
            {
                SoundEngine.PlaySound(SCalNPC.BrimstoneBigShotSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int bombExplodeDelay = 90;
                    float bombExplosionRadius = 600f;
                    float bombShootSpeed = outerSeeker ? 8f : npc.Distance(target.Center) * 0.012f + 9f;
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                    {
                        bomb.timeLeft = bombExplodeDelay;
                        bomb.ModProjectile<DemonicBomb>().ExplodeIntoDarts = outerSeeker;
                    });
                    Vector2 bombShootVelocity = eyePosition.DirectionTo(target.Center).RotatedByRandom(0.3f) * bombShootSpeed;
                    Utilities.NewProjectileBetter(eyePosition, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f, -1, bombExplosionRadius);
                }
            }
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            SpriteEffects directions = npc.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (npc.Infernum().ExtraAI[1] > 0)
            {
                for (int i = 0; i < 16; i++)
                {
                    Vector2 offset = (Tau * i / 16f).ToRotationVector2() * (npc.Infernum().ExtraAI[1] / 5f * 4f);
                    Color color = Color.OrangeRed with { A = 0 } * 1.3f;
                    spriteBatch.Draw(texture, npc.Center + offset - Main.screenPosition, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, directions, 0f);
                }
            }
            spriteBatch.Draw(texture, npc.Center - Main.screenPosition, npc.frame, lightColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, directions, 0f);
            return false;
        }
    }
}
