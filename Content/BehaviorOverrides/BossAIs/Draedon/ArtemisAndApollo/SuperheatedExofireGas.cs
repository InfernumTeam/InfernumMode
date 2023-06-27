using CalamityMod;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class SuperheatedExofireGas : ModProjectile, IAdditiveDrawer
    {
        public ref float LightPower => ref Projectile.ai[0];

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/NebulaGas1";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Superheated Fire");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.scale = 1.27f;
            Projectile.hide = true;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide scale and initial rotation on the first frame this projectile exists.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = Main.rand.NextFloat(1f, 1.7f);
                Projectile.rotation = Main.rand.NextFloat(TwoPi);
                Projectile.localAI[0] = 1f;
            }

            // Calculate light power. This checks below the position of the fog to check if this fog is underground.
            // Without this, it may render over the fullblack that the game renders for obscured tiles.
            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / Sqrt(3f);
            LightPower = Lerp(LightPower, lightPowerBelow, 0.15f);
            Projectile.Opacity = Utils.GetLerpValue(180f, 165f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true);
            Projectile.rotation += Projectile.velocity.X * 0.004f;
            Projectile.velocity *= 0.985f;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity > 0.6f;

        public override bool PreDraw(ref Color lightColor) => false;

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Vector2 screenArea = new(Main.screenWidth, Main.screenHeight);
            Rectangle screenRectangle = Utils.CenteredRectangle(Main.screenPosition + screenArea * 0.5f, screenArea * 1.33f);

            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20;
            if (!Projectile.Hitbox.Intersects(screenRectangle))
                return;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity;
            Color drawColor = new Color(255, 188, 89) * opacity;
            Vector2 scale = Projectile.Size / texture.Size() * Projectile.scale * 1.35f;
            spriteBatch.Draw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
