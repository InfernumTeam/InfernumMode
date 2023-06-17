using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class TwinsShield : ModProjectile
    {
        public NPC Owner => Main.npc[(int)OwnerIndex];

        public ref float OwnerIndex => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public const float MaxRadius = 180f;

        public const int HealTime = 180;

        public const int Lifetime = HealTime + HealTime / 3;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shield");
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
            CooldownSlot = ImmunityCooldownID.Bosses;
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

            bool alone = !NPC.AnyNPCs(NPCID.Spazmatism) && Owner.type == NPCID.Retinazer || !NPC.AnyNPCs(NPCID.Retinazer) && Owner.type == NPCID.Spazmatism;
            if (!alone && Projectile.timeLeft < HealTime)
                Projectile.timeLeft = HealTime;

            Projectile.ExpandHitboxBy((int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
                return false;
            Cultist.CultistBehaviorOverride.DrawForcefield(Projectile.Center - Main.screenPosition, Projectile.Opacity, Owner.type == NPCID.Spazmatism ? Color.LimeGreen : Color.Red, InfernumTextureRegistry.HexagonGrid.Value, false, 1.3f * (Radius / MaxRadius), fresnelScaleFactor: 1.3f, noiseScaleFactor: 1.05f);
            return false;
        }
    }
}
