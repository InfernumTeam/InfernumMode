using CalamityMod;
using CalamityMod.NPCs.ExoMechs;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Projectiles
{
    public class SoulDrivenHeadphonesProj : ModProjectile
    {
        public class MusicUIIcon
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

            public string TrackName
            {
                get;
                set;
            }

            public Asset<Texture2D> BossIconTexture
            {
                get;
                set;
            }

            public Func<Color> HoverTextColor
            {
                get;
                set;
            }

            public Func<bool> UnlockCondition
            {
                get;
                set;
            }

            public bool Draw(Player player, Vector2 center, float indexRatio, float opacity, out Vector2 textDrawPosition, out Color textColor, out string text)
            {
                Texture2D background = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/HeadphoneIconBackground").Value;
                Texture2D icon = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/MusicIcons").Value;

                // Acquire drawing information.
                bool unlockedTrack = UnlockCondition() || player.Infernum_Music().UnlockAllMusic;
                float scale = Scale * opacity * 0.8f;
                float indexAngle = MathHelper.TwoPi * indexRatio - MathHelper.PiOver2;
                Vector2 drawPosition = center + indexAngle.ToRotationVector2() * 90f;
                Vector2 backgroundOrigin = background.Size() * 0.5f;
                Rectangle iconFrame = icon.Frame(1, 3, 0, (int)(indexRatio * 10f) % 3);
                Vector2 iconOrigin = iconFrame.Size() * 0.5f;
                Color iconColor = Main.hslToRgb((indexRatio + Main.GlobalTimeWrappedHourly * 0.33f) % 1f, 1f, 0.5f);
                Rectangle drawArea = Utils.CenteredRectangle(drawPosition, background.Size() * scale);

                // Determine if the mouse is hovering over the icon.
                // If it is, it should display the hover text and increase in size.
                bool hoveringOverBackground = drawArea.Contains(Main.MouseScreen.ToPoint());
                Scale = MathHelper.Clamp(Scale + hoveringOverBackground.ToDirectionInt() * 0.04f, 1f, 1.35f);

                // Draw the icon.
                Main.spriteBatch.Draw(background, drawPosition, null, Color.White * opacity, 0f, backgroundOrigin, scale, 0, 0f);
                Vector2 headIconScale = Vector2.One * 30f / BossIconTexture.Value.Size() * scale;
                Main.spriteBatch.Draw(BossIconTexture.Value, drawPosition, null, (unlockedTrack ? Color.White : Color.Black) * opacity, 0f, BossIconTexture.Value.Size() * 0.5f, headIconScale, 0, 0f);

                if (unlockedTrack)
                {
                    Main.spriteBatch.SetBlendState(BlendState.NonPremultiplied);
                    Main.spriteBatch.Draw(icon, drawPosition - indexAngle.ToRotationVector2() * 16f, iconFrame, iconColor * opacity, 0f, iconOrigin, scale, 0, 0f);
                    Main.spriteBatch.ResetBlendState();
                }

                // Draw the text above the icon.
                text = string.Empty;
                textColor = Color.White;
                textDrawPosition = Vector2.Zero;
                if (hoveringOverBackground && opacity > 0f)
                {
                    text = HoverText;
                    textColor = HoverTextColor();
                    if (!unlockedTrack)
                        textColor = Color.Gray;

                    textDrawPosition = drawPosition + Vector2.UnitY * 44f;
                }

                // Handle click behaviors.
                bool clicked = opacity > 0f && Main.mouseLeft && Main.mouseLeftRelease && hoveringOverBackground;
                if (hoveringOverBackground)
                    Main.blockMouse = true;

                if (clicked && unlockedTrack)
                {
                    player.Infernum_Music().CurrentTrackName = TrackName;
                    return true;
                }
                return false;
            }
        }

        public enum HeadphonesMatrixState : byte
        {
            PutOn,
            TakeOff,
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

        public HeadphonesMatrixState CurrentState
        {
            get;
            set;
        }

        public Player Owner => Main.player[Projectile.owner];

        public List<MusicUIIcon> UIStates = new()
        {
            new()
            {
                HoverText = "Storm After Rain - Infernum",
                TrackName = "TitleScreen",
                HoverTextColor = () =>
                {
                    float colorInterpolant = (float)(Math.Sin(MathHelper.Pi * Main.GlobalTimeWrappedHourly + 1f) * 0.5) + 0.5f;
                    return CalamityUtils.MulticolorLerp(colorInterpolant, new Color(170, 0, 0, 255), Color.OrangeRed, new Color(255, 200, 0, 255));
                },
                UnlockCondition = () => true,
                BossIconTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Items/SoulDrivenHeadphones")
            },

            new()
            {
                HoverText = "Gelatinous Dynasty - King Slime",
                TrackName = "KingSlime",
                HoverTextColor = () => Color.Lerp(Color.SlateBlue, Color.LightGray, 0.2f),
                UnlockCondition = () => NPC.downedSlimeKing,
                BossIconTexture = TextureAssets.NpcHeadBoss[7]
            },

            new()
            {
                HoverText = "Seer of the Night - The Eye of Cthulhu",
                TrackName = "EyeOfCthulhu",
                HoverTextColor = () => Color.Lerp(Color.DarkRed, Color.Gray, 0.4f),
                UnlockCondition = () => NPC.downedBoss1,
                BossIconTexture = TextureAssets.NpcHeadBoss[1]
            },

            new()
            {
                HoverText = "Maw of the Corruption - The Eater of Worlds",
                TrackName = "EaterOfWorlds",
                HoverTextColor = () => CalamityUtils.ColorSwap(Color.MediumPurple, Color.Lime, 4f),
                UnlockCondition = () => NPC.downedBoss2,
                BossIconTexture = TextureAssets.NpcHeadBoss[2]
            },

            new()
            {
                HoverText = "Intellect of the Crimson - The Brain of Cthulhu",
                TrackName = "BrainOfCthulhu",
                HoverTextColor = () => CalamityUtils.ColorSwap(Color.IndianRed, Color.Yellow, 2f),
                UnlockCondition = () => NPC.downedBoss2,
                BossIconTexture = TextureAssets.NpcHeadBoss[23]
            },

            new()
            {
                HoverText = "Royal Retaliation - The Queen Bee",
                TrackName = "QueenBee",
                HoverTextColor = () => CalamityUtils.ColorSwap(Color.Black, Color.Yellow, 1f),
                UnlockCondition = () => NPC.downedQueenBee,
                BossIconTexture = TextureAssets.NpcHeadBoss[14]
            },

            new()
            {
                HoverText = "Warden of the Damned - Skeletron",
                TrackName = "Boss3",
                HoverTextColor = () => CalamityUtils.ColorSwap(Color.MediumPurple, Color.HotPink, 2f),
                UnlockCondition = () => NPC.downedBoss3,
                BossIconTexture = TextureAssets.NpcHeadBoss[19]
            },

            new()
            {
                HoverText = "Inferior Fabrications - The Mechanical Trio",
                TrackName = "MechBosses",
                HoverTextColor = () => CalamityUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.36f % 1f, Color.Orange, Color.Red, Color.RoyalBlue),
                UnlockCondition = () => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
                BossIconTexture = TextureAssets.NpcHeadBoss[25]
            },

            new()
            {
                HoverText = "Floral Animosity - Plantera",
                TrackName = "Plantera",
                HoverTextColor = () => CalamityUtils.ColorSwap(Color.Pink, Color.Lime, 2.67f),
                UnlockCondition = () => NPC.downedPlantBoss,
                BossIconTexture = TextureAssets.NpcHeadBoss[12]
            },

            new()
            {
                HoverText = "Razorblade Typhoon - Duke Fishron",
                TrackName = "DukeFishron",
                HoverTextColor = () => Color.Turquoise,
                UnlockCondition = () => NPC.downedFishron,
                BossIconTexture = TextureAssets.NpcHeadBoss[4]
            },

            new()
            {
                HoverText = "Shining Kaleidoscope - The Empress of Light",
                TrackName = "EmpressOfLight",
                HoverTextColor = () => Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.4f % 1f, 0.9f, 0.55f),
                UnlockCondition = () => NPC.downedEmpressOfLight,
                BossIconTexture = TextureAssets.NpcHeadBoss[37]
            },

            new()
            {
                HoverText = "Eidolic Ancestry - The Lunatic Cultist",
                TrackName = "LunaticCultist",
                HoverTextColor = () => CalamityUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.36f % 1f, Color.Orange, Color.MediumPurple, Color.Turquoise, Color.SkyBlue),
                UnlockCondition = () => NPC.downedAncientCultist,
                BossIconTexture = TextureAssets.NpcHeadBoss[31]
            },

            new()
            {
                HoverText = "Duel for a Lost Kingdom - The Bereft Vassal",
                TrackName = "BereftVassal",
                HoverTextColor = () => Color.Lerp(Color.Cyan, Color.Yellow, (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.3f + 0.35f),
                UnlockCondition = () => WorldSaveSystem.DownedBereftVassal,
                BossIconTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassal_Head_Boss")
            },

            new()
            {
                HoverText = "The End of an Olden Era - The Moon Lord",
                TrackName = "MoonLord",
                HoverTextColor = () => CalamityUtils.ColorSwap(Color.Gray, Color.DarkCyan, 1.25f),
                UnlockCondition = () => NPC.downedMoonlord,
                BossIconTexture = TextureAssets.NpcHeadBoss[8]
            },

            new()
            {
                HoverText = "Catastrophic Fabrications - The Exo Mechs",
                TrackName = "ExoMechBosses",
                HoverTextColor = () => CalamityUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.32f % 1f, CalamityUtils.ExoPalette),
                UnlockCondition = () => DownedBossSystem.downedExoMechs,
                BossIconTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBody_Head_Boss")
            },

            new()
            {
                HoverText = "Their Fabricator - Draedon",
                TrackName = "Draedon",
                HoverTextColor = () => Draedon.TextColor,
                UnlockCondition = () => DownedBossSystem.downedExoMechs,
                BossIconTexture = ModContent.Request<Texture2D>("CalamityMod/Items/Armor/Vanity/DraedonMask")
            },
        };

        public ref float Time => ref Projectile.ai[0];

        public ref float DisappearCountdown => ref Projectile.ai[1];

        public const float MaxHologramWidth = 226f;

        public const float MaxHologramHeight = 272f;

        public override string Texture => "InfernumMode/Content/Items/SoulDrivenHeadphones_Head";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Soul-Driven Headphones");

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
            writer.Write(Projectile.Opacity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CurrentState = (HeadphonesMatrixState)reader.ReadByte();
            Projectile.Opacity = reader.ReadSingle();
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
                // Put the headphones on.
                case HeadphonesMatrixState.PutOn:
                    if (Owner.Infernum_Music().UsingHeadphones)
                        CurrentState = HeadphonesMatrixState.TakeOff;
                    DoBehavior_PutOnHeadphones();
                    break;

                // Take the headphones off.
                case HeadphonesMatrixState.TakeOff:
                    DoBehavior_TakeOffHeadphones();
                    break;

                // Make the UI appear.
                case HeadphonesMatrixState.HandleUIState:
                    DoBehavior_HandleUIState();
                    break;
            }

            Time++;
        }

        public void DoBehavior_PutOnHeadphones()
        {
            // Make the owner put the headphones on.
            float animationCompletion = (float)Math.Pow(Utils.GetLerpValue(0f, 40f, Time, true), 0.56);

            // Update the player's arm directions to make it look as though they're holding the headphones.
            float frontArmRotation = Owner.direction * animationCompletion * -MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation - Owner.direction * 1.72f);
            Projectile.Center = Owner.Center + Vector2.UnitX.RotatedBy(frontArmRotation) * Owner.direction * 16f;

            if (animationCompletion >= 1f)
            {
                CurrentState = HeadphonesMatrixState.HandleUIState;
                Owner.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
                Owner.Infernum_Music().UsingHeadphones = true;
                Projectile.Opacity = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_TakeOffHeadphones()
        {
            // Make the owner put the headphones on.
            float animationCompletion = (float)Math.Pow(Utils.GetLerpValue(0f, 55f, Time, true), 0.72);

            // Update the player's arm directions to make it look as though they're holding the headphones.
            float frontArmRotation = Owner.direction * (1f - animationCompletion) * -MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation - Owner.direction * 1.72f);
            Projectile.Center = Owner.Center + Vector2.UnitX.RotatedBy(frontArmRotation) * Owner.direction * 16f;

            Owner.Infernum_Music().UsingHeadphones = false;
            if (animationCompletion >= 1f)
                Projectile.Kill();
        }

        public void DoBehavior_HandleUIState()
        {
            // Use the UI effect.
            IsRenderingUI = true;
            UIOptionsOpacity = MathHelper.Clamp(UIOptionsOpacity + 0.045f, 0f, 1f);
        }

        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;            
            Projectile.spriteDirection = Owner.direction;
            Projectile.Center = Owner.Center;
            Owner.heldProj = Projectile.whoAmI;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Just draw the matrix as usual without the hologram or UI effect if the calling client isn't the one using the item, since they shouldn't be able to interact
            // with someone else's UI.
            if (Main.myPlayer != Projectile.owner)
                return true;

            DrawUI();
            return true;
        }

        public void DrawUI()
        {
            if (!IsRenderingUI || UIOptionsOpacity <= 0f)
                return;

            float opacity = UIOptionsOpacity;
            if (DisappearCountdown >= 1f)
                opacity *= Utils.GetLerpValue(1f, 36f, DisappearCountdown, true);

            string text = string.Empty;
            Color textColor = Color.Transparent;
            Vector2 textDrawPosition = Vector2.Zero;
            for (int i = 0; i < UIStates.Count; i++)
            {
                float indexCompletion = i / (float)UIStates.Count;
                if (UIStates.Count <= 1)
                    indexCompletion = 0.5f;

                bool clicked = UIStates[i].Draw(Owner, Projectile.Center - Vector2.UnitY * 150f - Main.screenPosition, indexCompletion, opacity, out Vector2 localTextDrawPosition, out Color localTextColor, out string localText) && opacity >= 1f;
                if (!string.IsNullOrEmpty(localText) && opacity >= 1f)
                {
                    text = localText;
                    textColor = localTextColor;
                    textDrawPosition = localTextDrawPosition;
                }

                if (DisappearCountdown == 0f && clicked)
                {
                    SoundEngine.PlaySound(SoundID.Item129);
                    Main.blockMouse = false;
                    break;
                }
            }

            // Draw text above everything else if necessary.
            if (!string.IsNullOrEmpty(text))
            {
                var font = FontAssets.MouseText.Value;
                Vector2 textArea = font.MeasureString(text);
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, textDrawPosition - Vector2.UnitX * textArea * 0.5f, textColor, 0f, textArea * new Vector2(0f, 0.5f), Vector2.One);
            }

            if (Main.mouseLeft && Main.mouseLeftRelease && !Main.blockMouse)
                DisappearCountdown = 36f;
        }

        public override bool? CanDamage() => false;
    }
}
