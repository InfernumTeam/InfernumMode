using CalamityMod.NPCs;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class EidolistIce : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/AbyssAIs/AbyssalIce";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Abyssal Ice");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            if (CalamityGlobalNPC.adultEidolonWyrmHead != -1)
                CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 32f, Projectile.timeLeft, true);

            if (Projectile.ai[1] == 1f && Projectile.velocity.Length() < 39f)
                Projectile.velocity *= 1.021f;

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            if (drawPosition.Between(Vector2.One * -300f, new(Main.screenWidth + 300f, Main.screenHeight + 300f)))
                ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0));
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
