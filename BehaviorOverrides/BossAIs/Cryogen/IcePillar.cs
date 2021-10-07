using CalamityMod;
using InfernumMode.Miscellaneous;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cryogen
{
	public class IcePillar : ModProjectile
    {
		public ref float MaxPillarHeight => ref projectile.ai[0];
		public ref float Time => ref projectile.ai[1];
		public float CurrentHeight = 0f;
		public const float StartingHeight = 30f;
		public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Pillar");

		public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 480;
        }

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(CurrentHeight);
			writer.Write(projectile.rotation);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			CurrentHeight = reader.ReadSingle();
			projectile.rotation = reader.ReadSingle();
		}

		public override void AI()
        {
			Time++;

			projectile.extraUpdates = Time < 60f ? 0 : 1;

			// Fade in at the beginning of the projectile's life.
			if (Time < 60f)
				projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 1f, 0.35f);

			// Stop doing damage at the end of the projectile's life.
			else if (projectile.timeLeft < 40f)
				projectile.damage = 0;

			// Initialize the pillar.
			if (Main.netMode != NetmodeID.MultiplayerClient && MaxPillarHeight == 0f)
				InitializePillarProperties();

			// Quickly rise.
			if (Main.netMode != NetmodeID.MultiplayerClient && Time >= 60f && Time < 75f)
			{
				CurrentHeight = MathHelper.Lerp(StartingHeight, MaxPillarHeight, Utils.InverseLerp(60f, 75f, Time, true));
				if (Time % 6 == 0)
					projectile.netUpdate = true;
			}

			// Play a sound when rising.
			if (Time == 70)
			{
				Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
				Main.PlaySound(SoundID.Item45, target.Center);
			}
        }

		public void InitializePillarProperties()
		{
			WorldUtils.Find(new Vector2(projectile.Top.X, projectile.Top.Y - 160).ToTileCoordinates(), Searches.Chain(new Searches.Down(6000), new GenCondition[]
			{
				new Conditions.IsSolid(),
				new CustomTileConditions.ActiveAndNotActuated(),
				new CustomTileConditions.NotPlatform()
			}), out Point newBottom);

			bool isHalfTile = CalamityUtils.ParanoidTileRetrieval(newBottom.X, newBottom.Y - 1).halfBrick();
			projectile.Bottom = newBottom.ToWorldCoordinates(8, isHalfTile ? 8 : 0);

			Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
			MaxPillarHeight = MathHelper.Max(0f, projectile.Top.Y - target.Top.Y) + StartingHeight + 100f + Math.Abs(target.velocity.Y * 15f);

			CurrentHeight = StartingHeight;

			if (!Collision.CanHit(projectile.Bottom - Vector2.UnitY * 10f, 2, 2, projectile.Bottom - Vector2.UnitY * 32f, 2, 2))
				projectile.Kill();

			projectile.netUpdate = true;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D tipTexture = ModContent.GetTexture(Texture);
			Vector2 aimDirection = Vector2.UnitY.RotatedBy(projectile.rotation);
			if (Time < 60f)
			{
				float telegraphLineWidth = (float)Math.Sin(Time / 60f * MathHelper.Pi) * 5f;
				if (telegraphLineWidth > 3f)
					telegraphLineWidth = 3f;
				spriteBatch.DrawLineBetter(projectile.Top + aimDirection * 10f, projectile.Top + aimDirection * -MaxPillarHeight, Color.LightCyan, telegraphLineWidth);
			}

			float tipBottom = 0f;
			Vector2 scale = new Vector2(projectile.scale, 1f);

			DrawPillar(spriteBatch, scale, aimDirection, ref tipBottom);

			Vector2 tipDrawPosition = projectile.Bottom - aimDirection * (tipBottom + 4f) - Main.screenPosition;
			spriteBatch.Draw(tipTexture, tipDrawPosition, null, projectile.GetAlpha(Color.White), projectile.rotation, tipTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			return false;
		}

		public void DrawPillar(SpriteBatch spriteBatch, Vector2 scale, Vector2 aimDirection, ref float tipBottom)
		{
			Texture2D pillarBodyPiece = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Cryogen/IcePillarPiece");

			for (int i = pillarBodyPiece.Height; i < CurrentHeight + pillarBodyPiece.Height; i += pillarBodyPiece.Height)
			{
				Vector2 drawPosition = projectile.Bottom - aimDirection * i - Main.screenPosition;
				spriteBatch.Draw(pillarBodyPiece, drawPosition, null, projectile.GetAlpha(Color.White), projectile.rotation, pillarBodyPiece.Size() * new Vector2(0.5f, 0f), scale, SpriteEffects.None, 0f);
				tipBottom = i;
			}
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

		public override bool CanDamage() => Time >= 70f;

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.Item51, projectile.Center);

			int spikeCount = (int)MathHelper.Lerp(1f, 4f, Utils.InverseLerp(100f, 960f, CurrentHeight, true));
			Vector2 aimDirection = Vector2.UnitY.RotatedBy(projectile.rotation);
			for (int i = 0; i < spikeCount; i++)
			{
				Vector2 icicleSpawnPosition = projectile.Bottom - aimDirection * CurrentHeight * i / spikeCount;
				icicleSpawnPosition -= aimDirection * Main.rand.NextFloatDirection() * 20f + Main.rand.NextVector2Circular(8f, 8f);
				Vector2 icicleShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 10f);
				Utilities.NewProjectileBetter(icicleSpawnPosition, icicleShootVelocity, ModContent.ProjectileType<AimedIcicleSpike>(), 145, 0f);
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			Vector2 start = projectile.Bottom;
			Vector2 end = projectile.Bottom - Vector2.UnitY.RotatedBy(projectile.rotation) * CurrentHeight;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, projectile.width * projectile.scale, ref _);
		}
	}
}

