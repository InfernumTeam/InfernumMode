using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
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
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    internal class HolySpinningFireBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy BeamDrawer;

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 360;

        public const float LaserLength = 7800f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Fire Beam");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            Projectile.scale = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
                return;
            NPC owner = Main.npc[CalamityGlobalNPC.doughnutBoss];

            if (owner.type != ModContent.NPCType<ProfanedGuardianCommander>() || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.width = 60;

            // Do not naturally die.
            Projectile.timeLeft = Lifetime;

            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.1f, 0f, 1f);

            // Rotate.
            Projectile.rotation += 0.01f;
            Projectile.velocity = (MathHelper.TwoPi * Projectile.ai[1] + Projectile.rotation).ToRotationVector2();
            Projectile.Center = owner.Center + Projectile.velocity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * Projectile.scale * 2f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            return Projectile.width * Projectile.scale * 2f;
        }

        public Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.OrangeRed, Color.Gold, 0.5f) * Projectile.Opacity;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GuardiansLaserVertexShader);

            InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture2(InfernumTextureRegistry.StreakThinGlow);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.UseColor(WayfinderSymbol.Colors[0]);
            Vector2 startPos = Projectile.Center;
            Vector2 endPos = Projectile.Center + Projectile.velocity * LaserLength;

            Vector2[] drawPoints = new Vector2[8];
            for (int i = 0; i < drawPoints.Length; i++)
                drawPoints[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPoints.Length);

            BeamDrawer.DrawPixelated(drawPoints, -Main.screenPosition, 40);
        }
    }
}
