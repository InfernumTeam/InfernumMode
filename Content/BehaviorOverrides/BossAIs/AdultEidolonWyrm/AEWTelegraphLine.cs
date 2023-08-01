using CalamityMod;
using CalamityMod.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWTelegraphLine : ModProjectile, IAboveWaterProjectileDrawer
    {
        public bool DarkForm
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public bool CreateSplitAEW
        {
            get => Projectile.localAI[1] == 0f;
            set => Projectile.localAI[1] = 1f - value.ToInt();
        }

        public ref float Time => ref Projectile.localAI[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Time);

        public override void ReceiveExtraAI(BinaryReader reader) => Time = reader.ReadSingle();

        public override void AI()
        {
            Projectile.Opacity = CalamityUtils.Convert01To010(Time / Lifetime) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor) => false;

        public override void Kill(int timeLeft)
        {
            if (!CreateSplitAEW)
                return;

            // Play a roar before charging.
            if (DarkForm)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (Projectile.WithinRange(target.Center, 3200f))
                    SoundEngine.PlaySound(CommonCalamitySounds.WyrmScreamSound, target.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 splitFormVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 18f;
            Utilities.NewProjectileBetter(Projectile.Center, splitFormVelocity, ModContent.ProjectileType<AEWSplitForm>(), AEWHeadBehaviorOverride.PowerfulShotDamage, 0f, -1, DarkForm.ToInt());
        }

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            float telegraphWidth = Lerp(0.3f, 5f, CalamityUtils.Convert01To010(Time / Lifetime));

            // Draw a telegraph line outward.
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
            Main.spriteBatch.DrawLineBetter(start, end, DarkForm ? Color.Violet : Color.Lerp(Color.Yellow, Color.Pink, 0.5f), telegraphWidth);
        }
    }
}