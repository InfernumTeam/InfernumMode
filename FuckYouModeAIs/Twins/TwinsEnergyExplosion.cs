using CalamityMod.Projectiles;
using CalamityMod.Projectiles.DraedonsArsenal;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Twins
{
    public class TwinsEnergyExplosion : ModProjectile
	{
		public ref float OwnerType => ref projectile.ai[0];
		public ref float Radius => ref projectile.ai[1];
		public const int Lifetime = 80;

		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Boom");
        }

        public override void SetDefaults()
        {
            projectile.width = 72;
            projectile.height = 72;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = Lifetime;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;
            projectile.scale = 0.001f;
        }

		public override void AI()
		{
			if (projectile.localAI[0] == 0f)
			{
				Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaGrenadeExplosion"), projectile.Center);
				projectile.localAI[0] = 1f;
			}
			Main.LocalPlayer.Infernum().CurrentScreenShakePower = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / Lifetime) * 14f + 2f;

			Radius = MathHelper.Lerp(Radius, 2516f, 0.15f);
			projectile.scale = MathHelper.Lerp(1.2f, 5f, Utils.InverseLerp(Lifetime, 0f, projectile.timeLeft, true));
			CalamityGlobalProjectile.ExpandHitboxBy(projectile, (int)(Radius * projectile.scale), (int)(Radius * projectile.scale));
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			spriteBatch.EnterShaderRegion();

			float pulseCompletionRatio = Utils.InverseLerp(Lifetime, 0f, projectile.timeLeft, true);
			Vector2 scale = new Vector2(1.5f, 1f);
			DrawData drawData = new DrawData(ModContent.GetTexture("Terraria/Misc/Perlin"),
				projectile.Center - Main.screenPosition + projectile.Size * scale * 0.5f,
				new Rectangle(0, 0, projectile.width, projectile.height),
				new Color(new Vector4(1f - (float)Math.Sqrt(pulseCompletionRatio))) * 0.7f * projectile.Opacity,
				projectile.rotation,
				projectile.Size,
				scale,
				SpriteEffects.None, 0);

			Color pulseColor = OwnerType == NPCID.Spazmatism ? Color.MediumPurple : Color.Red;
			GameShaders.Misc["ForceField"].UseColor(pulseColor);
			GameShaders.Misc["ForceField"].Apply(drawData);
			drawData.Draw(spriteBatch);

			spriteBatch.ExitShaderRegion();
			return false;
		}
	}
}
