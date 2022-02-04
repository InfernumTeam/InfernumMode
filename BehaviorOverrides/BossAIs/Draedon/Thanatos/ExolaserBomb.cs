using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ExolaserBomb : ModProjectile
    {
        public int GrowTime;
        public PrimitiveTrailCopy FireDrawer;
        public ref float Time => ref projectile.ai[0];
        public ref float Radius => ref projectile.ai[1];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Exolaser Bomb");

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

        public override void AI()
        {
            Radius = projectile.scale * 100f;

            if (!NPC.AnyNPCs(ModContent.NPCType<ThanatosHead>()) && projectile.timeLeft > 30)
                projectile.timeLeft = 30;

            if (projectile.timeLeft < 30f)
            {
                projectile.scale = MathHelper.Lerp(projectile.scale, 0.015f, 0.1f);
                Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.InverseLerp(18f, 8f, projectile.timeLeft, true) * 12f;
            }
            else
                projectile.scale = MathHelper.Lerp(0.04f, 7.5f, MathHelper.Clamp(Time / GrowTime, 0f, 1f));

            if (projectile.velocity != Vector2.Zero && projectile.velocity.Length() < 30f)
            {
                if (projectile.timeLeft > 160)
                    projectile.timeLeft = 160;
                projectile.velocity *= 1.024f;
            }

            Time++;
        }

        public float SunWidthFunction(float completionRatio) => Radius * (float)Math.Sin(MathHelper.Pi * completionRatio);

        public Color SunColorFunction(float completionRatio) => Color.Lerp(Color.Red, Color.Red, (float)Math.Sin(MathHelper.Pi * completionRatio) * 0.4f + 0.3f) * projectile.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(SunWidthFunction, SunColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.45f);
            GameShaders.Misc["Infernum:Fire"].SetShaderTexture(ModContent.GetTexture("InfernumMode/ExtraTextures/CultistRayMap"));

            List<float> rotationPoints = new List<float>();
            List<Vector2> drawPoints = new List<Vector2>();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 24f)
            {
                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + MathHelper.Pi * -0.37f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(projectile.Center - offsetDirection * Radius / 2f, projectile.Center + offsetDirection * Radius / 2f, i / 16f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 30);
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.CreateGenericDustExplosion(projectile.Center, 235, 105, 30f, 2.25f);
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/FlareSound"), projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;


        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
