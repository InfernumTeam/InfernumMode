using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Projectiles
{
	public class ProvidenceArenaDelimiterProj : ModProjectile
	{
		private static Vector2 HeldItemOffset => new(22f, -25f);
		
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Arena Thing");
		}

		public override void SetDefaults()
		{
			Projectile.width = 2;
			Projectile.height = 2;
			Projectile.friendly = true;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Player player = Main.player[Projectile.owner];

			// If you're dumb enough to use this in multiplayer, nobody else run the code.
			if (Main.myPlayer != Projectile.owner)
				return;

			PoDPlayer csp = player.Infernum();

			// If the player is no longer actively channeling the item (or gets cursed or CCed...), try to write a schematic of their selected area.
			if (!player.channel || player.noItems || player.CCed)
			{
				if (csp.SelectedProvidenceArena != null)
					WorldSaveSystem.ProvidenceArena = csp.SelectedProvidenceArena.Value;
				Projectile.Kill();
				return;
			}

			// Get the cursor's tile position and apply it to one or both corners.
			Point cursorTilePos = Main.MouseWorld.ToTileCoordinates();

			// On frame 1, where the first corner won't exist yet, set that corner.
			if (!csp.CornerOne.HasValue)
				csp.CornerOne = cursorTilePos;

			// Set the second corner on every frame so you can drag it around.
			csp.CornerTwo = cursorTilePos;

			// Cool looking visuals that become stronger as you select more area
			Rectangle selection = csp.SelectedProvidenceArena.GetValueOrDefault();
			int area = selection.Width * selection.Height;
			double sqrtArea = Math.Sqrt(area);
			double lightScale = Math.Min(0.3 + 0.0325 * sqrtArea, 1.6);
			Lighting.AddLight(Projectile.Center, 0f, 0.7f * (float)lightScale, (float)lightScale);
			float dustRadius = (float)Math.Min(5D * sqrtArea, 150D);
			SpawnEnergyVacuumDust(area, dustRadius);

			// Set the projectile's position to be (roughly) the center of where the player is holding up the item.
			Vector2 offset = HeldItemOffset;
			if (player.direction == -1)
				offset.X = -offset.X - 7f;
			Projectile.Center = player.Center + offset;

			// Keep the player channeling this item as a holdout projectile while it is functioning.
			player.itemTime = 2;
			player.itemAnimation = 2;
		}

		private void SpawnEnergyVacuumDust(double area, float spawnRadius)
		{
			int dustCount = (int)(0.5 * Math.Pow(area, 2D/3D));
			int dustID = 56;
			float minScale = 0.4f;
			float maxScale = minScale + spawnRadius * 0.003f;
			Color dustColor = Color.Lerp(Color.Orange, Color.Yellow, Main.rand.NextFloat());
			for (int i = 0; i < dustCount; ++i)
			{
				Vector2 posOffset = Main.rand.NextVector2Circular(spawnRadius, spawnRadius);
				Vector2 dustPos = Projectile.Center + posOffset;
				Vector2 dustVel = posOffset * -0.08f;
				float dustScale = Main.rand.NextFloat(minScale, maxScale);
				Dust d = Dust.NewDustDirect(dustPos, 0, 0, dustID, 0.08f, 0.08f, newColor: dustColor);
				d.velocity = dustVel;
				d.noGravity = true;
				d.scale = dustScale;
			}
		}

		// Destroy the owner's corner data when the projectile expires for any reason.
		public override void Kill(int timeLeft)
		{
			Player player = Main.player[Projectile.owner];
			if (Main.myPlayer != Projectile.owner)
				return;
			
			PoDPlayer csp = player.Infernum();
			csp.CornerOne = csp.CornerTwo = null;
		}
	}
}
