using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeShield : ModProjectile
    {
        public NPC Owner => Main.npc[(int)OwnerIndex];

        public ref float OwnerIndex => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public const float MaxRadius = 100f;

        public const int HealTime = 180;

        public const int Lifetime = HealTime + HealTime / 3;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Shield");
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
            
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Owner.Center;

            Radius = Sin(Projectile.timeLeft / (float)Lifetime * Pi) * MaxRadius * 4f;
            if (Radius > MaxRadius)
                Radius = MaxRadius;
            Projectile.scale = 2f;

            if (PrimeHeadBehaviorOverride.AnyArms && Projectile.timeLeft < HealTime)
                Projectile.timeLeft = HealTime;

            Projectile.ExpandHitboxBy((int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
                return false;
            Cultist.CultistBehaviorOverride.DrawForcefield(Projectile.Center - Main.screenPosition, Projectile.Opacity, Color.Lerp(Color.Orange, Color.Red, 0.84f), InfernumTextureRegistry.HexagonGrid.Value, false, 1f * (Radius / MaxRadius), fresnelScaleFactor: 1.3f, noiseScaleFactor: 0.75f);
            return false;
        }
    }
}
