using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SuicideBomberRitual : ModProjectile, IAdditiveDrawer
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 84;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Ritual");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 60f, Time, true);
            Projectile.scale = Projectile.Opacity;
            Projectile.direction = (Projectile.identity % 2 == 0).ToDirectionInt();
            Projectile.rotation += Projectile.direction * 0.18f;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BrimstoneDemonSummonExplosion>(), 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor) => false;
        
        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/SupremeCalamitas/SuicideBomberRitual").Value;
            Texture2D innerCircle = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/SupremeCalamitas/SuicideBomberRitualCircleInner").Value;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.Red, Color.Blue, Projectile.identity / 6f % 1f));
            Color color2 = Projectile.GetAlpha(Color.Lerp(Color.Red, Color.Blue, (Projectile.identity / 6f + 0.27f) % 1f));
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
            spriteBatch.Draw(innerCircle, drawPosition, null, color2, -Projectile.rotation, innerCircle.Size() * 0.5f, Projectile.scale, 0, 0f);
        }
    }
}
