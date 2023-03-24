using CalamityMod.NPCs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVortex : ModProjectile
    {
        public static NPC CeaselessVoid => Main.npc[CalamityGlobalNPC.voidBoss];

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ceaseless Vorttex");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Disappear if the Ceaseless Void is not present.
            if (CalamityGlobalNPC.voidBoss == -1)
            {
                Projectile.Kill();
                return;
            }

            Time++;
        }

        public override bool? CanDamage() => false;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = InfernumTextureRegistry.WhiteHole.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            DrawData metaball = new(texture, drawPosition, null, Color.HotPink, 0f, texture.Size() * 0.5f, Projectile.scale * 0.125f, 0, 0);
            ScreenInversionMetaballSystem.AddMetaball(metaball);
            return false;
        }
    }
}
