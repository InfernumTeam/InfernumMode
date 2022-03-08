using CalamityMod;
using InfernumMode.BehaviorOverrides.BossAIs.Ravager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenSlime
{
    [AutoloadBossHead]
    public class QueenSlimeNPC : ModNPC
    {
        public enum QueenSlimeAttackType
        {
            RepeatedSlams,
            CrystalShatter
        }

        public enum QueenSlimeFrameType
        {
            HoverRedirect,
            SlamDownward,
            SlamDownwardPreparation,
            FlatState,
            Flying
        }

        public QueenSlimeAttackType AttackType
        {
            get => (QueenSlimeAttackType)npc.ai[0];
            set => npc.ai[0] = (int)value;
        }

        public QueenSlimeFrameType FrameType
        {
            get => (QueenSlimeFrameType)npc.localAI[0];
            set => npc.localAI[0] = (int)value;
        }

        public bool InPhase2 => npc.life < npc.lifeMax * 0.5f;

        public bool OnSolidGround
        {
            get
            {
                bool solidGround = false;
                for (int i = -8; i < 8; i++)
                {
                    Tile ground = CalamityUtils.ParanoidTileRetrieval((int)(npc.Bottom.X / 16f) + i, (int)(npc.Bottom.Y / 16f) + 1);
                    bool notAFuckingTree = ground.type != TileID.Trees && ground.type != TileID.PineTree && ground.type != TileID.PalmTree;
                    if (ground.nactive() && notAFuckingTree && (Main.tileSolid[ground.type] || Main.tileSolidTop[ground.type]))
                    {
                        solidGround = true;
                        break;
                    }
                }
                return solidGround;
            }
        }

        public Player Target => Main.player[npc.target];

        public ref float AttackTimer => ref npc.ai[1];

        public ref float SlamTelegraphInterpolant => ref npc.localAI[1];

        public static Color DustColor
        {
            get
            {
                Color c1 = new Color(0, 160, 255);
                Color c2 = Color.Lerp(new Color(200, 200, 200), new Color(255, 80, 255), Main.rand.NextFloat());
                return Color.Lerp(c1, c2, Main.rand.NextFloat());
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Queen Slime");
            Main.npcFrameCount[npc.type] = 16;
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 12;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 5f;
            npc.damage = 130;
            npc.width = 114;
            npc.height = 110;
            npc.defense = 26;
            npc.lifeMax = 60000;
            npc.aiStyle = -1;
            aiType = -1;
            npc.Opacity = 1f;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(0, 33, 0, 0);
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.netAlways = true;
            npc.boss = true;
            music = MusicID.Boss1;
        }

        public override void AI()
        {
            npc.TargetClosestIfTargetIsInvalid();

            npc.damage = npc.defDamage;
            switch (AttackType)
            {
                case QueenSlimeAttackType.RepeatedSlams:
                    DoBehavior_RepeatedSlams();
                    break;
                case QueenSlimeAttackType.CrystalShatter:
                    DoBehavior_CrystalShatter();
                    break;
            }
            SlamTelegraphInterpolant = 0f;
            AttackTimer++;
        }

        public void DoBehavior_RepeatedSlams()
        {
            int slamCount = 5;
            int hoverTime = 150;
            int slamDelay = 16;
            int slamTime = 180;
            int postSlamSitTime = 17;
            int gelSlamCount = 7;
            float gelShootSpeed = 9.5f;
            ref float slamCounter = ref npc.Infernum().ExtraAI[0];

            // Hover into position.
            if (AttackTimer < hoverTime)
            {
                float hoverSpeed = MathHelper.Lerp(12f, 48f, Utils.InverseLerp(0f, 18f, AttackTimer, true));
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 384f;
                npc.velocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), hoverSpeed);
                npc.rotation = npc.rotation.AngleLerp(0f, 0.1f);
                npc.damage = 0;

                // Stop in place when close to the hover position.
                if (npc.WithinRange(hoverDestination, 30f))
                {
                    AttackTimer = hoverTime;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
                FrameType = QueenSlimeFrameType.HoverRedirect;
            }

            // Sit in place before slamming.
            else if (AttackTimer < hoverTime + slamDelay)
            {
                npc.velocity = -Vector2.UnitY * Utils.InverseLerp(0f, slamDelay - 6f, AttackTimer - hoverTime, true) * 6f;
                FrameType = QueenSlimeFrameType.SlamDownward;
            }

            // Slam downward.
            else if (AttackTimer < hoverTime + slamDelay + slamTime)
            {
                npc.velocity.X *= 0.8f;
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.9f, 0f, 12.5f);
                for (int i = 0; i < 5; i++)
                {
                    npc.position += npc.velocity;

                    // Slam into the ground once it's reached and create hit effects.
                    if (OnSolidGround && npc.Bottom.Y >= Target.Top.Y)
                    {
                        // Create a shockwave and bursts of gel that accelerate.
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SlimeGodExit"), npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Utilities.NewProjectileBetter(npc.Bottom, Vector2.Zero, ModContent.ProjectileType<StompShockwave>(), 135, 0f);
                            for (int j = 0; j < gelSlamCount; j++)
                            {
                                float offsetAngle = MathHelper.Lerp(-0.6f, 0.6f, j / (float)(gelSlamCount - 1f));
                                Vector2 gelSpawnPosition = npc.Center;
                                Vector2 gelShootVelocity = (Target.Center - gelSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedBy(offsetAngle) * gelShootSpeed;
                                Utilities.NewProjectileBetter(gelSpawnPosition, gelShootVelocity, ModContent.ProjectileType<AcceleratingGel>(), 125, 0f);
                            }
                        }

                        npc.velocity = Vector2.Zero;
                        AttackTimer = hoverTime + slamDelay + slamTime;
                        npc.netUpdate = true;

                        slamCounter++;
                        if (slamCounter >= slamCount)
                            SelectNextAttack();
                        break;
                    }
                }

                FrameType = QueenSlimeFrameType.SlamDownward;
            }

            // Sit in place after slamming.
            else
            {
                while (Collision.SolidCollision(npc.Bottom, 1, 1))
                    npc.position.Y--;
                npc.position = (npc.position / 16f).Floor() * 16f;
                if (!OnSolidGround)
                    npc.position.Y += 16f;

                FrameType = QueenSlimeFrameType.FlatState;

                if (AttackTimer >= hoverTime + slamDelay + slamTime + postSlamSitTime)
                {
                    AttackTimer = 0f;
                }
            }
        }

        public void DoBehavior_CrystalShatter()
        {

        }

        public void SelectNextAttack()
        {
            switch (AttackType)
            {
                case QueenSlimeAttackType.RepeatedSlams:
                    AttackType = QueenSlimeAttackType.CrystalShatter;
                    break;
                case QueenSlimeAttackType.CrystalShatter:
                    AttackType = QueenSlimeAttackType.RepeatedSlams;
                    break;
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public void PrepareShader()
        {
            Main.graphics.GraphicsDevice.Textures[1] = ModContent.GetTexture("InfernumMode/ExtraTextures/QueenSlimeRainbow");
            Main.graphics.GraphicsDevice.Textures[2] = ModContent.GetTexture("InfernumMode/ExtraTextures/QueenSlimeFadeMap");
        }

        public void DrawWings(SpriteBatch spriteBatch, Color color)
        {
            Texture2D wingTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/QueenSlime/QueenSlimeWings");
            Rectangle wingFrame = wingTexture.Frame(1, 4, 0, (int)npc.localAI[3] / 6);

            for (int i = 0; i < 2; i++)
            {
                float horizontalDirection = 1f;
                float horizontalOffset = 0f;
                SpriteEffects direction = SpriteEffects.None;
                if (i == 1)
                {
                    horizontalDirection = 0f;
                    horizontalOffset = 2f;
                    direction = SpriteEffects.FlipHorizontally;
                }

                Vector2 origin = npc.frame.Size() * new Vector2(horizontalDirection, 0.5f);
                Vector2 wingDrawPosition = npc.Center + Vector2.UnitX * horizontalOffset;
                if (npc.rotation != 0f)
                    wingDrawPosition = wingDrawPosition.RotatedBy(npc.rotation, npc.Bottom);

                wingDrawPosition -= Main.screenPosition;
                float rotationOffset = MathHelper.Clamp(npc.velocity.Y, -6f, 6f) * -0.1f;
                if (i == 0)
                    rotationOffset *= -1f;

                spriteBatch.Draw(wingTexture, wingDrawPosition, wingFrame, color, npc.rotation + rotationOffset, origin, 0.8f, direction, 0f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 drawBottom = npc.Bottom - Main.screenPosition;
            drawBottom.Y += 2f;
            int frame = npc.frame.Y / npc.frame.Height;
            Rectangle frameThing = texture.Frame(2, Main.npcFrameCount[npc.type], frame / Main.npcFrameCount[npc.type], frame % Main.npcFrameCount[npc.type]);
            frameThing.Inflate(0, -2);
            Vector2 origin = frameThing.Size() * new Vector2(0.5f, 1f);
            Color color = Color.Lerp(Color.White, drawColor, 0.5f);
            if (InPhase2)
                DrawWings(spriteBatch, color);

            Texture2D crystalTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/QueenSlime/QueenSlimeCrystal");
            Rectangle crystalFrame = crystalTexture.Frame();
            Vector2 crystalOrigin = crystalFrame.Size() * 0.5f;
            Vector2 crystalDrawPosition = npc.Center;
            float crystalDrawOffset = 0f;
            switch (frame)
            {
                case 1:
                case 6:
                    crystalDrawOffset -= 10f;
                    break;
                case 3:
                case 5:
                    crystalDrawOffset += 10f;
                    break;
                case 4:
                case 12:
                case 13:
                case 14:
                case 15:
                    crystalDrawOffset += 18f;
                    break;
                case 7:
                case 8:
                    crystalDrawOffset -= 14f;
                    break;
                case 9:
                    crystalDrawOffset -= 16f;
                    break;
                case 10:
                    crystalDrawOffset -= 18f;
                    break;
                case 11:
                    crystalDrawOffset += 20f;
                    break;
                case 20:
                    crystalDrawOffset -= 14f;
                    break;
                case 21:
                case 23:
                    crystalDrawOffset -= 18f;
                    break;
                case 22:
                    crystalDrawOffset -= 22f;
                    break;
            }

            crystalDrawPosition.Y += crystalDrawOffset;
            if (npc.rotation != 0f)
                crystalDrawPosition = crystalDrawPosition.RotatedBy(npc.rotation, npc.Bottom);

            crystalDrawPosition -= Main.screenPosition;

            spriteBatch.EnterShaderRegion();

            PrepareShader();
            GameShaders.Misc["Infernum:QueenSlime"].Apply();
            if (AttackType == QueenSlimeAttackType.RepeatedSlams && npc.velocity.Y != 0f)
            {
                float scaleFactor = 1f;
                if (npc.ai[2] == 1f)
                    scaleFactor = 6f;

                for (int i = 7; i >= 0; i--)
                {
                    float afterimageOpacity = 1f - i / 8f;
                    Vector2 afterimageDrawBottom = Vector2.Lerp(npc.oldPos[i], npc.oldPos[0], 0.56f) + new Vector2(npc.width * 0.5f, npc.height);
                    afterimageDrawBottom -= (npc.Bottom - Vector2.Lerp(afterimageDrawBottom, npc.Bottom, 0.75f)) * scaleFactor;
                    afterimageDrawBottom -= Main.screenPosition;
                    Color afterimageColor = color * afterimageOpacity;
                    spriteBatch.Draw(texture, afterimageDrawBottom, frameThing, afterimageColor, npc.rotation, origin, npc.scale, SpriteEffects.FlipHorizontally, 0f);
                }
            }
            spriteBatch.ExitShaderRegion();

            spriteBatch.Draw(crystalTexture, crystalDrawPosition, crystalFrame, color, npc.rotation, crystalOrigin, 1f, SpriteEffects.FlipHorizontally, 0f);
            PrepareShader();
            GameShaders.Misc["Infernum:QueenSlime"].Apply();

            spriteBatch.EnterShaderRegion();

            DrawData drawData = new DrawData(texture, drawBottom, frameThing, npc.GetAlpha(color), npc.rotation, origin, npc.scale, SpriteEffects.FlipHorizontally, 0);
            PrepareShader();
            GameShaders.Misc["Infernum:QueenSlime"].Apply(drawData);
            drawData.Draw(spriteBatch);
            spriteBatch.ExitShaderRegion();

            Texture2D crownTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/QueenSlime/QueenSlimeCrown");
            frameThing = crownTexture.Frame();
            origin = frameThing.Size() * 0.5f;
            drawBottom = new Vector2(npc.Center.X, npc.Top.Y - frameThing.Bottom + 44f);
            float crownOffset = 0f;
            switch (frame)
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

            drawBottom.Y += crownOffset;
            if (npc.rotation != 0f)
                drawBottom = drawBottom.RotatedBy(npc.rotation, npc.Bottom);

            drawBottom -= Main.screenPosition;
            spriteBatch.Draw(crownTexture, drawBottom, frameThing, color, npc.rotation, origin, 1f, SpriteEffects.FlipHorizontally, 0f);
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frame.Width = 180;

            int frame = npc.frame.Y / frameHeight;
            switch (FrameType)
            {
                case QueenSlimeFrameType.HoverRedirect:
                    npc.frameCounter++;
                    if (npc.frameCounter >= 6)
                        frame++;
                    frame = Utils.Clamp(frame, 4, 6);
                    break;
                case QueenSlimeFrameType.SlamDownward:
                    frame = 10;
                    break;
                case QueenSlimeFrameType.SlamDownwardPreparation:
                    if (frame < 8 || frame > 10)
                    {
                        frame = 8;
                        npc.frameCounter = -1.0;
                    }

                    npc.frameCounter++;
                    if (npc.frameCounter >= 8D)
                    {
                        npc.frameCounter = 0D;

                        frame++;
                        if (frame >= 10)
                            frame = 10;
                    }
                    break;
                case QueenSlimeFrameType.FlatState:
                    if (frame < 11f || frame > 16f)
                        npc.frameCounter = 0;

                    frame = (int)MathHelper.Lerp(16f, 11f, MathHelper.Clamp((float)npc.frameCounter / 12f, 0f, 1f));
                    npc.frameCounter++;
                    break;
                case QueenSlimeFrameType.Flying:
                    if (frame < 20 || frame > 23)
                    {
                        if (frame < 4 || frame > 7)
                        {
                            frame = 4;
                            npc.frameCounter = -1.0;
                        }

                        if ((npc.frameCounter += 1D) >= 4.0)
                        {
                            npc.frameCounter = 0.0;
                            frame++;
                            if (frame >= 7)
                                frame = (!InPhase2 ? 7 : 22);
                        }
                    }
                    else if ((npc.frameCounter += 1D) >= 5.0)
                    {
                        npc.frameCounter = 0.0;
                        frame++;
                        if (frame >= 24)
                            frame = 20;
                    }
                    break;
            }
            npc.frame.Y = frame * frameHeight;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 12; i++)
            {
                Color dustColor = DustColor;
                dustColor.A = 150;
                Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, 4, 2 * hitDirection, -1f, 50, dustColor);
                dust.velocity *= 0.3f;
                dust.velocity += npc.velocity * 0.3f;
                if (Main.rand.Next(3) == 0)
                {
                    dust.noGravity = true;
                    dust.scale *= 1.2f;
                }
            }
        }

        public override bool CheckActive() => false;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.8f * bossLifeScale);
            npc.damage = (int)(npc.damage * 0.8f);
        }
    }
}
