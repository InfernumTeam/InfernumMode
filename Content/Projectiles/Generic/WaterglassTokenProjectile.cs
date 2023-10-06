using InfernumMode.Content.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class WaterglassTokenProjectile : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 150;

        public override string Texture => "InfernumMode/Content/Items/Misc/WaterglassToken";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Waterglass Token");

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Die if the owner is no longer present.
            if (!Owner.active)
            {
                Projectile.active = false;
                return;
            }

            // Fly upward before slowing down.
            Projectile.velocity = -Vector2.UnitY * Utils.GetLerpValue(90f, 36f, Time, true) * 6f;

            // Jitter after all movement ceases.
            if (Time >= 105f)
                Projectile.Center += Main.rand.NextVector2Circular(2f, 2f);

            // Fade out before teleporting the player.
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);

            if (Main.myPlayer == Projectile.owner)
                MoonlordDeathDrama.RequestLight(Utils.GetLerpValue(60f, 20f, Projectile.timeLeft, true), Owner.Center);
            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                if (SubworldSystem.IsActive<LostColosseum>())
                    SubworldSystem.Exit();
                else
                {
                    Main.LocalPlayer.Infernum_Biome().PositionBeforeEnteringSubworld = Main.LocalPlayer.Center;
                    SubworldSystem.Enter<LostColosseum>();
                }
            }
        }
    }
}
