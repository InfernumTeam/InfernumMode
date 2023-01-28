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

        public const float LaserLength = 8300f;

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
            Projectile.rotation += 0.013f;
            Projectile.velocity = (MathHelper.TwoPi * Projectile.ai[1] + Projectile.rotation).ToRotationVector2();
            Projectile.Center = owner.Center + Projectile.velocity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * Projectile.scale * 1.75f;
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
            float interpolant = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
            float colorInterpolant = MathHelper.Lerp(0.3f, 0.5f, interpolant);
            Color orange = new(1f, 0.45f, 0f, 1f);
            Color yellow = new(1f, 1, 0f, 1f);
            return Color.Lerp(Color.OrangeRed, Color.Gold, colorInterpolant) * Projectile.Opacity;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GuardiansLaserVertexShader);

            InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThinGlow);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture2(InfernumTextureRegistry.CultistRayMap);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.UseColor(new Color(255, 221, 135));
            InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["flipY"].SetValue(Projectile.ai[1] == 0.5f);
            Vector2 startPos = Projectile.Center - Projectile.velocity * 2f;
            Vector2 endPos = Projectile.Center + Projectile.velocity * LaserLength;

            Vector2[] drawPoints = new Vector2[8];
            for (int i = 0; i < drawPoints.Length; i++)
                drawPoints[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPoints.Length);

            BeamDrawer.DrawPixelated(drawPoints, -Main.screenPosition, 40);
        }
    }
}
