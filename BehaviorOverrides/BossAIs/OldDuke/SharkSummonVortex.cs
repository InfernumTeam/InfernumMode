using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.OldDuke;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.OldDuke
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
            Projectile.timeLeft = 120;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(MathHelper.Pi * Time / 120f) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            Projectile.rotation -= Projectile.Opacity * 0.1f;

            float brightnessFactor = Projectile.scale * 2f;
            Lighting.AddLight(Projectile.Center, brightnessFactor, brightnessFactor * 2f, brightnessFactor);

            if (Time == 0f)
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/OldDukeVortex"), Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 12f == 11f)
            {
                Vector2 sharkVelocity = (MathHelper.TwoPi * Time / 120f).ToRotationVector2() * 8f;
                int shark = NPC.NewNPC(new InfernumSource(), (int)Projectile.Center.X, (int)Projectile.Center.Y, ModContent.NPCType<OldDukeSharkron>());
                if (Main.npc.IndexInRange(shark))
                {
                    Main.npc[shark].velocity = sharkVelocity;
                    Main.npc[shark].ai[1] = 1f;
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

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<Irradiated>(), 600);

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;
    }
}
