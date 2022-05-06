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

        public Player Target => Main.player[NPC.target];

        public static NPC Owner => Main.npc[GlobalNPCOverrides.Athena];

        public static float FlameTrailPulse => Owner.ModNPC<AthenaNPC>().FlameTrailPulse;

        public static float OwnerAttackTime => Owner.ai[1];

        public ref float ConvergeOffsetAngle => ref NPC.ai[1];

        public override string Texture => "InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaNPC";

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("XM-04 Athena");
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            AthenaSetDefaults(NPC);
            NPC.damage = 0;
            NPC.chaseable = false;
        }

        public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(GlobalNPCOverrides.Athena) || !Owner.active || (AthenaAttackType)Owner.ai[0] != AthenaAttackType.DashingIllusions)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            CopyOwnerAttributes();

            // Maintain an original offset angle but inherit the distance
            // from the target that the main boss has, while also fading away rapidly.
            NPC.Center = Target.Center + (ConvergeOffsetAngle + Owner.AngleFrom(Target.Center)).ToRotationVector2() * Owner.Distance(Target.Center);
            NPC.Opacity = (float)Math.Pow(Owner.Opacity, 2D);
        }

        public void CopyOwnerAttributes()
        {
            NPC.target = Owner.target;
            NPC.frame = Owner.frame;
            NPC.life = Owner.life;
            NPC.lifeMax = Owner.lifeMax;
            NPC.dontTakeDamage = Owner.dontTakeDamage;
        }

        public override Color? GetAlpha(Color drawColor) => GlobalNPCOverrides.Athena == -1 ? null : Owner.GetAlpha(drawColor);

        // Update these in the illusion NPC's file if this needs changing for some reason.
        // Static methods doesn't easily work in this context, unfortunately.
        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(15f, 80f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 8f, completionRatio) * NPC.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
            trailOpacity *= MathHelper.Lerp(1f, 0.27f, 1f - FlameTrailPulse) * NPC.Opacity;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Blue, 0.74f);
            Color endColor = Color.DarkCyan;
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A /= 8;
            return color * NPC.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (FlameTrail is null)
                FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            drawColor = Color.Lerp(drawColor, new(0f, 1f, 1f, 0.7f), 0.65f);
            DrawBaseNPC(NPC, screenPos, drawColor, FlameTrailPulse, FlameTrail);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
