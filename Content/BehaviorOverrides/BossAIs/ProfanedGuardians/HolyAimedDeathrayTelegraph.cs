using InfernumMode.Common.Graphics;
using InfernumMode.Content.BehaviorOverrides.BossAIs.WallOfFlesh;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyAimedDeathrayTelegraph : FireBeamTelegraph, IScreenCullDrawer
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 72;
        }
        public override bool PreDraw(ref Color lightColor) => false;

        public override void Kill(int timeLeft)
        {
            if (Target.Center.Y > (Main.maxTilesY - 300f) * 16f)
                SoundEngine.PlaySound(SoundID.Item74, Target.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 beamDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Utilities.NewProjectileBetter(Projectile.Center, beamDirection, ModContent.ProjectileType<HolyAimedDeathray>(), 220, 0f, -1, 0f, Owner.whoAmI);
        }

        public void CullDraw(SpriteBatch spriteBatch)
        {
            Vector2 aimDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i <= 4; i++)
            {
                float lineWidth = MathHelper.SmoothStep(0.25f, 1f, i / 4f) * Projectile.scale;
                Color lineColor = Color.Lerp(Color.White, Color.Orange, MathHelper.Lerp(0.15f, 1f, i / 4f));
                lineColor.A = 0;

                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + aimDirection * 6200f, lineColor, lineWidth);
            }
        }
    }
}
