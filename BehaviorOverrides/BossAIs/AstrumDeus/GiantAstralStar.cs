using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class GiantAstralStar : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;
        public ref float Time => ref Projectile.ai[0];
        public ref float Radius => ref Projectile.ai[1];
        public ref float AngerOnCreation => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Consumed Astral Star");

        public override void SetDefaults()
        {
            Projectile.width = 164;
            Projectile.height = 164;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9000;
            Projectile.scale = 0.2f;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(AngerOnCreation);

        public override void ReceiveExtraAI(BinaryReader reader) => AngerOnCreation = reader.ReadSingle();

        public override void AI()
        {
            Radius = Projectile.scale * MathHelper.Lerp(72f, 90f, AngerOnCreation);

            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && Projectile.timeLeft > 30)
                Projectile.timeLeft = 30;

            if (Projectile.timeLeft < 30f)
            {
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.015f, 0.1f);
                Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.GetLerpValue(18f, 8f, Projectile.timeLeft, true) * 12f;
            }

            if (Projectile.velocity != Vector2.Zero && Projectile.velocity.Length() < MathHelper.Lerp(18f, 24.5f, AngerOnCreation))
            {
                if (Projectile.timeLeft > 160)
                    Projectile.timeLeft = 160;
                Projectile.velocity *= 1.024f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.scale >= 7f && Time % 45f == 44f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 fireVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(14f, 24f) * MathHelper.Lerp(1f, 1.56f, AngerOnCreation);
                    Utilities.NewProjectileBetter(Projectile.Center, fireVelocity, ModContent.ProjectileType<AstralShot2>(), 165, 0f);
                }
            }

            Time++;
        }

        public float SunWidthFunction(float completionRatio) => Radius * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color SunColorFunction(float completionRatio) => Color.Lerp(new Color(237, 93, 83), Color.Red, (float)Math.Sin(MathHelper.Pi * completionRatio) * 0.2f + 0.3f) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.45f);
            GameShaders.Misc["Infernum:Fire"].UseImage1("Images/Misc/Perlin");

            List<float> rotationPoints = new();
            List<Vector2> drawPoints = new();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 50f)
            {
                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + MathHelper.Pi * -0.27f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * Radius / 2f, Projectile.Center + offsetDirection * Radius / 2f, i / 16f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 16);
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.CreateGenericDustExplosion(Projectile.Center, 235, 105, 20f, 4.25f);
            SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 80; i++)
            {
                Vector2 fireVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(18f, 27f) * MathHelper.Lerp(1f, 1.56f, AngerOnCreation);
                Utilities.NewProjectileBetter(Projectile.Center, fireVelocity, ModContent.ProjectileType<AstralShot2>(), 165, 0f);

                for (int j = 0; j < 4; j++)
                {
                    Vector2 sparkleVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(12f, 39f);
                    Utilities.NewProjectileBetter(Projectile.Center + sparkleVelocity * 3f, sparkleVelocity, ModContent.ProjectileType<AstralSparkleBig>(), 0, 0f);
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(Projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
