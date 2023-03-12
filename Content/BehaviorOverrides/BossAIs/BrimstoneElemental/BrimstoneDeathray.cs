using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneDeathray : BaseLaserbeamProjectile
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            set;
        } = null;

        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => 85;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Red;
        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Projectile.type].Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayEnd", AssetRequestMode.ImmediateLoad).Value;
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public Vector2 OwnerEyePosition => Main.npc[OwnerIndex].Center + new Vector2(Main.npc[OwnerIndex].spriteDirection * 20f, -68f);
        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
        public override void AttachToSomething()
        {
            if (!Main.projectile.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = OwnerEyePosition;

            if (Projectile.timeLeft == 10)
            {
                SoundEngine.PlaySound(CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas.BrimstoneShotSound, Projectile.Center);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return;

                int petalDamage = 130;
                for (float petalOffset = 20f; petalOffset < LaserLength; petalOffset += 165f)
                {
                    Vector2 petalSpawnPosition = OwnerEyePosition + Projectile.velocity * petalOffset;
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 petalVelocity = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * i) * 8f;
                        if (BossRushEvent.BossRushActive)
                            petalVelocity *= 1.85f;
                        Utilities.NewProjectileBetter(petalSpawnPosition, petalVelocity, ModContent.ProjectileType<BrimstonePetal2>(), petalDamage, 0f);
                    }
                }
            }
        }

        public float LaserWidthFunction(float completionRatio) => Projectile.scale * Projectile.width * Utils.GetLerpValue(0.02f, 0.05f, completionRatio, true) * 3f;

        public static Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -5.2f + completionRatio * 23f) * 0.5f + 0.5f;
            return Color.Lerp(Color.Red, new(255, 0, 25), colorInterpolant) * Utils.GetLerpValue(0.02f, 0.05f, completionRatio, true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero)
                return false;

            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVertexShader);
            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader, along with the electricity color.
            Color middleColor = new(252, 220, 178);
            Color middleColor2 = new(255, 162, 162);
            InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(middleColor2 * 2f);
            InfernumEffectsRegistry.GenericLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFire);

            LaserDrawer.Draw(baseDrawPoints, -Main.screenPosition, 60);
            return false;
        }

        public override bool? CanDamage() => Time > 10f ? null : false;
    }
}
