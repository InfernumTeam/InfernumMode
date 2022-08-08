using CalamityMod;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class DarkGodLaser : BaseLaserbeamProjectile
    {
        public int OwnerIndex
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public PrimitiveTrail LaserDrawer = null;

        public const int LaserLifetime = 300;
        public override float MaxScale => 1f;
        public override float MaxLaserLength => 3600f;
        public override float Lifetime => LaserLifetime;
        public override Color LaserOverlayColor => new(255, 255, 255, 100);
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Projectile.type].Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override string Texture => "CalamityMod/Projectiles/Boss/AresLaserBeamStart";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Antimatter Deathray");

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 54;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = LaserLifetime;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
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
            // Die if Deus is not present.
            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()))
            {
                Projectile.Kill();
                return;
            }
        }

        public override float DetermineLaserLength() => MaxLaserLength;

        public override void UpdateLaserMotion()
        {
            float spinSpeed = Utils.GetLerpValue(0f, 60f, Time, true) * 0.016f;
            Projectile.velocity = Projectile.velocity.RotatedBy(spinSpeed);
        }

        public override void PostAI()
        {
            // Determine scale.
            Time = Lifetime - Projectile.timeLeft;
            Projectile.scale = CalamityUtils.Convert01To010(Time / Lifetime) * MaxScale * 3f;
            if (Projectile.scale > MaxScale)
                Projectile.scale = MaxScale;
        }

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width;

        public static Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -1.23f + completionRatio * 23f) * 0.5f + 0.5f;
            return Color.Lerp(Color.Black, Color.Cyan, (float)Math.Pow(colorInterpolant, 3.3) * 0.25f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero)
                return false;

            if (LaserDrawer is null)
                LaserDrawer = new PrimitiveTrail(LaserWidthFunction, LaserColorFunction, null, GameShaders.Misc["Infernum:ArtemisLaser"]);

            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader, along with the electricity color.
            GameShaders.Misc["Infernum:ArtemisLaser"].UseColor(Color.Turquoise);
            GameShaders.Misc["Infernum:ArtemisLaser"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScarletDevilStreak"));
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;

            LaserDrawer.Draw(baseDrawPoints, -Main.screenPosition, 64);
            return false;
        }

        public override bool CanHitPlayer(Player target) => Projectile.scale >= 0.5f;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
