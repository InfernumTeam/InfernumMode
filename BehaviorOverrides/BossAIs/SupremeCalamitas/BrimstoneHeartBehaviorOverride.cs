using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneHeartBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SCalWormHeart>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults | NPCOverrideContext.NPCPreDraw;

		public override void SetDefaults(NPC npc)
        {
            npc.damage = 0;
            npc.width = npc.height = 24;
            npc.defense = 0;
            npc.lifeMax = 24000;
            npc.aiStyle = npc.modNPC.aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.HitSound = SoundID.NPCHit13;
            npc.DeathSound = SoundID.NPCDeath1;
        }

		public override bool PreAI(NPC npc)
        {
            // Die if Sepulcher is not present.
            if (CalamityGlobalNPC.SCalWorm == -1)
            {
                npc.active = false;
                return false;
            }

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

            NPC sepulcher = Main.npc[CalamityGlobalNPC.SCalWorm];
            ref float rotationalOffset = ref npc.ai[0];
            rotationalOffset += MathHelper.TwoPi / 300f;

            npc.Center = sepulcher.Center + rotationalOffset.ToRotationVector2() * 120f;
            return false;
        }

        public float PrimitiveWidthFunction(NPC npc, float completionRatio)
        {
            float widthInterpolant = Utils.InverseLerp(0f, 0.16f, completionRatio, true) * Utils.InverseLerp(1f, 0.84f, completionRatio, true);
            widthInterpolant = (float)Math.Pow(widthInterpolant, 8D);
            float baseWidth = MathHelper.Lerp(4f, 1f, widthInterpolant);
            float pulseWidth = MathHelper.Lerp(0f, 3.2f, (float)Math.Pow(Math.Sin(Main.GlobalTime * 2.6f + npc.whoAmI * 1.3f + completionRatio), 16D));
            return baseWidth + pulseWidth;
        }

        public Color PrimitiveColorFunction(float completionRatio)
        {
            float colorInterpolant = MathHelper.SmoothStep(0f, 1f, Utils.InverseLerp(0f, 0.34f, completionRatio, true) * Utils.InverseLerp(1.07f, 0.66f, completionRatio, true));
            return Color.Lerp(Color.DarkRed * 0.7f, Color.Red, colorInterpolant) * 0.425f;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            if (CalamityGlobalNPC.SCalWorm == -1)
                return true;

            if (npc.Infernum().OptionalPrimitiveDrawer is null)
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(c => PrimitiveWidthFunction(npc, c), PrimitiveColorFunction);

            NPC sepulcher = Main.npc[CalamityGlobalNPC.SCalWorm];
            List<Vector2> points = new List<Vector2>()
            {
                npc.Center,
                Vector2.Lerp(npc.Center, sepulcher.Center, 0.5f),
                sepulcher.Center
            };
            npc.Infernum().OptionalPrimitiveDrawer.Draw(points, -Main.screenPosition, 40);
            return true;
        }
    }
}
