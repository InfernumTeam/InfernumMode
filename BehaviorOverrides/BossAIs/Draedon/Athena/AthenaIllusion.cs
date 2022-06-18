using CalamityMod;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena.AthenaNPC;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    public class AthenaIllusion : ModNPC
    {
        public PrimitiveTrail FlameTrail = null;

        public Player Target => Main.player[npc.target];

        public static NPC Owner => Main.npc[GlobalNPCOverrides.Athena];

        public static float FlameTrailPulse => Owner.ModNPC<AthenaNPC>().FlameTrailPulse;

        public static float OwnerAttackTime => Owner.ai[1];

        public ref float ConvergeOffsetAngle => ref npc.ai[1];

        public override string Texture => "InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaNPC";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XM-04 Athena");
            Main.npcFrameCount[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
            AthenaSetDefaults(npc);
            npc.damage = 0;
            npc.chaseable = false;
        }

        public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(GlobalNPCOverrides.Athena) || !Owner.active || (AthenaAttackType)Owner.ai[0] != AthenaAttackType.DashingIllusions)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            CopyOwnerAttributes();

            // Maintain an original offset angle but inherit the distance
            // from the target that the main boss has, while also fading away rapidly.
            npc.Center = Target.Center + (ConvergeOffsetAngle + Owner.AngleFrom(Target.Center)).ToRotationVector2() * Owner.Distance(Target.Center);
            npc.Opacity = (float)Math.Pow(Owner.Opacity, 2D);
        }

        public void CopyOwnerAttributes()
        {
            npc.target = Owner.target;
            npc.frame = Owner.frame;
            npc.life = Owner.life;
            npc.lifeMax = Owner.lifeMax;
            npc.dontTakeDamage = Owner.dontTakeDamage;
        }

        public override Color? GetAlpha(Color drawColor) => GlobalNPCOverrides.Athena == -1 ? (Color?)null : Owner.GetAlpha(drawColor);

        // Update these in the illusion NPC's file if this needs changing for some reason.
        // Static methods doesn't easily work in this context, unfortunately.
        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(15f, 80f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 8f, completionRatio) * npc.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true);
            trailOpacity *= MathHelper.Lerp(1f, 0.27f, 1f - FlameTrailPulse) * npc.Opacity;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Blue, 0.74f);
            Color endColor = Color.DarkCyan;
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A /= 8;
            return color * npc.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (FlameTrail is null)
                FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            drawColor = Color.Lerp(drawColor, new Color(0f, 1f, 1f, 0.7f), 0.65f);
            DrawBaseNPC(npc, Main.screenPosition, drawColor, FlameTrailPulse, FlameTrail);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
