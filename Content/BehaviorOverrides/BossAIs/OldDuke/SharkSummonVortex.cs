using CalamityMod.NPCs.OldDuke;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.OldDuke
{
    public class SharkSummonVortex : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sulphurous Vortex");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 408;
            Projectile.height = 408;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 72;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Sin(Pi * Time / 72f) * 1.35f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Projectile.scale = Projectile.Opacity;

            Projectile.rotation -= Projectile.Opacity * 0.1f;

            float brightnessFactor = Projectile.scale * 2f;
            Lighting.AddLight(Projectile.Center, brightnessFactor, brightnessFactor * 2f, brightnessFactor);

            if (Time == 0f)
                SoundEngine.PlaySound(OldDukeVortex.SpawnSound, Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 10f == 9f)
            {
                Vector2 sharkVelocity = (TwoPi * Time / 120f).ToRotationVector2() * 8f;
                int shark = NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y, ModContent.NPCType<SulphurousSharkron>(), 0, 0f, 1f);
                if (Main.npc.IndexInRange(shark))
                {
                    Main.npc[shark].velocity = sharkVelocity;
                    Main.npc[shark].netUpdate = true;
                }
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity > 0f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float dist1 = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            float dist2 = Vector2.Distance(Projectile.Center, targetHitbox.TopRight());
            float dist3 = Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft());
            float dist4 = Vector2.Distance(Projectile.Center, targetHitbox.BottomRight());

            float minDist = dist1;
            if (dist2 < minDist)
                minDist = dist2;
            if (dist3 < minDist)
                minDist = dist3;
            if (dist4 < minDist)
                minDist = dist4;

            return minDist <= 210f * Projectile.scale;
        }
    }
}
