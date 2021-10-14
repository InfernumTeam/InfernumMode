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

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
	public class CrystalPillar : ModProjectile
    {
        public bool DarknessVariant
		{
            get => projectile.ai[0] == 1f;
            set => projectile.ai[0] = value.ToInt();
		}
		public ref float MaxPillarHeight => ref projectile.ai[1];
		public float Time = 0f;
		public float CurrentHeight = 0f;
		public const float StartingHeight = 82f;
		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystal");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
            cooldownSlot = 1;
        }

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(Time);
			writer.Write(CurrentHeight);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			Time = reader.ReadSingle();
			CurrentHeight = reader.ReadSingle();
		}

		public override void AI()
        {
			Time++;

			if (Time < 60f)
				projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 1f, 0.35f);
			else if (projectile.timeLeft < 40f)
			{
				projectile.damage = 0;
				projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 0f, 0.25f);
			}

			// Initialize the pillar.
			if (MaxPillarHeight == 0f)
			{
				WorldUtils.Find(new Vector2(projectile.Top.X, projectile.Top.Y - 160).ToTileCoordinates(), Searches.Chain(new Searches.Down(6000), new GenCondition[]
				{
					new Conditions.IsSolid(),
					new CustomTileConditions.ActiveAndNotActuated()
				}), out Point newBottom);

				bool isHalfTile = CalamityUtils.ParanoidTileRetrieval(newBottom.X, newBottom.Y - 1).halfBrick();
				projectile.Bottom = newBottom.ToWorldCoordinates(8, isHalfTile ? 8 : 0);
				Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
				MaxPillarHeight = MathHelper.Max(0f, projectile.Top.Y - target.Top.Y) + StartingHeight + 180f + Math.Abs(target.velocity.Y * 60f);
				CurrentHeight = StartingHeight;

				projectile.netUpdate = true;
			}

			// Quickly rise.
			if (Time >= 60f && Time < 75f)
			{
				CurrentHeight = MathHelper.Lerp(StartingHeight, MaxPillarHeight, Utils.InverseLerp(60f, 75f, Time, true));
				if (Time == 74 || Time % 6 == 0)
					projectile.netUpdate = true;
			}

			// Play a sound when rising.
			if (Time == 70)
			{
				Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
				Main.PlaySound(SoundID.Item73, target.Center);
			}
        }


		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			if (Time < 60f)
			{
				float scale = (float)Math.Sin(Time / 60f * MathHelper.Pi) * 5f;
				if (scale > 1f)
					scale = 1f;
				scale *= 2f;
				Utils.DrawLine(spriteBatch, projectile.Top + Vector2.UnitY * 10f, projectile.Top + Vector2.UnitY * (-MaxPillarHeight + 240f), Color.LightGoldenrodYellow, Color.LightGoldenrodYellow, scale);
			}

			Texture2D tipTexture = ModContent.GetTexture(Texture);
			Texture2D pillarTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Providence/CrystalPillarBodyPiece");

			float tipBottom = 0f;
			Color drawColor = projectile.GetAlpha(Color.White);
			for (int i = pillarTexture.Height; i < CurrentHeight + pillarTexture.Height; i += pillarTexture.Height)
			{
				Vector2 drawPosition = projectile.Bottom - Vector2.UnitY * i - Main.screenPosition;
				spriteBatch.Draw(pillarTexture, drawPosition, null, drawColor, 0f, pillarTexture.Size() * new Vector2(0.5f, 0f), projectile.scale, SpriteEffects.None, 0f);
				tipBottom = i;
			}

			Vector2 tipDrawPosition = projectile.Bottom - Vector2.UnitY * (tipBottom - 8f) - Main.screenPosition;
			spriteBatch.Draw(tipTexture, tipDrawPosition, null, drawColor, 0f, tipTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
			return false;
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			Vector2 start = projectile.Bottom;
			Vector2 end = projectile.Bottom - Vector2.UnitY * (CurrentHeight - 8f);
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, projectile.width * projectile.scale, ref _);
		}
	}
}
