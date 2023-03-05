using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolySineSpear : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy TrailDrawer;

        public float StartingRotation
        {
            get;
            set;
        } = 0f;

        public Vector2 InitialCenter
        {
            get;
            set;
        } = Vector2.Zero;

        public Vector2 InitialVelocity
        {
            get;
            set;
        } = Vector2.Zero;

        public static NPC Commander
        {
            get
            {
                if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss))
                    return Main.npc[CalamityGlobalNPC.doughnutBoss];
                return null;
            }
        }

        public float WaveOffset;

        public float TelegraphLength => 20f;

        public float FlyStraightLength => 15f;

        public float SineOffset => MathF.Sin((Timer - WaveOffset) / 9f - WaveOffset);

        public ref float Timer => ref Projectile.ai[0];

        public ref float SpearDirection => ref Projectile.ai[1];

        public float SpearReleaseRate = 3f;

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/ProfanedSpearInfernum";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Spear");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 25;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 200;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Commander is null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            //Wave up and down over time.
            if (Timer > FlyStraightLength)
            {
                Vector2 moveOffset = (StartingRotation + MathHelper.PiOver2).ToRotationVector2() * SineOffset * 8f;
                Projectile.Center += moveOffset;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4 + (SineOffset * 0.5f * Projectile.direction);
                Projectile.velocity *= 1.01f;
            }
            //Release spears.
            if (Timer % SpearReleaseRate == SpearReleaseRate - 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 * SpearDirection) * 7f;
                SpearDirection *= -1f;
                Utilities.NewProjectileBetter(Projectile.Center + velocity.SafeNormalize(Vector2.UnitY), velocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), GuardianComboAttackManager.HolySpearDamage, 0f);
            }

            Lighting.AddLight(Projectile.Center, Vector3.One);
            Timer++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, Color.White, 2f);
            return false;
        }

        internal float TrailWidthFunction(float completionRatio) => Projectile.scale * 20f;

        internal Color TrailColorFunction(float completionRatio) => Color.Lerp(WayfinderSymbol.Colors[1], Color.Transparent, completionRatio + 0.2f);

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {

            TrailDrawer ??= new PrimitiveTrailCopy(TrailWidthFunction, TrailColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.HoneycombNoise);

            TrailDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 25);
        }
    }
}
