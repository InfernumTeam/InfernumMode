using System.Collections.Generic;
using CalamityMod;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class ArenaIcicle : ModProjectile
    {
        public IcicleDrawer Drawer;

        public float Direction => Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1000000;
            Projectile.hide = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.Deerclops))
                Projectile.Kill();
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Drawer?.IsCollidingWith(Projectile.Center, targetHitbox) ?? false;

        public override void OnKill(int timeLeft) => Drawer?.DoShatterEffect(Projectile.Center);

        public override bool PreDraw(ref Color lightColor)
        {
            Drawer ??= new()
            {
                Seed = Projectile.identity * 313,
                MaxDistanceBeforeCutoff = InfernumConfig.Instance.ReducedGraphicsConfig ? 1125f : 1780f,
                DistanceUsedForBase = 500f,
                BranchMaxBendFactor = 0.04f,
                BranchTurnAngleVariance = 0.137f,
                MinBranchLength = 85f,
                BaseWidth = 16f,
                ChanceToCreateNewBranches = 0.15f,
                VerticalStretchFactor = 12f,
                BranchGrowthWidthDecay = 0.6f,
                MaxCutoffBranchesPerBranch = 2,
                BaseDirection = Pi * Direction / 9f,
            };
            Drawer.Draw(Projectile.Center.ToPoint(), true);
            return false;
        }
    }
}
