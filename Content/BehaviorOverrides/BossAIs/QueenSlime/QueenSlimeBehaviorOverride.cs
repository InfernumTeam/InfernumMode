using CalamityMod;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.QueenSlimeBoss;

        public const float Phase2LifeRatio = 0.625f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio
        };

        #region Fields, Properties, and Enumerations
        public enum QueenSlimeAttackType
        {
            SpawnAnimation,
        }
        #endregion Fields, Properties, and Enumerations

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Despawn if the target is gone.
            if (target.dead || !target.active)
            {
                npc.active = false;
                return false;
            }

            npc.damage = npc.defDamage;
            switch ((QueenSlimeAttackType)attackType)
            {
                case QueenSlimeAttackType.SpawnAnimation:
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player player, ref float attackTimer)
        {

        }

        public static void SelectNextAttack(NPC npc)
        {
            QueenSlimeAttackType previousAttack = (QueenSlimeAttackType)npc.ai[0];
            QueenSlimeAttackType nextAttack = QueenSlimeAttackType.SpawnAnimation;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Drawing and Frames

        public static Vector2 CrownPosition(NPC npc)
        {
            Vector2 crownPosition = new(npc.Center.X, npc.Top.Y - 12f);
            float crownOffset = 0f;
            int frameHeight;
            if (npc.frame.Height == 0)
                frameHeight = 122;
            else
                frameHeight = npc.frame.Height;
            switch (npc.frame.Y / frameHeight)
            {
                case 1:
                    crownOffset -= 10f;
                    break;
                case 3:
                case 5:
                case 6:
                    crownOffset += 10f;
                    break;
                case 4:
                case 12:
                case 13:
                case 14:
                case 15:
                    crownOffset += 18f;
                    break;
                case 7:
                case 8:
                    crownOffset -= 14f;
                    break;
                case 9:
                    crownOffset -= 16f;
                    break;
                case 10:
                    crownOffset -= 4f;
                    break;
                case 11:
                    crownOffset += 20f;
                    break;
                case 20:
                    crownOffset -= 14f;
                    break;
                case 21:
                case 23:
                    crownOffset -= 18f;
                    break;
                case 22:
                    crownOffset -= 22f;
                    break;
            }

            crownPosition.Y += crownOffset;
            if (npc.rotation != 0f)
                crownPosition = crownPosition.RotatedBy(npc.rotation, npc.Bottom);
            return crownPosition;
        }

        #endregion Drawing and Frames

        #region Misc Utilities

        public static bool InPhase2(NPC npc) => npc.life < npc.lifeMax * Phase2LifeRatio;

        public static bool OnSolidGround(NPC npc)
        {
            bool solidGround = false;
            for (int i = -8; i < 8; i++)
            {
                Tile ground = CalamityUtils.ParanoidTileRetrieval((int)(npc.Bottom.X / 16f) + i, (int)(npc.Bottom.Y / 16f) + 1);
                bool notAFuckingTree = ground.TileType is not TileID.Trees and not TileID.PineTree and not TileID.PalmTree;
                if (ground.HasUnactuatedTile && notAFuckingTree && (Main.tileSolid[ground.TileType] || Main.tileSolidTop[ground.TileType]))
                {
                    solidGround = true;
                    break;
                }
            }
            return solidGround;
        }
        #endregion Misc Utilities

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Keep your feet working! This gelatinous queen will stop at nothing to crush her foes!";
            yield return n => "Short hops may help better than trying to fly away from all the crystal shrapnel!";
        }
        #endregion Tips
    }
}
