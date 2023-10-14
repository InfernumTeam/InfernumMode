using CalamityMod;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items.Mounts;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.DataStructures;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Projectiles
{
    public class HyperplaneMatrixProjectile : ModProjectile
    {
        public class MatrixUIIcon
        {
            public string HoverText
            {
                get;
                set;
            }

            public float Scale
            {
                get;
                set;
            } = 1f;

            public Asset<Texture2D> IconTexture
            {
                get;
                set;
            }

            public Action<Player> ClickBehavior
            {
                get;
                set;
            }

            public bool Draw(Player player, Vector2 left, Vector2 right, float indexRatio, float opacity)
            {
                Texture2D background = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/HyperplaneMatrixIconBackground").Value;
                Texture2D icon = IconTexture.Value;

                // Acquire drawing information.
                float scale = Scale * opacity * 0.8f;
                Vector2 iconScale = Vector2.One * scale;
                if ((icon.Size() * iconScale).Length() >= 42f)
                    iconScale *= Vector2.One * 42f / icon.Size();

                Vector2 drawPosition = Vector2.Lerp(left, right, indexRatio);
                drawPosition.Y -= CalamityUtils.Convert01To010(indexRatio) * opacity * 40f;
                Vector2 backgroundOrigin = background.Size() * 0.5f;
                Vector2 iconOrigin = icon.Size() * 0.5f;
                Rectangle drawArea = Utils.CenteredRectangle(drawPosition, background.Size() * scale);

                // Determine if the mouse is hovering over the icon.
                // If it is, it should display the hover text and increase in size.
                bool hoveringOverBackground = drawArea.Contains(Main.MouseScreen.ToPoint());
                Scale = Clamp(Scale + hoveringOverBackground.ToDirectionInt() * 0.04f, 1f, 1.35f);

                // Draw the icon.
                Main.spriteBatch.Draw(background, drawPosition, null, Color.White * opacity, 0f, backgroundOrigin, scale, 0, 0f);
                Main.spriteBatch.Draw(icon, drawPosition, null, Color.White * opacity, 0f, iconOrigin, iconScale, 0, 0f);

                // Draw the text above the icon.
                if (hoveringOverBackground && opacity > 0f)
                {
                    var font = FontAssets.MouseText.Value;
                    Vector2 textArea = font.MeasureString(HoverText);
                    Vector2 textPosition = (left + right) * 0.5f - Vector2.UnitY * 70f;
                    ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, HoverText, textPosition - Vector2.UnitX * textArea * 0.5f, Draedon.TextColor, 0f, textArea * new Vector2(0f, 0.5f), Vector2.One);
                }

                // Handle click behaviors.
                bool clicked = opacity > 0f && Main.mouseLeft && Main.mouseLeftRelease && hoveringOverBackground;
                if (clicked)
                {
                    ClickBehavior?.Invoke(player);
                    return true;
                }
                return false;
            }
        }

        public enum HyperplaneMatrixState : byte
        {
            InitialShake,
            PrepareHologram,
            HandleUIState
        }

        public bool IsRenderingUI
        {
            get;
            set;
        }

        public float UIOptionsOpacity
        {
            get;
            set;
        }

        public HyperplaneMatrixState CurrentState
        {
            get;
            set;
        }

        public PrimitiveTrailCopy HologramRayDrawer
        {
            get;
            set;
        }

        public Player Owner => Main.player[Projectile.owner];

        internal static List<MatrixUIIcon> UIStates;

        public ref float Time => ref Projectile.ai[0];

        public ref float DisappearCountdown => ref Projectile.ai[1];

        public ref float HologramRayWidth => ref Projectile.localAI[0];

        public ref float HologramRayBrightness => ref Projectile.localAI[1];

        public const float MaxHologramWidth = 264f;

        public const float MaxHologramHeight = 304f;

        public override void SetStaticDefaults()
        {
            UIStates = new()
            {
                new()
                {
                    HoverText = Utilities.GetLocalization("UI.HyperplaneMatrixUI.Godmode").Value,
                    IconTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/CyborgImmortalityIcon"),
                    ClickBehavior = (Player player) =>
                    {
                        Referenced<bool> cyberneticImmortality = player.Infernum().GetRefValue<bool>("CyberneticImmortalityIsActive");
                        cyberneticImmortality.Value = !cyberneticImmortality.Value;
                        CalamityUtils.DisplayLocalizedText($"Mods.InfernumMode.Status.CyberneticImmortality{(cyberneticImmortality.Value ? "Enabled" : "Disabled")}", Draedon.TextColor);
                    }
                },

                new()
                {
                    HoverText = Utilities.GetLocalization("UI.HyperplaneMatrixUI.NoClip").Value,
                    IconTexture = ModContent.Request<Texture2D>("CalamityMod/Items/Mounts/ExoThrone"),
                    ClickBehavior = player => player.Infernum().SetValue<bool>("PhysicsDefianceIsEnabled", !player.Infernum().GetValue<bool>("PhysicsDefianceIsEnabled"))
                },

                new()
                {
                    HoverText = Utilities.GetLocalization("UI.HyperplaneMatrixUI.TimeSetMorning").Value,
                    IconTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/SunriseIcon"),
                    ClickBehavior = _ => HyperplaneMatrixTimeChangeSystem.ApproachTime(1, true)
                },

                new()
                {
                    HoverText = Utilities.GetLocalization("UI.HyperplaneMatrixUI.TimeSetNoon").Value,
                    IconTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/NoonIcon"),
                    ClickBehavior = _ => HyperplaneMatrixTimeChangeSystem.ApproachTime(27000, true)
                },

                new()
                {
                    HoverText = Utilities.GetLocalization("UI.HyperplaneMatrixUI.TimeSetDusk").Value,
                    IconTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/SunsetIcon"),
                    ClickBehavior = _ => HyperplaneMatrixTimeChangeSystem.ApproachTime(1, false)
                },

                new()
                {
                    HoverText = Utilities.GetLocalization("UI.HyperplaneMatrixUI.Butcher").Value,
                    IconTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/AtomizeIcon"),
                    ClickBehavior = _ => AtomizeHostileNPCs()
                }
            };

            InfernumPlayer.MovementUpdateEvent += (InfernumPlayer player) =>
            {
                // Remove acceleration when in physics defiance mode.
                if (!player.GetValue<bool>("PhysicsDefianceIsEnabled") || player.Player.grappling[0] >= 0 || player.Player.mount.Active)
                    return;

                // Grant the player infinite flight time.
                player.Player.wingTime = player.Player.wingTimeMax;
                player.Player.legFrame.Y = -player.Player.legFrame.Height;

                float speed = DraedonGamerChairMount.MovementSpeed * 2f;
                if (player.Player.controlLeft)
                {
                    player.Player.velocity.X = -speed;
                    player.Player.direction = -1;
                }
                else if (player.Player.controlRight)
                {
                    player.Player.velocity.X = speed;
                    player.Player.direction = 1;
                }
                else
                    player.Player.velocity.X = 0f;

                if (player.Player.controlUp || player.Player.controlJump)
                    player.Player.velocity.Y = -speed;

                else if (player.Player.controlDown)
                {
                    player.Player.velocity.Y = speed;
                    if (Collision.TileCollision(player.Player.position, player.Player.velocity, player.Player.width, player.Player.height, true, false, (int)player.Player.gravDir).Y == 0f)
                        player.Player.velocity.Y = 0.5f;
                }
                else
                    player.Player.velocity.Y = 0f;
            };
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 7200;
            Projectile.penetrate = -1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)CurrentState);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CurrentState = (HyperplaneMatrixState)reader.ReadByte();
        }

        public override void AI()
        {
            Item heldItem = Owner.ActiveItem();

            // Die if no longer holding the click button or otherwise cannot use the item.
            bool shouldDie = !Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed || heldItem is null;
            if (IsRenderingUI)
            {
                shouldDie = Owner.dead || !Owner.active || heldItem is null;
                if (DisappearCountdown > 0f)
                {
                    HologramRayWidth *= 0.9f;
                    HologramRayBrightness *= 0.95f;
                    UIOptionsOpacity = HologramRayBrightness;
                    DisappearCountdown--;
                    if (DisappearCountdown <= 0f)
                        shouldDie = true;
                }
            }

            if (Main.myPlayer == Projectile.owner && shouldDie)
            {
                Projectile.Kill();
                return;
            }

            // Stick to the owner.
            Projectile.Center = Owner.MountedCenter;
            AdjustPlayerValues();

            switch (CurrentState)
            {
                // Shake in the player's hand for a bit before performing any UI effects.
                case HyperplaneMatrixState.InitialShake:
                    DoBehavior_InitialShake();
                    break;

                // Prepare the hologram and make the UI components appear.
                case HyperplaneMatrixState.PrepareHologram:
                    DoBehavior_PrepareHologram();
                    break;

                // Make the UI appear.
                case HyperplaneMatrixState.HandleUIState:
                    DoBehavior_HandleUIState();
                    break;
            }

            Time++;
        }

        public void DoBehavior_InitialShake()
        {
            int shakeTime = 90;

            // Play an electric sound on the first frame.
            if (Time == 1f)
                SoundEngine.PlaySound(VoltageRegulationSystem.InstallSound);

            // Randomly release small electric sparks.
            if (Main.rand.NextBool(4))
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f), 264);
                spark.color = Color.Cyan;
                spark.velocity = Main.rand.NextVector2Unit() * Main.rand.NextVector2Unit(1f, 4f);
                spark.scale = Main.rand.NextFloat(0.9f, 1.2f);
                spark.noLight = true;
                spark.noGravity = true;
            }

            // Perform the shake effect. The matrix will return to its intended position again on the next frame, giving an illusion of jitter.
            Projectile.Center += Main.rand.NextVector2Unit() * Lerp(0.4f, 3f, Time / shakeTime);

            if (Time <= shakeTime)
                return;

            // Open the UI if the matrix is usable.
            // If it isn't, explode and hurt the owner.
            if (HyperplaneMatrix.CanBeUsed)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.HyperplaneMatrixActivateSound, Owner.Center);
                CurrentState = HyperplaneMatrixState.PrepareHologram;
                Time = 0f;
                Projectile.netUpdate = true;
                return;
            }

            // Emit explosion particles.
            for (int i = 0; i < 25; i++)
            {
                Vector2 firePosition = Projectile.Center + Main.rand.NextVector2Circular(50f, 50f);
                Color fireColor = Color.Lerp(Color.Cyan, Color.Lime, Main.rand.NextFloat(0.3f, 0.7f));
                float fireScale = Main.rand.NextFloat(1.5f, 1.9f);
                float fireRotationSpeed = Main.rand.NextFloat(-0.05f, 0.05f);

                var particle = new HeavySmokeParticle(firePosition, Vector2.Zero, fireColor, 54, fireScale, 1f, fireRotationSpeed, true, 0f, true);
                GeneralParticleHandler.SpawnParticle(particle);
            }

            // Hurt the player.
            var hurtReason = PlayerDeathReason.ByCustomReason(Utilities.GetLocalization("Status.Death.HyperplaneMatrixExplosion").Format(Owner.name));
            Owner.Hurt(hurtReason, HyperplaneMatrix.UnableToBeUsedHurtDamage, 0);

            // Destroy the matrix.
            SoundEngine.PlaySound(SoundID.NPCDeath56, Owner.Center);
            Projectile.Kill();
        }

        public void DoBehavior_PrepareHologram()
        {
            // Use the UI effect.
            IsRenderingUI = true;

            // Make the hologram appear.
            HologramRayBrightness = Clamp(HologramRayBrightness + 0.03f, 0f, 1f);
            HologramRayWidth = Clamp(HologramRayWidth + 4f, 1f, MaxHologramWidth);

            // Prepare the UI once the hologram is sufficiently large.
            if (HologramRayWidth >= MaxHologramWidth)
            {
                CurrentState = HyperplaneMatrixState.HandleUIState;
                Time = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_HandleUIState()
        {
            // Use the UI effect.
            IsRenderingUI = true;
            UIOptionsOpacity = Clamp(UIOptionsOpacity + 0.045f, 0f, 1f);
        }

        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = 0f;

            Projectile.spriteDirection = Owner.direction;
            Projectile.velocity = Vector2.UnitX * Owner.direction;
            Projectile.Center += Projectile.velocity * 10f;

            // Update the player's arm directions to make it look as though they're holding the matrix.
            float frontArmRotation = Owner.direction * -0.9f;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public float RayWidthFunction(float completionRatio)
        {
            float widthOffset = Cos(completionRatio * 73f - Main.GlobalTimeWrappedHourly * 8f) *
                Utils.GetLerpValue(0f, 0.1f, completionRatio, true) *
                Utils.GetLerpValue(1f, 0.9f, completionRatio, true);
            return Lerp(2f, HologramRayWidth + 3f, completionRatio) + widthOffset;
        }

        public static Color RayColorFunction(float completionRatio)
        {
            float opacity = Filters.Scene["InfernumMode:ScreenSaturationBlur"].IsActive() ? 0.1f : 0.3f;
            return Color.Cyan * Utils.GetLerpValue(0.8f, 0.5f, completionRatio, true) * opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Just draw the matrix as usual without the hologram or UI effect if the calling client isn't the one using the item, since they shouldn't be able to interact
            // with someone else's UI.
            if (Main.myPlayer != Projectile.owner)
                return true;

            DrawUI();
            DrawHologramRay();
            return true;
        }

        public void DrawHologramRay()
        {
            // Initialize and draw the hologram ray.
            HologramRayDrawer ??= new(RayWidthFunction, RayColorFunction, null, false, InfernumEffectsRegistry.ScrollingCodePrimShader);

            // Don't try to draw if the size of the hologram is negligible, for performance reasons.
            float length = HologramRayBrightness * MaxHologramHeight;
            if (length <= 1f)
                return;

            Vector2 rayStartingPoint = Projectile.Center;
            List<Vector2> points = new();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint - Vector2.UnitY * length, i / 12f));

            InfernumEffectsRegistry.ScrollingCodePrimShader.SetShaderTexture(InfernumTextureRegistry.HyperplaneMatrixCode);
            Main.instance.GraphicsDevice.Textures[4] = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/TechyNoise").Value;
            HologramRayDrawer.Draw(points, -Main.screenPosition, 47);
        }

        public void DrawUI()
        {
            if (!IsRenderingUI || UIOptionsOpacity <= 0f)
                return;

            float opacity = Projectile.Opacity * HologramRayBrightness;
            Vector2 left = Projectile.Center - Main.screenPosition + new Vector2(MaxHologramWidth * -0.45f, -MaxHologramHeight + 90f) * opacity;
            Vector2 right = Projectile.Center - Main.screenPosition + new Vector2(MaxHologramWidth * 0.45f, -MaxHologramHeight + 90f) * opacity;
            for (int i = 0; i < UIStates.Count; i++)
            {
                float indexCompletion = i / (float)(UIStates.Count - 1f);
                if (UIStates.Count <= 1)
                    indexCompletion = 0.5f;

                if (DisappearCountdown == 0f && UIStates[i].Draw(Owner, left, right, indexCompletion, opacity))
                {
                    SoundEngine.PlaySound(DecryptionComputer.InstallSound);
                    break;
                }
            }

            if (Main.mouseLeft && Main.mouseLeftRelease)
                DisappearCountdown = 36f;
        }

        public static void AtomizeHostileNPCs()
        {
            List<int> enemiesToNotAtomize = new()
            {
                NPCID.TargetDummy,
                NPCID.CultistDevote,
                NPCID.CultistArcherBlue,
                NPCID.CultistTablet,
                NPCID.DD2LanePortal,
                NPCID.DD2EterniaCrystal,
                ModContent.NPCType<Draedon>(),
                ModContent.NPCType<Eidolist>(),
            };

            int killedNPCs = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || enemiesToNotAtomize.Contains(n.type))
                    continue;

                if (n.friendly || n.townNPC)
                    continue;

                n.HitSound = null;
                n.DeathSound = SoundID.NPCDeath55;
                n.defense = 0;

                // Sometimes worm segments can spam the "X has been defeated!" text as a result of the above stuff.
                // To mitigate this, the boss status is stripped if the NPC inherits HP from something else.
                if (n.realLife >= 0)
                    n.boss = false;

                n.Calamity().DR = 0f;
                n.Calamity().unbreakableDR = false;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    n.StrikeInstantKill();
                n.boss = false;

                // If for some reason the NPC is not immediately dead, kill it manually. This mitigates death animations entirely.
                if (n.active)
                {
                    n.life = 0;
                    n.HitEffect();
                    n.NPCLoot();
                    n.active = false;
                }
                killedNPCs++;

                // Create a bunch of electric plasma at the NPC's position if near players.
                Player closest = Main.player[Player.FindClosest(n.Center, 1, 1)];
                if (!n.WithinRange(closest.Center, 5000f))
                    continue;

                for (int j = 0; j < 16; j++)
                {
                    ElectricCloudParticle electricPlasma = new(n.Center, Main.rand.NextVector2Circular(12f, 12f), 120, 3f);
                    GeneralParticleHandler.SpawnParticle(electricPlasma);
                }

                for (int j = 0; j < 20; j++)
                {
                    Color electricColor = Color.Lerp(Color.Cyan, Color.ForestGreen, Main.rand.NextFloat(0.6f));
                    HeavySmokeParticle electricPlasma = new(n.Center, Main.rand.NextVector2Circular(8f, 8f), electricColor, 56, 1.3f, 1f, 0f, true, 0.002f);
                    GeneralParticleHandler.SpawnParticle(electricPlasma);
                }
            }

            if (killedNPCs >= 1)
                SoundEngine.PlaySound(InfernumSoundRegistry.SonicBoomSound, Main.LocalPlayer.Center);
        }

        public override bool? CanDamage() => false;
    }
}
