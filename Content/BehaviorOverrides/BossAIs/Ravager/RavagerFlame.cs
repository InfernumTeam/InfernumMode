using CalamityMod.NPCs;
using InfernumMode.Content.Dusts;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class RitualFlame : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Blue Flame");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 540;
            
        }

        public override void AI()
        {
            Dust blueFlame = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<RavagerMagicDust>(), 0f, 0f, 100, default, 1.5f);
            blueFlame.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
            blueFlame.noGravity = true;

            if (Projectile.ai[0] == 0f)
            {
                Projectile.ai[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
            }

            // Fuck off if the Ravager isn't around.
            if (CalamityGlobalNPC.scavenger < 0 || !Main.npc[CalamityGlobalNPC.scavenger].active)
            {
                Projectile.active = false;
                return;
            }

            NPC ravager = Main.npc[CalamityGlobalNPC.scavenger];

            Projectile.velocity = (Projectile.velocity * 24f + Projectile.SafeDirectionTo(ravager.Center) * 13f) / 25f;
            if (Projectile.WithinRange(ravager.Center, 100f))
                Projectile.Kill();
        }
    }
}
