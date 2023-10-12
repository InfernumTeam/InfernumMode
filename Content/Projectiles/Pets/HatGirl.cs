using InfernumMode.Assets.Sounds;
using InfernumMode.Core;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Pets
{
    public class HatGirl : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public bool HoleBelow
        {
            get
            {
                int tileWidth = 5;
                int tileX = (int)(Projectile.Center.X / 16f) - tileWidth;
                if (Projectile.velocity.X > 0f)
                    tileX += tileWidth;

                int tileY = (int)(Projectile.Bottom.Y / 16f);
                for (int y = tileY; y < tileY + 2; y++)
                {
                    for (int x = tileX; x < tileX + tileWidth; x++)
                    {
                        if (Main.tile[x, y].HasTile)
                            return false;
                    }
                }
                return true;
            }
        }

        public bool WalkingNearOwner
        {
            get => Projectile.ai[0] == 0f;
            set => Projectile.ai[0] = value ? 0f : 1f;
        }

        public bool SaidSillyVALine
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        public ref float TalkAnimationCounter => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 11;
            Main.projPet[Type] = true;

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) =>
            {
                player.SetValue<bool>("HatGirl", false);
            };

            InfernumPlayer.KillEvent += (InfernumPlayer player, double damage, int hitDirection, bool pvp, Terraria.DataStructures.PlayerDeathReason damageSource) =>
            {
                TipsManager.PotentialTipToUse = TipsManager.SelectTip();
            };
        }


        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 58;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.netImportant = true;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = !Owner.WithinRange(Projectile.Center, 200f);
            return true;
        }

        public static void SayThingWhileOwnerIsAlive(Player owner, string key)
        {
            if (owner.dead || !owner.active || owner.ownedProjectileCounts[ModContent.ProjectileType<HatGirl>()] <= 0)
                return;

            string text = Language.GetTextValue(key);
            if (TipsManager.SaidText.Contains(text))
                return;

            foreach (Projectile pet in Utilities.AllProjectilesByID(ModContent.ProjectileType<HatGirl>()))
            {
                if (pet.owner != owner.whoAmI)
                    continue;

                pet.Center = owner.Center;
                pet.netUpdate = true;
            }

            owner.Infernum().SetValue<bool>("ShouldDisplayTips", true);
            TipsManager.PotentialTipToUse = text;
            TipsManager.SaidText.Add(text);
        }

        public override void AI()
        {
            // Die if the owner is no longer present.
            if (!Owner.active)
            {
                Projectile.active = false;
                return;
            }
            InfernumPlayer modPlayer = Owner.Infernum();
            if (Owner.dead)
            {
                modPlayer.SetValue<bool>("HatGirl", false);
                modPlayer.SetValue<bool>("ShouldDisplayTips", false);
            }
            if (modPlayer.GetValue<bool>("HatGirl"))
                Projectile.timeLeft = 2;

            // Give some advice if the player died to a boss.
            if (Main.myPlayer == Projectile.owner && modPlayer.GetValue<bool>("ShouldDisplayTips") && !Owner.dead)
            {
                string tipText = TipsManager.PotentialTipToUse;
                Color messageColor = Color.DeepPink;
                if (!string.IsNullOrEmpty(tipText))
                {
                    if (InfernumConfig.Instance.BossIntroductionAnimationsAreAllowed)
                        Main.NewText(tipText, messageColor);
                    else
                    {
                        int textIndex = CombatText.NewText(Projectile.Hitbox, messageColor, tipText, true);
                        if (textIndex >= 100)
                            textIndex = 99;

                        Main.combatText[textIndex].position = Projectile.Center;
                        Main.combatText[textIndex].crit = true;
                        Main.combatText[textIndex].text = tipText;
                        Main.combatText[textIndex].color = messageColor;
                        Main.combatText[textIndex].lifeTime *= 3;
                    }

                    modPlayer.SetValue<bool>("ShouldDisplayTips", false);
                    TalkAnimationCounter = 1f;

                    TipsManager.SaidText.Add(tipText);
                }

                // Sometimes play a cute VA sound.
                else if (!SaidSillyVALine)
                {
                    if (Main.rand.NextBool(10))
                    {
                        CombatText.NewText(Projectile.Hitbox, messageColor, "Peck!", true);
                        SoundEngine.PlaySound(InfernumSoundRegistry.HatGirlPeckVASound with { Volume = 0.6f }, Projectile.Center);
                    }
                    SaidSillyVALine = true;
                }
            }

            // Reset things.
            Projectile.tileCollide = true;

            // Determine direction.
            if (Math.Abs(Projectile.velocity.X) > 0.25f)
                Projectile.spriteDirection = -Math.Sign(Projectile.velocity.X);

            if (WalkingNearOwner)
                DoBehavior_WalkNearOwner();
            else
                DoBehavior_FlyToOwner();

            Projectile.frameCounter++;
            Projectile.gfxOffY = 4;
        }

        public void DoBehavior_WalkNearOwner()
        {
            bool onSolidGround = Projectile.velocity.Y == 0f;
            float distanceFromOwner = Projectile.Distance(Owner.Center);

            // Reset rotation.
            Projectile.rotation = 0f;

            // Jump if a bit far from the owner and standing still or if there's a hole at the current position.
            if (onSolidGround && (HoleBelow || distanceFromOwner > 350f && Projectile.position.X == Projectile.oldPosition.X || Owner.Center.Y < Projectile.Center.Y - 180f))
            {
                Projectile.velocity.Y = -12f;
                Projectile.netUpdate = true;
            }

            // Decide frames.
            if (!onSolidGround)
            {
                if (Projectile.frame is <= 0 or >= 3)
                    Projectile.frame = 1;
                if (Projectile.frameCounter >= 5)
                {
                    Projectile.frame = 2;
                    Projectile.frameCounter = 0;
                }
            }

            // Walk.
            else if (Math.Abs(Projectile.velocity.X) > 2.4f)
                Projectile.frame = Projectile.frameCounter / 4 % 2 + 5;

            // Get comfortable.
            else
            {
                if (Projectile.frame is <= 2 or >= 5)
                    Projectile.frame = 3;
                if (Projectile.frameCounter >= 5)
                {
                    Projectile.frame = 4;
                    Projectile.frameCounter = 0;
                }
            }

            // Do the talk animation.
            if (TalkAnimationCounter >= 1f)
            {
                TalkAnimationCounter++;

                Projectile.frame = (int)Math.Round(Lerp(7f, 10f, 1f - TalkAnimationCounter / 50f));
                if (TalkAnimationCounter >= 50f)
                    TalkAnimationCounter = 0f;
            }

            // Be affected by gravity.
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.3f, -24f, 12f);

            // Attempt to approach the owner if not close.
            if (distanceFromOwner > 120f)
            {
                float horizontalAcceleration = (Owner.Center.X > Projectile.Center.X).ToDirectionInt() * 0.15f;
                Projectile.velocity.X = Clamp(Projectile.velocity.X + horizontalAcceleration, -8.4f, 8.4f);
                if (distanceFromOwner >= 400f)
                {
                    WalkingNearOwner = false;
                    Projectile.netUpdate = true;
                }

                return;
            }

            // Otherwise slow down.
            Projectile.velocity.X *= 0.85f;
        }

        public void DoBehavior_FlyToOwner()
        {
            // Phase through blocks.
            Projectile.tileCollide = false;

            bool touchingTiles = Collision.SolidCollision(Projectile.TopLeft, Projectile.width, Projectile.height);

            // Use floaty frames.
            Projectile.frame = 2;

            // Determine rotation.
            Projectile.rotation = Clamp(Projectile.velocity.X * 0.03f, -0.15f, 0.15f);

            // Move towards the owner.
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * 23f, 0.04f);

            if (Projectile.WithinRange(Owner.Center, 125f) && touchingTiles)
                Projectile.velocity.Y -= 2f;

            Projectile.Center = Owner.Center + (Projectile.Center - Owner.Center).ClampMagnitude(0f, 1900f);

            // Return to ground.
            if (!touchingTiles && Projectile.WithinRange(Owner.Center, 150f))
            {
                WalkingNearOwner = true;
                Projectile.netUpdate = true;
            }
        }
    }
}
