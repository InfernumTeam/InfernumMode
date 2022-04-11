using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AimedDeathray : BaseLaserbeamProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float MaxScale => 1.05f;
        public override float MaxLaserLength => 2820f;
        public override float Lifetime => TelegraphTime + LaserDamageTime;
        public override Color LaserOverlayColor => Color.Lerp(Color.IndianRed, Color.Red, 0.6f) * 1.2f;
        public override Color LightCastColor => LaserOverlayColor;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayStart").Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayMid").Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd").Value;

        internal const float TelegraphTime = 120f;
        internal const float LaserDamageTime = 120f;

        // To allow easy, static access from different locations.
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Deathray");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange((int)Projectile.ai[1]) || !Main.npc[(int)Projectile.ai[1]].active)
                Projectile.Kill();

            if (Time == TelegraphTime + 1f)
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/PlasmaBolt"), Projectile.Center);

            Projectile.velocity = (Main.npc[(int)Projectile.ai[1]].rotation + MathHelper.PiOver2).ToRotationVector2();
            Projectile.Center = Main.npc[(int)Projectile.ai[1]].Center + Projectile.velocity * 96f;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (float i = 0f; i < LaserLength; i += 180f)
            {
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Vector2 shootVelocity = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * direction) * 4.7f;
                    Utilities.NewProjectileBetter(Projectile.Center + Projectile.velocity * i, shootVelocity, ProjectileID.DeathLaser, 130, 0f);
                }
            }
        }

        public override void DetermineScale()
        {
            float maxScale = Time > TelegraphTime ? MaxScale : MaxScale * 0.25f;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, (float)Math.Sin(Time / Lifetime * MathHelper.Pi) * 5f * maxScale, 0.1f);
            if (Projectile.scale > maxScale)
                Projectile.scale = maxScale;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool CanDamage() => Time > TelegraphTime;
    }
}
