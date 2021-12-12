using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
    public class PsionicOrb : ModProjectile
    {
        public PrimitiveTrailCopy OrbDrawer;
        public ref float Time => ref projectile.ai[0];
        public ref float PredictiveAimRotation => ref projectile.ai[1];
        public bool UseUndergroundAI
        {
            get => projectile.localAI[0] == 1f;
            set => projectile.localAI[0] = value.ToInt();
        }
        public int Lifetime => UseUndergroundAI ? 225 : 300;
        public int AttackCycleTime => UseUndergroundAI ? 95 : 120;
        public const int Radius = 30;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psychic Energy");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 34;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = Radius - 4;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(UseUndergroundAI);

        public override void ReceiveExtraAI(BinaryReader reader) => UseUndergroundAI = reader.ReadBoolean();

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / (float)Lifetime) * 5f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            projectile.velocity *= 0.97f;

            Player nearestTarget = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (Time % AttackCycleTime > AttackCycleTime * 0.667f)
            {
                if (Time % AttackCycleTime == (int)(AttackCycleTime * 0.667f) + 5)
                    Main.PlaySound(SoundID.Item125, nearestTarget.Center);

                if (Time % 9f == 8f)
                    ShootRay();
            }
            Vector2 aimVector = (nearestTarget.Center + nearestTarget.velocity * 32f - projectile.Center).SafeNormalize(Vector2.UnitY);
            PredictiveAimRotation = Vector2.Normalize(Vector2.Lerp(aimVector, PredictiveAimRotation.ToRotationVector2(), 0.02f)).ToRotation();
            Lighting.AddLight(projectile.Center, Color.Cyan.ToVector3() * 1.6f);
            Time++;
        }

        public void ShootRay()
		{
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 shootVelocity = PredictiveAimRotation.ToRotationVector2() * (UseUndergroundAI ? 11f : 13.25f);
                shootVelocity *= 0.5365f;
                int ray = Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ProjectileID.CultistBossLightningOrbArc, 125, 0f, 255);

                if (Main.projectile.IndexInRange(ray))
                {
                    Main.projectile[ray].ai[0] = shootVelocity.ToRotation();
                    Main.projectile[ray].ai[1] = Main.rand.Next(100);
                    Main.projectile[ray].tileCollide = false;
                }
            }

            for (int i = 0; i < 36; i++)
            {
                Dust psychicMagic = Dust.NewDustPerfect(projectile.Center, 264);
                psychicMagic.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 5f;
                psychicMagic.scale = 1.45f;
                psychicMagic.noLight = true;
                psychicMagic.noGravity = true;
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public float WidthFunction(float completionRatio)
		{
            float squeezeInterpolant = (float)Math.Pow(Utils.InverseLerp(0f, 0.27f, completionRatio, true), 0.9f) * Utils.InverseLerp(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(projectile.width * 0.1f, projectile.width, squeezeInterpolant) * projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
		{
            Color color = Color.Lerp(Color.Cyan, Color.White, (float)Math.Sin(Math.Pow(completionRatio, 2D) * MathHelper.Pi));
            color *= 1f - 0.5f * (float)Math.Pow(completionRatio, 3D);
            color *= projectile.Opacity * 3f;
            return color;
		}

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (OrbDrawer is null)
                OrbDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:BrainPsychic"]);

            List<Vector2> drawPoints = new List<Vector2>();

            // Create a charged circle out of several primitives.
            for (float offsetAngle = 0f; offsetAngle <= MathHelper.TwoPi; offsetAngle += MathHelper.Pi / 6f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + Main.GlobalTime * 2.6f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                    drawPoints.Add(Vector2.Lerp(projectile.Center - offsetDirection * Radius * 0.925f, projectile.Center + offsetDirection * Radius * 0.925f, i / 16f));

                OrbDrawer.Draw(drawPoints, projectile.Size * 0.5f - Main.screenPosition, 24);
            }
            return false;
        }
    }
}
