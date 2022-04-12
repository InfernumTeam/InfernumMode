using CalamityMod;
using InfernumMode.BehaviorOverrides.BossAIs.Ravager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    [AutoloadBossHead]
    public class DeerclopsNPC : ModNPC
    {
        #region Fields, Properties, and Enumerations
        public enum DeerclopsAttackType
        {
            WalkToTarget
        }

        public enum DeerclopsFrameType
        {
            StandInPlace,
            Fall,
            Walk
        }

        public DeerclopsAttackType AttackType
        {
            get => (DeerclopsAttackType)npc.ai[0];
            set => npc.ai[0] = (int)value;
        }

        public DeerclopsFrameType FrameType
        {
            get => (DeerclopsFrameType)npc.localAI[0];
            set => npc.localAI[0] = (int)value;
        }

        public Player Target => Main.player[npc.target];

        public ref float AttackTimer => ref npc.ai[1];

        #endregion Fields, Properties, and Enumerations

        #region Set Defaults

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Deerclops");
            Main.npcFrameCount[npc.type] = 5;
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 5;
        }

        public override void SetDefaults()
        {
            npc.width = 60;
            npc.height = 154;
            npc.aiStyle = 123;
            npc.damage = 20;
            npc.defense = 10;
            npc.lifeMax = 7900;
            npc.HitSound = SoundID.NPCHit1;
            npc.knockBackResist = 0f;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.value = Item.buyPrice(0, 5, 0, 0);
            npc.npcSlots = 5f;
            npc.coldDamage = true;
            music = MusicID.Boss3;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.8f * bossLifeScale);
            npc.damage = (int)(npc.damage * 0.8f);
        }

        #endregion Set Defaults

        #region AI and Behaviors

        public override void AI()
        {
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);
            npc.TargetClosestIfTargetIsInvalid();

            switch (AttackType)
            {
                case DeerclopsAttackType.WalkToTarget:
                    DoBehavior_WalkToTarget();
                    break;
            }

            AttackTimer++;
        }

        public void DoBehavior_WalkToTarget()
        {
            FrameType = DeerclopsFrameType.Walk;
            if (Math.Abs(npc.velocity.X) > 0.2f)
                npc.spriteDirection = Math.Sign(npc.velocity.X);
            else
                FrameType = DeerclopsFrameType.StandInPlace;

            float walkSpeed = MathHelper.Lerp(5f, 8.5f, 1f - npc.life / (float)npc.lifeMax);
            float fallSpeed = 0.4f;
            float horizontalOffsetFromTarget = Target.Center.X - npc.Center.X;
            float horizontalDistanceFromTarget = Math.Abs(horizontalOffsetFromTarget);
            bool shouldSlowDown = horizontalDistanceFromTarget < 80f;
            Rectangle hitbox = Target.Hitbox;

            if (shouldSlowDown)
            {
                npc.velocity.X *= 0.9f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
            {
                int horizontalDirection = Math.Sign(horizontalOffsetFromTarget);
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, horizontalDirection * walkSpeed, 0.25f);
            }
            int checkAreaX = 40;
            int checkAreaY = 20;
            Vector2 checkTopLeft = new Vector2(npc.Center.X - checkAreaX / 2, npc.position.Y + npc.height - checkAreaY);
            bool acceptTopSurfaces = npc.Bottom.Y >= hitbox.Top;
            bool shouldRise = Utilities.SolidCollision(checkTopLeft, checkAreaX, checkAreaY, acceptTopSurfaces);
            bool fuck = Utilities.SolidCollision(checkTopLeft, checkAreaX, checkAreaY - 4, acceptTopSurfaces);
            bool shouldJump = !Utilities.SolidCollision(checkTopLeft + new Vector2(checkAreaX * npc.direction, 0f), 16, 80, acceptTopSurfaces);
            float jumpSpeed = 8f;

            if (((checkTopLeft.X < hitbox.X && checkTopLeft.X + npc.width > hitbox.X + hitbox.Width) || shouldSlowDown) && 
                checkTopLeft.Y + checkAreaY < hitbox.Y + hitbox.Height - 16)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + fallSpeed * 2f, 0.001f, 16f);
                return;
            }
            if (shouldRise && !fuck)
            {
                npc.velocity.Y = 0f;
                return;
            }
            if (shouldRise)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -8f, 0f);
                return;
            }
            if (npc.velocity.Y == 0f && shouldJump)
            {
                npc.velocity.Y = -jumpSpeed;
                FrameType = DeerclopsFrameType.Fall;
                return;
            }
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + fallSpeed, -jumpSpeed, 16f);
        }

        public void SelectNextAttack()
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            AttackTimer = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Drawing and Frames

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
		{
            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * (npc.gfxOffY - 44f);
            Vector2 origin = npc.frame.Size() * 0.5f;
            Color color = npc.GetAlpha(drawColor);
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(texture, drawPosition, npc.frame, color, npc.rotation, origin, npc.scale, direction, 0f);

            return false;
		}

        public override void FindFrame(int frameHeight)
        {
            npc.frame.Height = frameHeight = 240;
            npc.frame.Width = 218;

            int frame = npc.frame.Y / npc.frame.Height % Main.npcFrameCount[npc.type];
            frame += npc.frame.X / npc.frame.Width * Main.npcFrameCount[npc.type];
            switch (FrameType)
            {
                case DeerclopsFrameType.StandInPlace:
                    frame = 0;
                    break;
                case DeerclopsFrameType.Fall:
                    frame = 1;
                    break;
                case DeerclopsFrameType.Walk:
                    if (frame < 2 || frame > 10)
                    {
                        frame = 2;
                        npc.frameCounter = 0;
                    }

                    npc.frameCounter += Math.Abs(npc.velocity.X) * 0.2f + 1f;
                    if (npc.frameCounter >= 10D)
                    {
                        frame++;
                        if (frame > 10)
                            frame = 2;

                        npc.frameCounter = 0;
                    }
                    break;
            }

            npc.frame.X = npc.frame.Width * (frame / Main.npcFrameCount[npc.type]);
            npc.frame.Y = frameHeight * (frame % Main.npcFrameCount[npc.type]);
        }

        #endregion Drawing and Frames

        #region Hit Effects and Loot

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override bool CheckActive() => false;

        #endregion Hit Effects and Loot
    }
}
