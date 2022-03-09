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

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    [AutoloadBossHead]
    public class EmpressOfLightNPC : ModNPC
    {
        #region Fields, Properties, and Enumerations
        public enum EmpressOfLightAttackType
        {
            RepeatedSlams,
            CrystalShatter,
            CrownDashes,
            CrystalShardBursts,
            CrownLasers
        }

        public EmpressOfLightAttackType AttackType
        {
            get => (EmpressOfLightAttackType)npc.ai[0];
            set => npc.ai[0] = (int)value;
        }

        public bool InPhase2 => true;

        public bool Enraged => Main.dayTime;

        public Player Target => Main.player[npc.target];

        public ref float AttackTimer => ref npc.ai[1];

        public ref float WingFrameCounter => ref npc.localAI[0];

        #endregion Fields, Properties, and Enumerations

        #region Set Defaults

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Empress Of Light");
            Main.npcFrameCount[npc.type] = 2;
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 12;
        }

        public override void SetDefaults()
        {
            npc.noGravity = true;
            npc.width = 100;
            npc.height = 100;
            npc.damage = 80;
            npc.defense = 50;
            npc.lifeMax = 70000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0f;
            npc.value = 250000f;
            npc.noTileCollide = true;
            npc.boss = true;
            npc.Opacity = 0f;
            npc.dontTakeDamage = true;
            npc.boss = true;
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

            npc.damage = 0;
            npc.spriteDirection = 1;
            switch (AttackType)
            {
            }

            WingFrameCounter++;
            AttackTimer++;
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

        public override Color? GetAlpha(Color drawColor)
        {
            return Color.Lerp(drawColor, Color.White, 0.4f) * npc.Opacity;
        }

        public void PrepareShader()
        {
            Main.graphics.GraphicsDevice.Textures[1] = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsTexture");
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            int num = (int)npc.ai[0];
            Color baseColor = npc.GetAlpha(drawColor);
            Texture2D wingOutlineTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsOutline");
            Texture2D leftArmTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightLeftArm");
            Texture2D rightArmTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightRightArm");
            Texture2D wingTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWings");
            Texture2D tentacleTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightTentacles");
            Texture2D dressGlowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightGlowmask");

            int armFrameToUseLeft = 0;
            int armFrameToUseRight = 0;
            Rectangle tentacleFrame = tentacleTexture.Frame(1, 8, 0, (int)(WingFrameCounter / 5f) % 8);
            Rectangle wingFrame = wingOutlineTexture.Frame(1, 11, 0, (int)(WingFrameCounter / 5f) % 11);
            Rectangle leftArmFrame = leftArmTexture.Frame(1, 7, 0, armFrameToUseLeft);
            Rectangle rightArmFrame = rightArmTexture.Frame(1, 7, 0, armFrameToUseRight);
            Vector2 origin = leftArmFrame.Size() / 2f;
            Vector2 origin2 = rightArmFrame.Size() / 2f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            int leftArmDrawOrder = 0;
            int rightArmDrawOrder = 0;
            if (armFrameToUseLeft == 5)
                leftArmDrawOrder = 1;

            if (armFrameToUseRight == 5)
                rightArmDrawOrder = 1;

            float baseColorOpacity = 1f;
            int laggingAfterimageCount = 0;
            int baseDuplicateCount = 0;
            float afterimageOffsetFactor = 0f;
            float opacity = 0f;
            float num8 = 0f;

            // Charge thing.
            if (num == 8 || num == 9)
            {
                afterimageOffsetFactor = Utils.InverseLerp(0f, 30f, AttackTimer, true) * Utils.InverseLerp(90f, 30f, AttackTimer, true);
                opacity = Utils.InverseLerp(0f, 30f, AttackTimer, true) * Utils.InverseLerp(90f, 70f, AttackTimer, true);
                num8 = Utils.InverseLerp(0f, 15f, AttackTimer, true) * Utils.InverseLerp(45f, 30f, AttackTimer, true);
                baseColor = Color.Lerp(baseColor, Color.White, afterimageOffsetFactor);
                baseColorOpacity *= 1f - num8;
                laggingAfterimageCount = 4;
                baseDuplicateCount = 3;
            }

            if (num == 10)
            {
                afterimageOffsetFactor = Utils.InverseLerp(30f, 90f, AttackTimer, true) * Utils.InverseLerp(165f, 90f, AttackTimer, true);
                opacity = Utils.InverseLerp(0f, 60f, AttackTimer, true) * Utils.InverseLerp(180f, 120f, AttackTimer, true);
                num8 = Utils.InverseLerp(0f, 60f, AttackTimer, true) * Utils.InverseLerp(180f, 120f, AttackTimer, true);
                baseColor = Color.Lerp(baseColor, Color.White, afterimageOffsetFactor);
                baseColorOpacity *= 1f - num8;
                baseDuplicateCount = 4;
            }

            if (baseDuplicateCount + laggingAfterimageCount > 0)
            {
                for (int i = -baseDuplicateCount; i <= baseDuplicateCount + laggingAfterimageCount; i++)
                {
                    if (i == 0)
                        continue;

                    Color duplicateColor = Color.White;
                    Vector2 drawPosition = baseDrawPosition;

                    // Create cool afterimages while charging at the target.
                    if (num == 8 || num == 9)
                    {
                        float hue = (i + 5f) / 10f;
                        float drawOffsetFactor = 200f;
                        Vector3 offsetInformation = Vector3.Transform(Vector3.Forward, 
                            Matrix.CreateRotationX((Main.GlobalTime - 0.3f + i * 0.1f) * 0.7f * MathHelper.TwoPi) * 
                            Matrix.CreateRotationY((Main.GlobalTime - 0.8f + i * 0.3f) * 0.7f * MathHelper.TwoPi) * 
                            Matrix.CreateRotationZ((Main.GlobalTime + i * 0.5f) * 0.1f * MathHelper.TwoPi));
                        drawOffsetFactor += Utils.InverseLerp(-1f, 1f, offsetInformation.Z, true) * 150f;
                        Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor * afterimageOffsetFactor;
                        drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * AttackTimer / 180f);

                        float lerpValue = Utils.InverseLerp(90f, 0f, AttackTimer, true);
                        duplicateColor = Main.hslToRgb(hue, 1f, MathHelper.Lerp(0.5f, 1f, lerpValue)) * 0.8f * opacity;
                        duplicateColor.A /= 3;
                        drawPosition += drawOffset;
                    }

                    // Do the transition visuals for phase 2.
                    if (num == 10)
                    {
                        // Fade in.
                        if (AttackTimer >= 90f)
                        {
                            int offsetIndex = i;
                            if (offsetIndex < 0)
                                offsetIndex++;

                            Vector2 circularOffset = ((offsetIndex + 0.5f) * MathHelper.PiOver4 + Main.GlobalTime * MathHelper.Pi * 1.333f).ToRotationVector2();
                            drawPosition += circularOffset * afterimageOffsetFactor * new Vector2(600f, 150f);
                        }

                        // Fade out and create afterimages that dissipate.
                        else
                            drawPosition += 200f * new Vector2(i, 0f) * afterimageOffsetFactor;

                        duplicateColor = Color.White * opacity * baseColorOpacity * 0.8f;
                        duplicateColor.A /= 3;
                    }

                    // Create lagging afterimages.
                    if (i > baseDuplicateCount)
                    {
                        float lagBehindFactor = Utils.InverseLerp(30f, 70f, AttackTimer, true);
                        if (lagBehindFactor == 0f)
                            continue;

                        drawPosition = baseDrawPosition + npc.velocity * -3f * (i - baseDuplicateCount - 1f) * lagBehindFactor;
                        duplicateColor *= 1f - num8;
                    }

                    // Draw wings.
                    spriteBatch.Draw(wingOutlineTexture, drawPosition, wingFrame, duplicateColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);
                    spriteBatch.Draw(wingTexture, drawPosition, wingFrame, duplicateColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);

                    // Draw tentacles in phase 2.
                    if (InPhase2)
                        spriteBatch.Draw(tentacleTexture, drawPosition, tentacleFrame, duplicateColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                    // Draw the base texture.
                    spriteBatch.Draw(texture, drawPosition, npc.frame, duplicateColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                    // Draw hands.
                    for (int j = 0; j < 2; j++)
                    {
                        if (j == leftArmDrawOrder)
                            spriteBatch.Draw(leftArmTexture, drawPosition, leftArmFrame, duplicateColor, npc.rotation, origin, npc.scale, direction, 0f);

                        if (j == rightArmDrawOrder)
                            spriteBatch.Draw(rightArmTexture, drawPosition, rightArmFrame, duplicateColor, npc.rotation, origin2, npc.scale, direction, 0f);
                    }
                }
            }

            // Draw wings. This involves usage of a shader to give the wing texture.
            baseColor *= baseColorOpacity;
            spriteBatch.Draw(wingOutlineTexture, baseDrawPosition, wingFrame, baseColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);

            spriteBatch.EnterShaderRegion();

            DrawData wingData = new DrawData(wingTexture, baseDrawPosition, wingFrame, baseColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0);
            PrepareShader();
            GameShaders.Misc["Infernum:EmpressOfLightWings"].Apply(wingData);
            wingData.Draw(spriteBatch);
            spriteBatch.ExitShaderRegion();

            float pulse = (float)Math.Sin(Main.GlobalTime * MathHelper.Pi) * 0.5f + 0.5f;
            Color tentacleDressColor = Main.hslToRgb((pulse * 0.08f + 0.6f) % 1f, 1f, 0.5f);
            tentacleDressColor.A = 0;
            tentacleDressColor *= 0.6f;
            if (Enraged)
            {
                tentacleDressColor = Main.OurFavoriteColor;
                tentacleDressColor.A = 0;
                tentacleDressColor *= 0.3f;
            }

            tentacleDressColor *= baseColorOpacity * npc.Opacity;

            // Draw tentacles.
            if (InPhase2)
            {
                spriteBatch.Draw(tentacleTexture, baseDrawPosition, tentacleFrame, baseColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 drawOffset = npc.rotation.ToRotationVector2().RotatedBy(MathHelper.TwoPi * i / 4f + MathHelper.PiOver4) * MathHelper.Lerp(2f, 8f, pulse);
                    spriteBatch.Draw(tentacleTexture, baseDrawPosition + drawOffset, tentacleFrame, tentacleDressColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                }
            }

            // Draw the base texture.
            spriteBatch.Draw(texture, baseDrawPosition, npc.frame, baseColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

            // Draw the dress.
            if (InPhase2)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 drawOffset = npc.rotation.ToRotationVector2().RotatedBy(MathHelper.TwoPi * i / 4f + MathHelper.PiOver4) * MathHelper.Lerp(2f, 8f, pulse);
                    spriteBatch.Draw(dressGlowmaskTexture, baseDrawPosition + drawOffset, null, tentacleDressColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                }
            }

            // Draw arms.
            for (int k = 0; k < 2; k++)
            {
                if (k == leftArmDrawOrder)
                    spriteBatch.Draw(leftArmTexture, baseDrawPosition, leftArmFrame, baseColor, npc.rotation, origin, npc.scale, direction, 0f);

                if (k == rightArmDrawOrder)
                    spriteBatch.Draw(rightArmTexture, baseDrawPosition, rightArmFrame, baseColor, npc.rotation, origin2, npc.scale, direction, 0f);
            }
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frame.Y = InPhase2.ToInt() * frameHeight;
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
