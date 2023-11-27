using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.ScreenEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolySpear : ModProjectile
    {
        public Vector2 CurrentDirectionEdge
        {
            get
            {
                if (InLava)
                    return Vector2.UnitY;

                float bestOrthogonality = -100000f;
                Vector2 aimDirection = (Projectile.rotation - PiOver4).ToRotationVector2();
                Vector2 edge = Vector2.Zero;
                Vector2[] edges = new Vector2[]
                {
                    Vector2.UnitX,
                    -Vector2.UnitX,
                    Vector2.UnitY,
                    -Vector2.UnitY
                };

                // Determine which edge the current direction aligns with most based on dot products.
                for (int i = 0; i < edges.Length; i++)
                {
                    float orthogonality = Vector2.Dot(aimDirection, edges[i]);
                    if (orthogonality > bestOrthogonality)
                    {
                        edge = edges[i];
                        bestOrthogonality = orthogonality;
                    }
                }

                return edge;
            }
        }

        public bool SpawnedInBlocks
        {
            get;
            set;
        }

        public bool BeenInBlockSinceStart
        {
            get;
            set;
        }

        public bool InLava
        {
            get
            {
                IEnumerable<Projectile> lavaProjectiles = Utilities.AllProjectilesByID(ModContent.ProjectileType<ProfanedLava>());
                if (!lavaProjectiles.Any())
                    return false;

                Rectangle tipHitbox = Utils.CenteredRectangle(Projectile.Center + (Projectile.rotation - PiOver4).ToRotationVector2() * 60f, Vector2.One);
                return lavaProjectiles.Any(l => l.Colliding(l.Hitbox, tipHitbox));
            }
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float DeathCountdown => ref Projectile.ai[1];

        public static int DeathDelay => 90;

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/CommanderSpear2";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 124;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            bool tileCollision = Collision.SolidCollision(Projectile.Top, Projectile.width, Projectile.height);
            if (tileCollision && Time <= 1f)
            {
                SpawnedInBlocks = true;
                BeenInBlockSinceStart = false;
            }
            if (SpawnedInBlocks && !tileCollision && Time >= 35f)
                BeenInBlockSinceStart = false;

            // Decide the rotation of the spear based on velocity, if there is any.
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

            // Handle death effects.
            if (DeathCountdown >= 1f)
            {
                // Prevent a natural death disrupting the fire wall directions by locking the timeLeft variable in place.
                Projectile.timeLeft = 60;

                // Release fire pillars.
                if (DeathCountdown % 5f == 3f)
                {
                    float perpendicularOffset = Utils.Remap(DeathCountdown, DeathDelay, 0f, 0f, 3600f);
                    Vector2 pillarDirection = -(Projectile.rotation - PiOver4).ToRotationVector2();
                    if (InLava)
                        pillarDirection = -Vector2.UnitY;

                    // Make the gaps a bit wider if the pillars will spawn at around a 45-degree inclination, since it's a bit too tight without this.
                    float evenAngle = pillarDirection.ToRotation();
                    if (evenAngle < 0f)
                        evenAngle += TwoPi;
                    bool closeTo45DegreeGap = Distance(evenAngle % PiOver2, PiOver4) < ToRadians(20f);
                    if (closeTo45DegreeGap)
                        perpendicularOffset *= 1.6f;

                    SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 pillarSpawnPosition = Projectile.Center + CurrentDirectionEdge.RotatedBy(PiOver2) * perpendicularOffset - pillarDirection * 800f;
                        Utilities.NewProjectileBetter(pillarSpawnPosition, pillarDirection, ModContent.ProjectileType<HolySpearFirePillar>(), 400, 0f);

                        pillarSpawnPosition = Projectile.Center - CurrentDirectionEdge.RotatedBy(PiOver2) * perpendicularOffset - pillarDirection * 800f;
                        Utilities.NewProjectileBetter(pillarSpawnPosition, pillarDirection, ModContent.ProjectileType<HolySpearFirePillar>(), 400, 0f);
                    }
                }

                Projectile.velocity *= 0.93f;
                DeathCountdown--;
                if (DeathCountdown <= 0f)
                    Projectile.Kill();
            }

            // Stick to lava.
            else if (InLava)
                PrepareForDeath(Projectile.velocity);

            // Wait a little bit before interacting with tiles.
            int collideDelay = SpawnedInBlocks ? 65 : 24;
            Projectile.tileCollide = Time >= collideDelay && !BeenInBlockSinceStart;
            Time++;
        }

        public void PrepareForDeath(Vector2 oldVelocity)
        {
            if (DeathCountdown > 0f)
                return;

            if (Main.netMode != NetmodeID.Server)
                Utilities.NewProjectileBetter(Projectile.Center + oldVelocity.SafeNormalize(Vector2.UnitY) * 60f, oldVelocity, ModContent.ProjectileType<StrongProfanedCrack>(), 0, 0f);

            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 9f;
            ScreenEffectSystem.SetBlurEffect(Projectile.Center, 0.2f, 18);

            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceSpearHitSound with { Volume = 2f }, Projectile.Center);
            Projectile.velocity *= InLava ? 0.6f : 0f;
            Projectile.Center += oldVelocity.SafeNormalize(Vector2.Zero) * 50f;
            DeathCountdown = DeathDelay;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            PrepareForDeath(oldVelocity);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            // Burst into lava metaballs on death.
            if (Main.netMode != NetmodeID.Server)
                ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticles(ModContent.Request<Texture2D>(Texture).Value.CreateMetaballsFromTexture(Projectile.Center, Projectile.rotation, Projectile.scale, 20f, 30));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float burnInterpolant = Utils.GetLerpValue(45f, 0f, Time, true);
            float drawOffsetRadius = burnInterpolant * 16f;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.White, Color.Yellow with { A = 0 } * 0.6f, burnInterpolant));
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            if (ProvidenceBehaviorOverride.IsEnraged)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/CommanderSpear2Night").Value;

            // Draw the spear as a white hot flame with additive blending before it converge inward to create the actual spear.
            for (int i = 0; i < 10; i++)
            {
                float rotation = Projectile.rotation + Lerp(-0.16f, 0.16f, i / 9f) * burnInterpolant;
                Vector2 drawOffset = (TwoPi * i / 10f).ToRotationVector2() * drawOffsetRadius;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;
                Main.EntitySpriteDraw(texture, drawPosition, null, color, rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
            }
            return false;
        }
    }
}
