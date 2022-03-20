using CalamityMod;
using CalamityMod.Items.Potions;
using CalamityMod.Items.TreasureBags;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    [AutoloadBossHead]
    public class AthenaNPC : ModNPC
    {
        public class AthenaTurret
        {
            private int frame;
            public int Frame
            {
                get => frame;
                set => frame = Utils.Clamp(value, 0, IsSmall ? 2 : 3);
            }
            public bool IsSmall;
        }

        public enum AthenaAttackType
        {
            CircleOfLightning
        }

        public enum AthenaTurretFrameType
        {
            Blinking,
            OpenMainTurret,
            CloseAllTurrets
        }

        public PrimitiveTrail FlameTrail = null;

        public AthenaTurret[] Turrets = new AthenaTurret[5];

        public Player Target => Main.player[npc.target];

        public AthenaAttackType AttackState
        {
            get => (AthenaAttackType)(int)npc.ai[0];
            set => npc.ai[0] = (int)value;
        }

        public bool HasInitialized
        {
            get => npc.ai[2] == 1f;
            set => npc.ai[2] = value.ToInt();
        }

        public AthenaTurretFrameType TurretFrameState
        {
            get => (AthenaTurretFrameType)(int)npc.localAI[0];
            set => npc.localAI[0] = (int)value;
        }

        public ref float AttackTimer => ref npc.ai[1];

        public ref float MinionRedCrystalGlow => ref npc.localAI[1];

        public ref float TelegraphInterpolant => ref npc.localAI[2];

        public ref float TelegraphRotation => ref npc.localAI[3];

        public static Vector2 UniversalVerticalTurretOffset => Vector2.UnitY * -22f;

        public static Vector2[] TurretOffsets => new Vector2[]
        {
            UniversalVerticalTurretOffset + new Vector2(-66f, -6f),
            UniversalVerticalTurretOffset + new Vector2(-36f, -2f),
            UniversalVerticalTurretOffset,
            UniversalVerticalTurretOffset + new Vector2(36f, -2f),
            UniversalVerticalTurretOffset + new Vector2(66f, -6f),
        };

        public Vector2 MainTurretCenter => npc.Center + TurretOffsets[2];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XM-04 Athena");
            NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = npc.oldPos.Length;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 5f;
            npc.damage = 100;
            npc.width = 220;
            npc.height = 248;
            npc.defense = 100;
            npc.DR_NERD(0.35f);
            npc.LifeMaxNERB(1300000, 1300000, 1300000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
            npc.aiStyle = -1;
            aiType = -1;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(3, 33, 0, 0);
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.netAlways = true;
            npc.boss = true;
            bossBag = ModContent.ItemType<DraedonTreasureBag>();
            npc.Calamity().VulnerableToSickness = false;
            npc.Calamity().VulnerableToElectricity = true;
        }

        #region Syncing

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(TelegraphRotation);
            writer.Write(Turrets.Length);
            BitsByte[] turretSizes = new BitsByte[Turrets.Length / 8 + 1];
            for (int i = 0; i < Turrets.Length; i++)
                turretSizes[i / 8][i % 8] = Turrets[i].IsSmall;

            for (int i = 0; i < turretSizes.Length; i++)
                writer.Write(turretSizes[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            TelegraphRotation = reader.ReadSingle();
            int turretCount = reader.ReadInt32();
            int turretSizeCount = turretCount / 8 + 1;
            Turrets = new AthenaTurret[turretCount];

            for (int i = 0; i < turretSizeCount; i++)
            {
                BitsByte turretSizes = reader.ReadByte();
                for (int j = 0; j < 8; j++)
                {
                    if (i * 8 + j >= turretCount)
                        break;

                    Turrets[i * 8 + j].IsSmall = turretSizes[j];
                }
            }
        }

        #endregion Syncing

        #region AI and Behaviors
        public void InitializeTurrets()
        {
            Turrets = new AthenaTurret[5];
            for (int i = 0; i < Turrets.Length; i++)
            {
                Turrets[i] = new AthenaTurret()
                {
                    IsSmall = i != 2
                };
            }
        }

        public override void AI()
        {
            // Handle initializations.
            if (!HasInitialized)
            {
                InitializeTurrets();
                HasInitialized = true;
                npc.netUpdate = true;
            }

            // Search for a target.
            npc.TargetClosestIfTargetIsInvalid();

            // Set the global whoAmI index.
            GlobalNPCOverrides.Athena = npc.whoAmI;

            // Reset things.
            MinionRedCrystalGlow = 0f;
            TelegraphInterpolant = 0f;
            TurretFrameState = AthenaTurretFrameType.CloseAllTurrets;

            // Handle attacks.
            switch (AttackState)
            {
                case AthenaAttackType.CircleOfLightning:
                    DoBehavior_CircleOfLightning();
                    break;
            }
            AttackTimer++;
        }

        public void DoBehavior_CircleOfLightning()
        {
            int teleportFadeTime = 8;
            int teleportTime = teleportFadeTime * 2;
            int circleSummonDelay = 36;
            int telegraphTime = 42;
            int shootDelay = 38;
            int lightningShootRate = 4;
            int lightningShootTime = 170;

            TurretFrameState = AthenaTurretFrameType.CloseAllTurrets;
            if (AttackTimer >= teleportTime + circleSummonDelay)
                TurretFrameState = AthenaTurretFrameType.Blinking;
            if (AttackTimer >= teleportTime + circleSummonDelay + telegraphTime)
                TurretFrameState = AthenaTurretFrameType.OpenMainTurret;

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                npc.velocity *= 0.5f;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.15f, 0f, 1f);
            }
            
            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                Main.PlaySound(SoundID.Item103, npc.Center);
                npc.velocity = Vector2.Zero;
                npc.Center = Target.Center + (MathHelper.TwoPi * Main.rand.Next(4) / 4f).ToRotationVector2() * 450f;
                Main.PlaySound(SoundID.Item104, npc.Center);
                npc.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.15f, 0f, 1f);

            // Summon a circle of minions that spin in place and act as a barrier.
            // The player can technically teleport out of the circle, but doing so prevents seeing the boss.
            if (AttackTimer == teleportTime + circleSummonDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThunderStrike"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ExowlCircleSummonBoom>(), 0, 0f);

                    List<int> circle = new List<int>();
                    for (int i = 0; i < 30; i++)
                    {
                        int exowl = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Exowl>(), npc.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().CircleCenter = npc.Center;
                            Main.npc[exowl].ModNPC<Exowl>().CircleRadius = 1100f;
                            Main.npc[exowl].ModNPC<Exowl>().CircleOffsetAngle = MathHelper.TwoPi * i / 30f;
                            Main.npc[exowl].netUpdate = true;
                            circle.Add(exowl);
                        }
                    }

                    // Attach every member of the circle.
                    for (int i = 0; i < circle.Count; i++)
                        Main.npc[circle[i]].ModNPC<Exowl>().NPCToAttachTo = circle[(i + 1) % circle.Count];
                }
                npc.netUpdate = true;
            }

            // Determine telegraph variables.
            if (AttackTimer >= teleportTime + circleSummonDelay && AttackTimer < teleportTime + circleSummonDelay + telegraphTime)
                TelegraphRotation = TelegraphRotation.AngleLerp(npc.AngleTo(Target.Center + Target.velocity * 15f), 0.3f);

            if (AttackTimer < teleportTime + circleSummonDelay + teleportTime + shootDelay)
                TelegraphInterpolant = Utils.InverseLerp(0f, telegraphTime + shootDelay, AttackTimer - (teleportTime + circleSummonDelay), true);

            // Release the lightning.
            else if (AttackTimer % lightningShootRate == 0f)
            {
                if (AttackTimer % (lightningShootRate * 2f) == 0f)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), MainTurretCenter);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lightningShootVelocity = TelegraphRotation.ToRotationVector2() * 8.4f;
                    int lightning = Utilities.NewProjectileBetter(MainTurretCenter - lightningShootVelocity * 7.6f, lightningShootVelocity, ModContent.ProjectileType<TerateslaLightningBlast>(), 530, 0f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Main.projectile[lightning].ai[0] = TelegraphRotation;
                        Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    }

                    TelegraphRotation += MathHelper.TwoPi / lightningShootTime * lightningShootRate * 1.75f;
                    npc.netUpdate = true;
                }
            }

            MinionRedCrystalGlow = Utils.InverseLerp(0f, 60f, AttackTimer - (teleportTime + circleSummonDelay), true);

            if (AttackTimer >= teleportTime + circleSummonDelay + teleportTime + shootDelay + lightningShootTime)
            {
                AttackTimer = 0f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == ModContent.NPCType<Exowl>())
                        Main.npc[i].active = false;
                }
            }
        }

        public void SelectNextAttack()
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            AttackTimer = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Frames and Drawcode

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter++;
            for (int i = 0; i < Turrets.Length; i++)
            {
                int frame = Turrets[i].Frame;
                int maxFrame = Turrets[i].IsSmall ? 3 : 4;

                switch (TurretFrameState)
                {
                    case AthenaTurretFrameType.Blinking:
                        float frameInterpolant = (float)Math.Sin(npc.frameCounter * 0.13f + i * 1.02f) * 0.5f + 0.5f;
                        frame = (int)(frameInterpolant * maxFrame * 0.99f);
                        break;
                    case AthenaTurretFrameType.OpenMainTurret:
                        if (npc.frameCounter % 6 == 5)
                        {
                            if (Turrets[i].IsSmall)
                                frame--;
                            else
                                frame++;
                        }
                        break;
                    case AthenaTurretFrameType.CloseAllTurrets:
                        if (npc.frameCounter % 6 == 5)
                            frame--;
                        break;
                }

                Turrets[i].Frame = frame;
            }
        }

        public float FlameTrailPulse => (float)Math.Sin(Main.GlobalTime * 6f + npc.whoAmI * 111.5856f) * 0.5f + 0.5f;

        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(15f, 80f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 8f, completionRatio);
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
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            // Drift towards a brighter color.
            drawColor = Color.Lerp(drawColor, Color.White, 0.45f);

            // Declare the trail drawers if they have yet to be defined.
            if (FlameTrail is null)
                FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.GetTexture("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            // Draw a flame trail on the thrusters.
            for (int direction = -1; direction <= 1; direction++)
            {
                Vector2 baseDrawOffset = new Vector2(0f, direction == 0f ? -26f : -34f).RotatedBy(npc.rotation);
                baseDrawOffset += new Vector2(direction * 64f, 0f).RotatedBy(npc.rotation);

                float backFlameLength = direction == 0f ? 340f : 250f;
                backFlameLength *= MathHelper.Lerp(0.7f, 1f, 1f - FlameTrailPulse);

                Vector2 drawStart = npc.Center + baseDrawOffset;
                Vector2 drawEnd = drawStart - (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * backFlameLength;
                Vector2[] drawPositions = new Vector2[]
                {
                    drawStart,
                    drawEnd
                };

                for (int i = 0; i < 6; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
                    FlameTrail.Draw(drawPositions, drawOffset - Main.screenPosition, 70);
                }
            }

            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaNPC_Glowmask");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, 0, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, 0, 0f);

            for (int i = 0; i < Turrets.Length; i++)
            {
                if (Turrets[i] is null)
                    break;

                int totalFrames = 4;
                Texture2D turretTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretLarge");
                if (Turrets[i].IsSmall)
                {
                    totalFrames = 3;
                    turretTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretSmall");
                }

                // totalFrames is used instead of totalFrames - 1f in division to allow for some of the original color to still go through.
                // Even as a full crystal, I'd prefer the texture not be completely fullbright.
                Color turretColor = Color.Lerp(drawColor, Color.White, Turrets[i].Frame / (float)totalFrames);
                Rectangle turretFrame = turretTexture.Frame(1, totalFrames, 0, Turrets[i].Frame);
                Vector2 turretOrigin = turretFrame.Size() * 0.5f;
                Vector2 turretDrawPosition = drawPosition + TurretOffsets[i].RotatedBy(npc.rotation);
                spriteBatch.Draw(turretTexture, turretDrawPosition, turretFrame, npc.GetAlpha(turretColor), 0f, turretOrigin, npc.scale, 0, 0f);
            }

            // Draw a line telegraph as necessary
            if (TelegraphInterpolant > 0f)
            {
                spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D telegraphTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
                float telegraphScaleFactor = TelegraphInterpolant * 1.2f;

                for (float offsetAngle = 0f; offsetAngle < 0.2f; offsetAngle += 0.03f)
                {
                    Vector2 telegraphStart = MainTurretCenter + (TelegraphRotation + offsetAngle).ToRotationVector2() * 20f - Main.screenPosition;
                    Vector2 telegraphOrigin = new Vector2(0.5f, 0f) * telegraphTexture.Size();
                    Vector2 telegraphScale = new Vector2(telegraphScaleFactor, 3f);
                    Color telegraphColor = new Color(74, 255, 204) * (float)Math.Pow(TelegraphInterpolant, 0.79) * ((0.2f - offsetAngle) / 0.2f) * 1.6f;
                    spriteBatch.Draw(telegraphTexture, telegraphStart, null, telegraphColor, TelegraphRotation + offsetAngle - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                }

                spriteBatch.ResetBlendState();
            }

            return false;
        }

        #endregion Frames and Drawcode

        #region Misc Things

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ModContent.ItemType<OmegaHealingPotion>();
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1f);
        }

        public override bool CheckActive() => false;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax * 0.5f * bossLifeScale);
            npc.damage = (int)(npc.damage * 0.8f);
        }
        #endregion Misc Things
    }
}
