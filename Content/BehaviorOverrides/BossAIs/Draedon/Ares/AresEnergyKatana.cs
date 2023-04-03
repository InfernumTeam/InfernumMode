using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityModClass = CalamityMod.CalamityMod;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;
using InfernumMode.Assets.ExtraTextures;
using CalamityMod.InverseKinematics;
using static Humanizer.In;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Primitives;
using Terraria.Graphics.Shaders;
using System.Collections.Generic;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresEnergyKatana : ModNPC
    {
        private bool katanaIsInUse;

        public LimbCollection Limbs = new(new CyclicCoordinateDescentUpdateRule(0.27f, MathHelper.Pi * 0.75f), 140f, 154f);

        public AresCannonChargeParticleSet EnergyDrawer = new(-1, 15, 40f, Color.Red);

        public ThanatosSmokeParticleSet SmokeDrawer = new(-1, 3, 0f, 16f, 1.5f);

        public Vector2 SlashStart
        {
            get;
            set;
        }

        public PrimitiveTrailCopy SlashDrawer
        {
            get;
            set;
        }

        public bool KatanaIsInUse
        {
            get => katanaIsInUse;
            set
            {
                if (value && !katanaIsInUse)
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound, NPC.Center);

                katanaIsInUse = value;
            }
        }

        public ref float ArmOffsetDirection => ref NPC.ai[2];

        public ref float CurrentDirection => ref NPC.ai[3];

        public ref float SlashFadeOut => ref NPC.localAI[0];

        public static NPC Ares => AresCannonBehaviorOverride.Ares;

        public static float AttackTimer => Ares.ai[1];

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("XF-09 Ares Energy Katana");
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 50;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = 0;
            NPC.width = 170;
            NPC.height = 120;
            NPC.defense = 80;
            NPC.DR_NERD(0.35f);
            NPC.LifeMaxNERB(1250000, 1495000, 500000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.Opacity = 0f;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.netAlways = true;
            NPC.boss = true;
            NPC.hide = true;
            NPC.Calamity().canBreakPlayerDefense = true;
            Music = (InfernumMode.CalamityMod as CalamityModClass).GetMusicFromMusicMod("ExoMechs") ?? MusicID.Boss3;
        }

        public override void AI()
        {
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.active = false;
                return;
            }

            // Update the energy drawers.
            EnergyDrawer.Update();
            SmokeDrawer.Update();

            // Update limbs.
            UpdateLimbs();

            // Close the HP bar.
            NPC.boss = false;
            NPC.Calamity().ShouldCloseHPBar = true;

            // Inherit a bunch of attributes such as opacity from the body.
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(NPC);

            // Ensure this does not take damage in the desperation attack.
            NPC.dontTakeDamage = false;
            if (Ares.ai[0] == (int)AresBodyAttackType.PrecisionBlasts)
                NPC.dontTakeDamage = true;

            bool currentlyDisabled = ArmIsDisabled(NPC);
            Player target = Main.player[NPC.target];

            // Inherit a bunch of attributes such as opacity from the body.
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(NPC);

            AresCannonBehaviorOverride.UpdateParticleDrawers(SmokeDrawer, EnergyDrawer, 0f, 100f);

            // Hover in place below Ares if disabled.
            if (currentlyDisabled)
            {
                ExoMechAIUtilities.PerformAresArmDirectioning(NPC, Ares, target, Vector2.UnitY, currentlyDisabled, false, ref CurrentDirection);
                PerformHoverMovement();
                return;
            }

            switch ((AresBodyAttackType)Ares.ai[0])
            {
                case AresBodyAttackType.EnergyBladeSlices:
                    DoBehavior_EnergyBladeSlices();
                    break;
            }
        }

        public void UpdateLimbs()
        {
            Vector2 connectPosition = Ares.Center + new Vector2(ArmOffsetDirection * 70f, -108f).RotatedBy(Ares.rotation * Ares.spriteDirection);
            Vector2 endPosition = NPC.Center;

            for (int i = 0; i < 12; i++)
            {
                float lockedRotation;
                if (ArmOffsetDirection == 1f)
                    lockedRotation = 0.23f;
                else
                    lockedRotation = MathHelper.Pi - 0.23f;
                Limbs[0].Rotation = MathHelper.Clamp((float)Limbs[0].Rotation, lockedRotation - 0.45f, lockedRotation + 0.45f);

                Limbs.Update(connectPosition, endPosition);
            }
        }

        public void DoBehavior_EnergyBladeSlices()
        {
            int anticipationTime = 58;
            int sliceTime = 16;
            int hoverTime = 8;
            float wrappedAttackTimer = (AttackTimer + (int)ArmOffsetDirection * anticipationTime / 3) % (anticipationTime + sliceTime + hoverTime);
            float flySpeedBoost = Ares.velocity.Length() * 0.4f;

            Vector2 hoverOffset = Vector2.Zero;
            if (wrappedAttackTimer <= anticipationTime)
            {
                SlashFadeOut = 1f;
                float minHoverSpeed = Utils.Remap(wrappedAttackTimer, 7f, anticipationTime * 0.5f, 2f, 42f);
                Vector2 startingOffset = new(ArmOffsetDirection * 470f, 0f);
                Vector2 endingOffset = new(ArmOffsetDirection * 172f, -175f);
                hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(0f, anticipationTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 450f, flySpeedBoost + minHoverSpeed, 115f);
            }
            else if (wrappedAttackTimer <= anticipationTime + sliceTime)
            {
                SlashFadeOut = 0f;
                Vector2 startingOffset = new(ArmOffsetDirection * 172f, -175f);
                Vector2 endingOffset = new(ArmOffsetDirection * -260f, 400f);
                hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(anticipationTime, anticipationTime + sliceTime, wrappedAttackTimer, true));
                ExoMechAIUtilities.DoSnapHoverMovement(NPC, Ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 400f, flySpeedBoost + 49f, 115f);
            }
            else
            {
                NPC.velocity.X *= 0.6f;
                NPC.velocity.Y *= 0.1f;
                SlashFadeOut = MathHelper.Clamp(SlashFadeOut + 0.5f, 0f, 1f);
            }

            // Play a slice sound.
            if (wrappedAttackTimer == anticipationTime)
            {
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
                SlashStart = NPC.Center + ((float)Limbs.Limbs[1].Rotation).ToRotationVector2() * NPC.scale * 160f;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(InfernumSoundRegistry.AresSlashSound, NPC.Center);
            }

            // Rotate based on the direction of the arm.
            NPC.rotation = (float)Limbs[1].Rotation;
            NPC.spriteDirection = (int)ArmOffsetDirection;
            if (ArmOffsetDirection == 1)
                NPC.rotation += MathHelper.Pi;

            // Use the katanas.
            KatanaIsInUse = true;
        }

        public Vector2 PerformHoverMovement()
        {
            Vector2 hoverOffset = new(ArmOffsetDirection * 470f, 0f);
            Vector2 hoverDestination = Ares.Center + hoverOffset;
            ExoMechAIUtilities.DoSnapHoverMovement(NPC, hoverDestination, 64f, 115f);

            return hoverOffset;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override void FindFrame(int frameHeight)
        {
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.soundDelay == 1)
            {
                NPC.soundDelay = 3;
                SoundEngine.PlaySound(CommonCalamitySounds.ExoHitSound, NPC.Center);
            }

            for (int k = 0; k < 3; k++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1f);

            if (NPC.life <= 0)
            {
                for (int i = 0; i < 2; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);

                for (int i = 0; i < 20; i++)
                {
                    Dust exoEnergy = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 0, new Color(0, 255, 255), 2.5f);
                    exoEnergy.noGravity = true;
                    exoEnergy.velocity *= 3f;

                    exoEnergy = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                    exoEnergy.velocity *= 2f;
                    exoEnergy.noGravity = true;
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("AresPulseCannon1").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase1").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase2").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, InfernumMode.CalamityMod.Find<ModGore>("AresHandBase3").Type, NPC.scale);
                }
            }
        }

        public float SlashWidthFunction(float completionRatio) => NPC.scale * 100f;

        public Color SlashColorFunction(float completionRatio) => Color.White * Utils.GetLerpValue(0.9f, 0.4f, completionRatio, true) * (1f - SlashFadeOut) * NPC.Opacity;

        public void DrawSlash()
        {
            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            slashShader.UseColor(new Color(237, 148, 54));
            slashShader.UseSecondaryColor(new Color(104, 24, 38));
            slashShader.Shader.Parameters["fireColor"].SetValue(Color.Wheat.ToVector3());
            slashShader.Shader.Parameters["flipped"].SetValue(false);
            slashShader.Apply();

            Vector2 slashStart = SlashStart;
            Vector2 aimDirection = ((float)Limbs.Limbs[1].Rotation).ToRotationVector2();
            Vector2 slashEnd = NPC.Center + aimDirection * NPC.scale * 160f;
            Vector2 slashMiddle1 = Vector2.Lerp(slashStart, slashEnd, 0.25f);
            Vector2 slashMiddle2 = Vector2.Lerp(slashStart, slashEnd, 0.5f);
            Vector2 slashMiddle3 = Vector2.Lerp(slashStart, slashEnd, 0.75f);
            SlashDrawer.Draw(new List<Vector2>()
            {
                slashEnd,
                slashMiddle3 + aimDirection * 30f,
                slashMiddle2,
                slashMiddle1 - aimDirection * 30f,
                slashStart,
            }, -Main.screenPosition, 20, (float)Limbs.Limbs[1].Rotation + MathHelper.PiOver2);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            AresCannonBehaviorOverride.DrawCannon(NPC, InfernumTextureRegistry.InvisPath, Color.Transparent, drawColor, NPC.Center - Main.screenPosition, EnergyDrawer, SmokeDrawer);

            if (KatanaIsInUse)
            {
                var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
                SlashDrawer ??= new PrimitiveTrailCopy(SlashWidthFunction, SlashColorFunction, null, true, slashShader);

                // Draw the zany slash effect.
                Main.spriteBatch.EnterShaderRegion();

                for (int i = 0; i < 6; i++)
                    DrawSlash();
                Main.spriteBatch.ExitShaderRegion();

                int bladeFrameNumber = (int)((Main.GlobalTimeWrappedHourly * 16f + NPC.whoAmI * 7.13f) % 9f);
                Texture2D bladeTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/DraedonsArsenal/PhaseslayerBlade").Value;
                Rectangle bladeFrame = bladeTexture.Frame(3, 7, bladeFrameNumber / 7, bladeFrameNumber % 7);
                Vector2 bladeOrigin = bladeFrame.Size() * new Vector2(0.5f, 1f);
                Vector2 bladeDrawPosition = NPC.Center - Main.screenPosition - NPC.rotation.ToRotationVector2() * ArmOffsetDirection * 14f;
                Vector2 bladeScale = Vector2.One * NPC.scale;
                float squish = NPC.position.Distance(NPC.oldPosition) * 0.006f;
                bladeScale.X -= squish;

                Main.EntitySpriteDraw(bladeTexture, bladeDrawPosition, bladeFrame, NPC.GetAlpha(Color.White), NPC.rotation - ArmOffsetDirection * MathHelper.PiOver2, bladeOrigin, bladeScale, 0, 0);
            }

            return false;
        }

        public override bool CheckActive() => false;
    }
}
