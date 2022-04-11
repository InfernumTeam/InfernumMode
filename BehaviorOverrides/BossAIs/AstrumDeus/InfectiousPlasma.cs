using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class InfectiousPlasma : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Plasma");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 720;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.9875f;
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft < 540 && Projectile.timeLeft % 80f == 79f)
            {
                Vector2 plasmaVelocity = -Vector2.UnitY.RotatedByRandom(0.56f) * Main.rand.NextFloat(7f, 16f);
                Utilities.NewProjectileBetter(Projectile.Center, plasmaVelocity, ModContent.ProjectileType<PlasmaDrop>(), 160, 0f);
            }

            Projectile.Opacity = Utils.GetLerpValue(720f, 700f, Projectile.timeLeft, true) * Utils.GetLerpValue(5f, 30f, Projectile.timeLeft, true);
            Projectile.scale = MathHelper.Lerp(0.65f, 0.25f, Utils.GetLerpValue(325f, 30f, Projectile.timeLeft, true));
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity > 0.7f;

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
            color = Color.Lerp(color, Color.White, 0.35f);
            color.A = 0;
            return color * Projectile.Opacity * 0.45f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D plasmaTexture = Main.projectileTexture[Projectile.type];
            for (int i = 0; i < 5; i++)
            {
                Matrix drawOffsetEncoding = Matrix.CreateRotationX(Main.GlobalTimeWrappedHourly * 2.32f + i * 1.37f);
                drawOffsetEncoding *= Matrix.CreateRotationZ(Main.GlobalTimeWrappedHourly * 1.77f - i * 1.83f);
                Vector3 vectorizedOffset = Vector3.Transform(Vector3.Forward, drawOffsetEncoding) * 0.5f + new Vector3(0.5f);
                Vector2 drawOffset = new Vector2(vectorizedOffset.X, vectorizedOffset.Y) * MathHelper.Lerp(1f, 16f, vectorizedOffset.Z);
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;

                spriteBatch.Draw(plasmaTexture, drawPosition, null, Projectile.GetAlpha(Color.White), drawOffset.ToRotation(), plasmaTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
    }
}
