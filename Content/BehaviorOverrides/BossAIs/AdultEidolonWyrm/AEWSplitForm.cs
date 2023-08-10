using CalamityMod;
using CalamityMod.Dusts;
using InfernumMode.Content.BehaviorOverrides.AbyssAIs;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWSplitForm : ModProjectile
    {
        public bool DarkForm
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public bool HasCreatedIcicleBurstSpread
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        public const int SegmentCount = 60;

        public const float OffsetPerSegment = 72f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Adult Eidolon Wyrm");

        public override void SetDefaults()
        {
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 6;
            Projectile.timeLeft = Projectile.MaxUpdates * 240;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Emit particles.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.WithinRange(target.Center, 1000f) && Projectile.FinalExtraUpdate())
                EmitParticles();

            // Create a burst of icicles if the dark form collides with another split AEW.
            if (DarkForm && !HasCreatedIcicleBurstSpread)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type != Projectile.type)
                        continue;

                    if (!Main.projectile[i].active)
                        continue;

                    if (!Main.projectile[i].Hitbox.Intersects(Projectile.Hitbox))
                        continue;

                    if (i == Projectile.whoAmI)
                        continue;

                    HasCreatedIcicleBurstSpread = true;
                    Projectile.netUpdate = true;

                    Main.projectile[i].ModProjectile<AEWSplitForm>().HasCreatedIcicleBurstSpread = true;
                    Main.projectile[i].netUpdate = true;
                }

                if (HasCreatedIcicleBurstSpread)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 27; i++)
                        {
                            Vector2 icicleVelocity = (TwoPi * i / 27f).ToRotationVector2() * 8.4f;
                            Utilities.NewProjectileBetter(Projectile.Center, icicleVelocity, ModContent.ProjectileType<EidolistIce>(), AEWHeadBehaviorOverride.NormalShotDamage, 0f, -1, 0f, 1f);
                        }
                        for (int i = 0; i < 14; i++)
                        {
                            Vector2 icicleVelocity = (TwoPi * i / 14f).ToRotationVector2() * 16f;
                            Utilities.NewProjectileBetter(Projectile.Center, icicleVelocity, ModContent.ProjectileType<EidolistIce>(), AEWHeadBehaviorOverride.NormalShotDamage, 0f, -1, 0f, 1f);
                        }
                    }

                    Utilities.CreateShockwave(Projectile.Center);
                }
            }

            // Fade away once the split versions have dissipated.
            if (HasCreatedIcicleBurstSpread && Projectile.FinalExtraUpdate())
            {
                Projectile.Opacity -= 0.06f;
                if (Projectile.Opacity <= 0f)
                    Projectile.Kill();
            }
        }

        public void EmitParticles()
        {
            Vector2 idealParticleVelocity = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 6f);

            if (DarkForm)
            {
                for (int i = 0; i < 4; i++)
                {
                    Dust darkMatter = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.Asphalt, 0f, -3f, 0, default, 1.4f);
                    darkMatter.noGravity = true;
                    darkMatter.velocity = Vector2.Lerp(darkMatter.velocity, idealParticleVelocity, 0.55f);
                    darkMatter.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    Dust light = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, -3f, 0, default, 1.3f);
                    light.noGravity = true;
                    light.velocity = Vector2.Lerp(light.velocity, idealParticleVelocity, 0.55f);
                    light.fadeIn = Main.rand.NextFloat(0.4f, 0.85f);
                    light.noLight = true;
                    light.color = Color.Yellow;
                    light.scale = 2f;
                }

                for (int i = 0; i < 2; i++)
                {
                    Dust light = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(60f, 60f), ModContent.DustType<AuricBarDust>(), Main.rand.NextVector2Circular(2f, 2f));
                    light.noGravity = true;
                    light.alpha = 127;
                    light.scale = 3f;
                    light.fadeIn = 1.1f;
                }
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return (DarkForm ? new Color(65, 41, 132, 100) : new Color(255, 178, 167, 0)) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 decideDrawPosition(int index)
            {
                return Projectile.Center - Main.screenPosition - Projectile.velocity.SafeNormalize(Vector2.UnitY) * OffsetPerSegment * Projectile.scale * index;
            }

            static Texture2D decideSegmentTexture(int index)
            {
                // By default, segments are heads.
                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AdultEidolonWyrm/PrimordialWyrmHead").Value;

                // After the head is drawn, use body segments.
                if (index >= 1)
                {
                    string bodyTexturePath = "CalamityMod/NPCs/AdultEidolonWyrm/PrimordialWyrmBody";
                    if (index % 2 == 1)
                        bodyTexturePath += "Alt";

                    texture = ModContent.Request<Texture2D>(bodyTexturePath).Value;
                }

                // The last segment should be a tail.
                if (index == SegmentCount)
                    texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AdultEidolonWyrm/PrimordialWyrmTail").Value;

                return texture;
            }

            // Draw shadow afterimages. This cannot be performed in the main loop due to layering problems, specifically with new segments overlapping the afterimages.
            for (int i = 0; i < SegmentCount + 1; i++)
            {
                Texture2D texture = decideSegmentTexture(i);
                Color color = (DarkForm ? new Color(103, 84, 164, 0) : new Color(244, 207, 112, 0)) * Projectile.Opacity;
                Vector2 drawPosition = decideDrawPosition(i);

                for (int j = 0; j < 3; j++)
                {
                    float offsetAngle = Lerp(-PiOver2, PiOver2, j / 2f);
                    Vector2 drawOffset = (Projectile.rotation + offsetAngle).ToRotationVector2() * Projectile.scale * new Vector2(10f, 5f);
                    ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition + drawOffset, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0));
                }
            }

            // Draw the main body.
            for (int i = 0; i < SegmentCount + 1; i++)
            {
                Texture2D texture = decideSegmentTexture(i);
                Color color = Projectile.GetAlpha(Color.White);
                for (int j = 0; j < 2; j++)
                    ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, decideDrawPosition(i), null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0));
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < SegmentCount + 1; i++)
            {
                Vector2 segmentCenter = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * OffsetPerSegment * Projectile.scale * i;
                if (Utils.CenteredRectangle(segmentCenter, Projectile.Size).Intersects(targetHitbox))
                    return true;
            }

            return false;
        }
    }
}
