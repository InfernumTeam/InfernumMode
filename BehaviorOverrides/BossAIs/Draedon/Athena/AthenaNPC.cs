using CalamityMod;
using CalamityMod.Items.Potions;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

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
            CircleOfLightning,
            ExowlHologramSwarm,
            AimedPulseLasers
        }

        public enum AthenaTurretFrameType
        {
            Blinking,
            OpenMainTurret,
            CloseAllTurrets
        }

        public PrimitiveTrail FlameTrail = null;

        public PrimitiveTrail LightRayDrawer = null;

        public AthenaTurret[] Turrets = new AthenaTurret[5];

        public Player Target => Main.player[NPC.target];

        public AthenaAttackType AttackState
        {
            get => (AthenaAttackType)(int)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public bool HasInitialized
        {
            get => NPC.ai[2] == 1f;
            set => NPC.ai[2] = value.ToInt();
        }

        public AthenaTurretFrameType TurretFrameState
        {
            get => (AthenaTurretFrameType)(int)NPC.localAI[0];
            set => NPC.localAI[0] = (int)value;
        }

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float MinionRedCrystalGlow => ref NPC.localAI[1];

        public ref float TelegraphInterpolant => ref NPC.localAI[2];

        public ref float TelegraphRotation => ref NPC.localAI[3];

        public static Vector2 UniversalVerticalTurretOffset => Vector2.UnitY * -22f;

        public static Vector2[] TurretOffsets => new Vector2[]
        {
            UniversalVerticalTurretOffset + new Vector2(-66f, -6f),
            UniversalVerticalTurretOffset + new Vector2(-36f, -2f),
            UniversalVerticalTurretOffset,
            UniversalVerticalTurretOffset + new Vector2(36f, -2f),
            UniversalVerticalTurretOffset + new Vector2(66f, -6f),
        };

        public Vector2 MainTurretCenter => NPC.Center + TurretOffsets[2];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XM-04 Athena");
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = 100;
            NPC.width = 230;
            NPC.height = 170;
            NPC.defense = 100;
            NPC.DR_NERD(0.35f);
            NPC.LifeMaxNERB(1300000, 1300000, 1300000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.value = Item.buyPrice(3, 33, 0, 0);
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.netAlways = true;
            NPC.boss = true;
            NPC.Calamity().VulnerableToSickness = false;
            NPC.Calamity().VulnerableToElectricity = true;
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
                NPC.netUpdate = true;
            }

            // Search for a target.
            NPC.TargetClosestIfTargetIsInvalid();

            // Set the global whoAmI index.
            GlobalNPCOverrides.Athena = NPC.whoAmI;

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
                case AthenaAttackType.ExowlHologramSwarm:
                    DoBehavior_ExowlHologramSwarm();
                    break;
                case AthenaAttackType.AimedPulseLasers:
                    DoBehavior_AimedPulseLasers();
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
                NPC.velocity *= 0.5f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);
            }
            
            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
                NPC.velocity = Vector2.Zero;
                NPC.Center = Target.Center + (MathHelper.TwoPi * Main.rand.Next(4) / 4f).ToRotationVector2() * 450f;
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                NPC.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.15f, 0f, 1f);

            // Summon a circle of minions that spin in place and act as a barrier.
            // The player can technically teleport out of the circle, but doing so prevents seeing the boss.
            if (AttackTimer == teleportTime + circleSummonDelay)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ThunderStrike"), NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExowlCircleSummonBoom>(), 0, 0f);

                    List<int> circle = new();
                    for (int i = 0; i < 30; i++)
                    {
                        int exowl = NPC.NewNPC(new InfernumSource(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Exowl>(), NPC.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().CircleCenter = NPC.Center;
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
                NPC.netUpdate = true;
            }

            // Determine telegraph variables.
            if (AttackTimer >= teleportTime + circleSummonDelay && AttackTimer < teleportTime + circleSummonDelay + telegraphTime)
                TelegraphRotation = TelegraphRotation.AngleLerp(NPC.AngleTo(Target.Center + Target.velocity * 15f), 0.3f);

            if (AttackTimer < teleportTime + circleSummonDelay + teleportTime + shootDelay)
                TelegraphInterpolant = Utils.GetLerpValue(0f, telegraphTime + shootDelay, AttackTimer - (teleportTime + circleSummonDelay), true);

            // Release the lightning.
            else if (AttackTimer % lightningShootRate == 0f)
            {
                if (AttackTimer % (lightningShootRate * 2f) == 0f)
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/TeslaCannonFire"), MainTurretCenter);
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
                    NPC.netUpdate = true;
                }
            }

            MinionRedCrystalGlow = Utils.GetLerpValue(0f, 60f, AttackTimer - (teleportTime + circleSummonDelay), true);

            if (AttackTimer >= teleportTime + circleSummonDelay + teleportTime + shootDelay + lightningShootTime)
            {
                SelectNextAttack();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == ModContent.NPCType<Exowl>())
                        Main.npc[i].active = false;
                }
            }
        }

        public void DoBehavior_ExowlHologramSwarm()
        {
            int teleportFadeTime = 8;
            int teleportTime = teleportFadeTime * 2;
            int hologramCreationTime = 150;
            int holographFadeoutTime = 50;
            int hologramAttackTime = 360;
            ref float hologramInterpolant = ref NPC.Infernum().ExtraAI[0];
            ref float illusionCount = ref NPC.Infernum().ExtraAI[1];
            ref float hologramSpan = ref NPC.Infernum().ExtraAI[2];
            ref float exowlIllusionFadeInterpolant = ref NPC.Infernum().ExtraAI[3];
            ref float hologramRayDissipation = ref NPC.Infernum().ExtraAI[4];

            // Initialize the minion illusion count.
            if (illusionCount == 0f)
                illusionCount = 13f;

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                NPC.velocity *= 0.5f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);
            }

            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
                NPC.velocity = Vector2.Zero;
                NPC.Center = Target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 510f, -240f);
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/CodebreakerBeam"), NPC.Center);
                NPC.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.15f, 0f, 1f);

            // Calculate the hologram interpolant, span, and fade interpolant.
            hologramInterpolant = Utils.GetLerpValue(0f, 36f, AttackTimer - teleportTime, true);
            hologramRayDissipation = Utils.GetLerpValue(hologramCreationTime, hologramCreationTime - 12f, AttackTimer - teleportTime, true);
            hologramSpan = MathHelper.Lerp(8f, 450f, (float)Math.Pow(hologramInterpolant, 1.73) * hologramRayDissipation);
            exowlIllusionFadeInterpolant = Utils.GetLerpValue(0f, holographFadeoutTime, AttackTimer - teleportTime - hologramCreationTime, true);
            if (AttackTimer > teleportTime + hologramCreationTime)
            {
                Vector2 idealVelocity = NPC.SafeDirectionTo(Target.Center + (MathHelper.TwoPi * AttackTimer / 150f).ToRotationVector2() * 500f) * 16f;
                NPC.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 20f);
                hologramInterpolant = 0f;
            }

            // Transform the holograms into true exowls.
            if (AttackTimer == teleportTime + hologramCreationTime)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/LargeWeaponFire"), NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int realExowlIndex = Main.rand.Next((int)illusionCount);
                    for (int i = 0; i < illusionCount; i++)
                    {
                        Vector2 hologramPosition = GetHologramPosition(i, illusionCount, hologramSpan, hologramInterpolant);
                        int exowl = NPC.NewNPC(new InfernumSource(), (int)hologramPosition.X, (int)hologramPosition.Y, ModContent.NPCType<Exowl>(), NPC.whoAmI);
                        if (Main.npc.IndexInRange(exowl))
                        {
                            Main.npc[exowl].ModNPC<Exowl>().UseConfusionEffect = true;
                            Main.npc[exowl].ModNPC<Exowl>().IsIllusion = i != realExowlIndex;
                        }
                    }
                    NPC.netUpdate = true;
                }
            }

            if (AttackTimer == teleportTime + hologramCreationTime + hologramAttackTime)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == ModContent.NPCType<Exowl>())
                        Main.npc[i].active = false;
                }
                NPC.netUpdate = true;
            }

            if (AttackTimer >= teleportTime + hologramCreationTime + hologramAttackTime + 45f)
                SelectNextAttack();
        }

        public void DoBehavior_AimedPulseLasers()
        {
            int teleportFadeTime = 10;
            int teleportTime = teleportFadeTime * 2;

            // Fade out.
            if (AttackTimer < teleportFadeTime)
            {
                NPC.velocity *= 0.5f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);
            }

            // Teleport.
            if (AttackTimer == teleportFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
                NPC.velocity = Vector2.Zero;
                NPC.Center = Target.Center - Target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 400f;
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/CodebreakerBeam"), NPC.Center);
                NPC.netUpdate = true;
            }

            // Fade back in.
            if (AttackTimer >= teleportFadeTime && AttackTimer < teleportTime)
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.15f, 0f, 1f);
        }

        public void SelectNextAttack()
        {
            for (int i = 0; i < 5; i++)
                NPC.Infernum().ExtraAI[i] = 0f;

            switch (AttackState)
            {
                case AthenaAttackType.CircleOfLightning:
                    AttackState = AthenaAttackType.ExowlHologramSwarm;
                    break;
                case AthenaAttackType.ExowlHologramSwarm:
                    AttackState = AthenaAttackType.AimedPulseLasers;
                    break;
            }

            AttackTimer = 0f;
            NPC.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Frames and Drawcode

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            for (int i = 0; i < Turrets.Length; i++)
            {
                int frame = Turrets[i].Frame;
                int maxFrame = Turrets[i].IsSmall ? 3 : 4;

                switch (TurretFrameState)
                {
                    case AthenaTurretFrameType.Blinking:
                        float frameInterpolant = (float)Math.Sin(NPC.frameCounter * 0.13f + i * 1.02f) * 0.5f + 0.5f;
                        frame = (int)(frameInterpolant * maxFrame * 0.99f);
                        break;
                    case AthenaTurretFrameType.OpenMainTurret:
                        if (NPC.frameCounter % 6 == 5)
                        {
                            if (Turrets[i].IsSmall)
                                frame--;
                            else
                                frame++;
                        }
                        break;
                    case AthenaTurretFrameType.CloseAllTurrets:
                        if (NPC.frameCounter % 6 == 5)
                            frame--;
                        break;
                }

                Turrets[i].Frame = frame;
            }
        }

        public float FlameTrailPulse => (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + NPC.whoAmI * 111.5856f) * 0.5f + 0.5f;

        public float FlameTrailWidthFunction(float completionRatio)
        {
            float maxWidth = MathHelper.Lerp(15f, 80f, FlameTrailPulse);
            return MathHelper.SmoothStep(maxWidth, 8f, completionRatio);
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
            return color;
        }

        public float RayWidthFunction(float completionRatio)
        {
            float widthOffset = (float)Math.Cos(completionRatio * 73f - Main.GlobalTimeWrappedHourly * 8f) * 
                Utils.GetLerpValue(0f, 0.1f, completionRatio, true) * 
                Utils.GetLerpValue(1f, 0.9f, completionRatio, true);
            return MathHelper.Lerp(2f, NPC.Infernum().ExtraAI[2] * 0.7f, completionRatio) + widthOffset;
        }
        public Color RayColorFunction(float completionRatio)
        {
            return Color.Cyan * Utils.GetLerpValue(0.8f, 0.5f, completionRatio, true) * 0.6f;
        }

        public void DrawLightRay(float initialRayRotation, float rayBrightness, Vector2 rayStartingPoint)
        {
            if (LightRayDrawer is null)
                LightRayDrawer = new PrimitiveTrail(RayWidthFunction, RayColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
            Vector2 currentRayDirection = initialRayRotation.ToRotationVector2();

            float length = rayBrightness * NPC.Infernum().ExtraAI[4] * 400f;
            List<Vector2> points = new();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint + initialRayRotation.ToRotationVector2() * length, i / 12f));

            LightRayDrawer.Draw(points, -Main.screenPosition, 47);
        }

        public void DrawExowlHologram(SpriteBatch spriteBatch, Vector2 drawPosition, int exowlFrame, float hologramInterpolant)
        {
            float hologramOpacity = (float)Math.Pow(hologramInterpolant, 0.45);
            Texture2D exowlTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/Exowl").Value;
            Rectangle frame = exowlTexture.Frame(1, 3, 0, exowlFrame);

            DrawData fuckYou = new(exowlTexture, drawPosition, frame, Color.White * hologramOpacity, 0f, frame.Size() * 0.5f, 1f, 0, 0);
            GameShaders.Misc["Infernum:Hologram"].UseOpacity(hologramInterpolant);
            GameShaders.Misc["Infernum:Hologram"].UseColor(Color.Cyan);
            GameShaders.Misc["Infernum:Hologram"].UseSecondaryColor(Color.Gold);
            GameShaders.Misc["Infernum:Hologram"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/HologramTexture"));
            GameShaders.Misc["Infernum:Hologram"].Apply(fuckYou);
            fuckYou.Draw(Main.spriteBatch);
        }

        public Vector2 GetHologramPosition(int index, float illusionCount, float hologramSpan, float hologramInterpolant)
        {
            float completionRatio = index / (illusionCount - 1f);
            float hologramHorizontalOffset = MathHelper.Lerp(-0.5f, 0.5f, completionRatio) * hologramSpan;
            float hologramVerticalOffset = Utils.GetLerpValue(0f, 0.5f, hologramInterpolant, true) * 200f + CalamityUtils.Convert01To010(completionRatio) * 40f;
            return NPC.Top + new Vector2(hologramHorizontalOffset, -hologramVerticalOffset);
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Drift towards a brighter color.
            drawColor = Color.Lerp(drawColor, Color.White, 0.45f);

            // Declare the trail drawers if they have yet to be defined.
            if (FlameTrail is null)
                FlameTrail = new PrimitiveTrail(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            // Draw a flame trail on the thrusters.
            for (int direction = -1; direction <= 1; direction++)
            {
                Vector2 baseDrawOffset = new Vector2(0f, direction == 0f ? -26f : -34f).RotatedBy(NPC.rotation);
                baseDrawOffset += new Vector2(direction * 64f, 0f).RotatedBy(NPC.rotation);

                float backFlameLength = direction == 0f ? 340f : 250f;
                backFlameLength *= MathHelper.Lerp(0.7f, 1f, 1f - FlameTrailPulse);

                Vector2 drawStart = NPC.Center + baseDrawOffset;
                Vector2 drawEnd = drawStart - (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * backFlameLength;
                Vector2[] drawPositions = new Vector2[]
                {
                    drawStart,
                    drawEnd
                };

                for (int i = 0; i < 6; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
                    FlameTrail.Draw(drawPositions, drawOffset - screenPos, 70);
                }
            }

            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaNPC_Glowmask").Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Vector2 origin = NPC.frame.Size() * 0.5f;
            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, origin, NPC.scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, origin, NPC.scale, 0, 0f);

            for (int i = 0; i < Turrets.Length; i++)
            {
                if (Turrets[i] is null)
                    break;

                int totalFrames = 4;
                Texture2D turretTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretLarge").Value;
                if (Turrets[i].IsSmall)
                {
                    totalFrames = 3;
                    turretTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/AthenaTurretSmall").Value;
                }

                // totalFrames is used instead of totalFrames - 1f in division to allow for some of the original color to still go through.
                // Even as a full crystal, I'd prefer the texture not be completely fullbright.
                Color turretColor = Color.Lerp(drawColor, Color.White, Turrets[i].Frame / (float)totalFrames);
                Rectangle turretFrame = turretTexture.Frame(1, totalFrames, 0, Turrets[i].Frame);
                Vector2 turretOrigin = turretFrame.Size() * 0.5f;
                Vector2 turretDrawPosition = drawPosition + TurretOffsets[i].RotatedBy(NPC.rotation);
                Main.spriteBatch.Draw(turretTexture, turretDrawPosition, turretFrame, NPC.GetAlpha(turretColor), 0f, turretOrigin, NPC.scale, 0, 0f);
            }

            // Draw a line telegraph as necessary
            if (TelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D telegraphTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/BloomLine").Value;
                float telegraphScaleFactor = TelegraphInterpolant * 1.2f;

                for (float offsetAngle = 0f; offsetAngle < 0.2f; offsetAngle += 0.03f)
                {
                    Vector2 telegraphStart = MainTurretCenter + (TelegraphRotation + offsetAngle).ToRotationVector2() * 20f - screenPos;
                    Vector2 telegraphOrigin = new Vector2(0.5f, 0f) * telegraphTexture.Size();
                    Vector2 telegraphScale = new(telegraphScaleFactor, 3f);
                    Color telegraphColor = new Color(74, 255, 204) * (float)Math.Pow(TelegraphInterpolant, 0.79) * ((0.2f - offsetAngle) / 0.2f) * 1.6f;
                    Main.spriteBatch.Draw(telegraphTexture, telegraphStart, null, telegraphColor, TelegraphRotation + offsetAngle - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                }

                Main.spriteBatch.ResetBlendState();
            }

            // Draw holograms.
            if (AttackState == AthenaAttackType.ExowlHologramSwarm)
            {
                float hologramInterpolant = NPC.Infernum().ExtraAI[0];
                float illusionCount = NPC.Infernum().ExtraAI[1];
                float hologramSpan = NPC.Infernum().ExtraAI[2];

                Main.spriteBatch.EnterShaderRegion();

                float rayBrightness = Utils.GetLerpValue(0f, 0.45f, hologramInterpolant, true);
                DrawLightRay(-MathHelper.PiOver2, rayBrightness, MainTurretCenter);
                Main.spriteBatch.EnterShaderRegion();

                for (int i = 0; i < illusionCount; i++)
                {
                    int illusionFrame = (int)(Main.GlobalTimeWrappedHourly * 6f + i) % 3;
                    float completionRatio = i / (illusionCount - 1f);
                    Vector2 hologramDrawPosition = GetHologramPosition(i, illusionCount, hologramSpan, hologramInterpolant) - screenPos;
                    DrawExowlHologram(spriteBatch, hologramDrawPosition, illusionFrame, hologramInterpolant);
                }
                Main.spriteBatch.ExitShaderRegion();
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
                Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1f);
        }

        public override bool CheckActive() => false;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.5f * bossLifeScale);
            NPC.damage = (int)(NPC.damage * 0.8f);
        }
        #endregion Misc Things
    }
}
