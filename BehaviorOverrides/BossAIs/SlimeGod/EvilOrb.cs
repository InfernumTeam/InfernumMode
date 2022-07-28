using CalamityMod;
using CalamityMod.NPCs.SlimeGod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class EvilOrb : ModProjectile
    {
        public int NPCToOrbit;

        public float OrbitAngularOffset;

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb Thing");
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.Opacity = 0f;
        }
        
        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            if (!Main.npc[NPCToOrbit].active)
            {
                Projectile.Kill();
                return;
            }

            // Orbit in place.
            NPC npcToOrbit = Main.npc[NPCToOrbit];
            Projectile.Center = npcToOrbit.Center + OrbitAngularOffset.ToRotationVector2() * MathHelper.Max(npcToOrbit.width, npcToOrbit.height) * 1.1f;
            OrbitAngularOffset += MathHelper.Pi * (1f - Time / Lifetime) / Lifetime * 0.96f;

            // Determine opacity.
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true);
            Time++;

            // Explode into fire.
            if (Time >= Lifetime)
            {
                for (int i = 0; i < 8; i++)
                {
                    Dust fire = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, 267);
                    fire.color = Projectile.GetAlpha(Color.White);
                    fire.color.A = 255;
                    fire.velocity = Main.rand.NextVector2Circular(4f, 4f);
                    fire.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float shootOffsetAngle = MathHelper.Lerp(-0.23f, 0.23f, i / 2f);
                        Vector2 shootVelocity = (Projectile.Center - npcToOrbit.Center).SafeNormalize(Vector2.UnitY).RotatedBy(shootOffsetAngle) * 5f;
                        int bolt = Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<EvilBolt>(), 95, 0f);
                        if (Main.projectile.IndexInRange(bolt))
                            Main.projectile[bolt].ai[0] = (Main.npc[NPCToOrbit].type == ModContent.NPCType<CrimulanSlimeGod>()).ToInt();
                    }
                }

                Projectile.Kill();
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.6f;

        public override Color? GetAlpha(Color lightColor)
        {
            Color c = Main.npc[NPCToOrbit].type == ModContent.NPCType<CrimulanSlimeGod>() ? Color.Yellow : Color.Lime;
            c.A /= 3;
            return c * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
