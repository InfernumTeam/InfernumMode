using CalamityMod;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class CommanderSpearThrown : ModProjectile
    {
        public const int TelegraphTime = 30;

        public const int PassThroughTilesTime = 15;

        public ref float Timer => ref Projectile.ai[0];

        public bool ExplodeOnImpact => Projectile.ai[1] == 1;

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/CommanderSpear";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Profaned Spear");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 180;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Don't do anything if the telegraphs are being drawn.
            if (Timer < TelegraphTime)
            {
                Timer++;
                return;
            }

            if (ExplodeOnImpact)
                Projectile.tileCollide = Timer >= TelegraphTime + PassThroughTilesTime;

            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.08f, 0f, 1f);

            // Accelerate.
            if (Projectile.velocity.Length() < 36f)
                Projectile.velocity *= 1.05f;

            for (int i = 0; i < 40; i++)
            {
                // Bias towards lower values. 
                float size = Pow(Main.rand.NextFloat(), 2f);
                ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(Projectile.Center - (Projectile.velocity * 0.5f) + (Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f) * size),
                    Vector2.Zero, new(Main.rand.NextFloat(10f, 15f)), 0.9f);
            }

            Lighting.AddLight(Projectile.Center, Vector3.One);
            Timer++;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override bool ShouldUpdatePosition() => Timer >= TelegraphTime;

        public override void OnKill(int timeLeft)
        {
            if (!ExplodeOnImpact)
                return;
            ScreenEffectSystem.SetBlurEffect(Projectile.Center, 1f, 45);
            SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, Projectile.Center);
            SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelHitSound with { Volume = 2f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.65f, Volume = 1.6f }, Projectile.Center);

            GuardianComboAttackManager.CreateFireExplosion(Projectile.Center, true);

            for (int i = 0; i < 100; i++)
                ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(Projectile.Center + Main.rand.NextVector2Circular(100f, 100f), Vector2.Zero, new(Main.rand.NextFloat(52f, 85f)));

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int crossWaves = 2;
            int crossCount = 18;
            for (int i = 0; i < crossWaves; i++)
            {
                float speed = i switch
                {
                    0 => 8.5f,
                    1 => 5f,
                    _ => 2.5f
                };

                for (int j = 0; j < crossCount; j++)
                {
                    Vector2 crossVelocity = (TwoPi * j / crossCount + PiOver4 * i).ToRotationVector2() * speed;
                    Utilities.NewProjectileBetter(Projectile.Center + crossVelocity, crossVelocity, ModContent.ProjectileType<HolyCross>(), GuardianComboAttackManager.HolyCrossDamage, 0f);
                }
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Timer <= TelegraphTime)
            {
                float opacity = CalamityUtils.Convert01To010(Timer / TelegraphTime);
                BloomLineDrawInfo lineInfo = new(rotation: -Projectile.velocity.ToRotation(),
                    width: 0.003f + Pow(opacity, 5f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f),
                    bloom: Lerp(0.06f, 0.16f, opacity),
                    scale: Vector2.One * 1950f,
                    main: WayfinderSymbol.Colors[1],
                    darker: WayfinderSymbol.Colors[2],
                    opacity: opacity,
                    bloomOpacity: 0.4f,
                    lightStrength: 5f);

                Utilities.DrawBloomLineTelegraph(Projectile.Center - Main.screenPosition, lineInfo);
                return false;
            }
            if (ExplodeOnImpact)
            {
                Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

                // Draw the spear as a white hot flame with additive blending before it converge inward to create the actual spear.
                for (int i = 0; i < 5; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / 5f).ToRotationVector2() * 5f;
                    Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;
                    Main.EntitySpriteDraw(texture, drawPosition, null, Color.LightPink with { A = 0 }, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
                }
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White with { A = 150 }, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
                return false;
            }
            float alpha = 1f - (float)Projectile.alpha / 255;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor * alpha, 1);
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, Color.White, 2f);
            return false;
        }
    }
}
