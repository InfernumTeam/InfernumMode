using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class GiantAstralStar : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;
        public ref float Time => ref projectile.ai[0];
        public ref float Radius => ref projectile.ai[1];
        public ref float AngerOnCreation => ref projectile.localAI[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Consumed Astral Star");

        public override void SetDefaults()
        {
            projectile.width = 164;
            projectile.height = 164;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 9000;
            projectile.scale = 0.2f;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(AngerOnCreation);

        public override void ReceiveExtraAI(BinaryReader reader) => AngerOnCreation = reader.ReadSingle();

        public override void AI()
        {
            Radius = projectile.scale * MathHelper.Lerp(72f, 90f, AngerOnCreation);

            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHeadSpectral>()) && projectile.timeLeft > 30)
                projectile.timeLeft = 30;

            if (projectile.timeLeft < 30f)
            {
                projectile.scale = MathHelper.Lerp(projectile.scale, 0.015f, 0.1f);
                Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.InverseLerp(18f, 8f, projectile.timeLeft, true) * 12f;
            }

            if (projectile.velocity != Vector2.Zero && projectile.velocity.Length() < MathHelper.Lerp(18f, 24.5f, AngerOnCreation))
            {
                if (projectile.timeLeft > 160)
                    projectile.timeLeft = 160;
                projectile.velocity *= 1.024f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.scale >= 7f && Time % 45f == 44f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 fireVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(14f, 24f) * MathHelper.Lerp(1f, 1.56f, AngerOnCreation);
                    Utilities.NewProjectileBetter(projectile.Center, fireVelocity, ModContent.ProjectileType<AstralShot2>(), 165, 0f);
                }
            }

            Time++;
        }

        public float SunWidthFunction(float completionRatio) => Radius * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color SunColorFunction(float completionRatio) => Color.Lerp(new Color(237, 93, 83), Color.Red, (float)Math.Sin(MathHelper.Pi * completionRatio) * 0.2f + 0.3f) * projectile.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.45f);
            GameShaders.Misc["Infernum:Fire"].UseImage("Images/Misc/Perlin");

            List<float> rotationPoints = new List<float>();
            List<Vector2> drawPoints = new List<Vector2>();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 50f)
            {
                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + MathHelper.Pi * -0.27f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(projectile.Center - offsetDirection * Radius / 2f, projectile.Center + offsetDirection * Radius / 2f, i / 16f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 16);
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.CreateGenericDustExplosion(projectile.Center, 235, 105, 20f, 4.25f);
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/FlareSound"), projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 80; i++)
            {
                Vector2 fireVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(18f, 27f) * MathHelper.Lerp(1f, 1.56f, AngerOnCreation);
                Utilities.NewProjectileBetter(projectile.Center, fireVelocity, ModContent.ProjectileType<AstralShot2>(), 165, 0f);

                for (int j = 0; j < 4; j++)
                {
                    Vector2 sparkleVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(12f, 39f);
                    Utilities.NewProjectileBetter(projectile.Center + sparkleVelocity * 3f, sparkleVelocity, ModContent.ProjectileType<AstralSparkleBig>(), 0, 0f);
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
