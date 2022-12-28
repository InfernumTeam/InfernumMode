using CalamityMod;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.Particles;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.CloudElemental
{
    public class CloudElementalBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ThiccWaifu>();

        public enum AttackTypes
        {
            SpawnEffects,
            LightningShotgun,
            IceBulletHell,
            HailChunks
        }

        public const int BaseContactDamage = 60;

        public const float PhaseTwoLifeRatio = 0.5f;

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Pick a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Make immune to knockback
            npc.knockBackResist = 0;
            npc.damage = BaseContactDamage;
            

            // Set variables.
            ref float attackTimer = ref npc.ai[0];
            ref float currentAttack = ref npc.ai[1];
            float lifeRatio = (float)npc.life / npc.lifeMax;
            bool phase2 = lifeRatio <= PhaseTwoLifeRatio;

            // Perform the current attack.
            switch ((AttackTypes)currentAttack)
            {
                case AttackTypes.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;
                case AttackTypes.LightningShotgun:
                    DoBehavior_LightingShotgun(npc, target, ref attackTimer, phase2);
                    break;
                case AttackTypes.IceBulletHell:
                    DoBehavior_IceBulletHell(npc, target, ref attackTimer);
                        break;
                case AttackTypes.HailChunks:
                    DoBehavior_HailChunks(npc, target, ref attackTimer, phase2);
                    break;
            }

            // Increment the attack timer and return.
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float attackTimer)
        {
            float attackLength = 120;
            float fadeLength = 10;

            // On the first frame, pick the location to teleport to and move there.
            if (attackTimer is 0)
            {
                npc.Center = target.Center + new Vector2(0, -400);
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
                npc.lifeMax = 18000;
                npc.life = npc.lifeMax;
            }

            // Create visuals at the teleport position
            CreateTeleportVisuals(npc.Center);

            // Become invisible and do not take or deal damage.
            npc.Opacity = 0;
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Fade in shortly before appearing.
            if (attackTimer >= attackLength - fadeLength)
            {
                float interpolant = (attackTimer - (attackLength - fadeLength)) / (attackLength - fadeLength);
                npc.Opacity = MathHelper.Lerp(0, 1, interpolant);
            }

            // Take/deal damage again and select a new attack.
            if (attackTimer >= attackLength)
            {
                npc.Opacity = 1;
                npc.dontTakeDamage = false;
                npc.damage = BaseContactDamage;
                npc.ai[0] = 0;
                npc.ai[1] = Main.rand.Next(2, 4);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_LightingShotgun(NPC npc, Player target, ref float attackTimer, bool phase2)
        {
            float attackLength = 120;
            float initialDelay = 40;
            float fadeTime = 20;
            float teleportDistance = 400;
            float lightningAmount = 6;
            float lightningSoundDelay = 30;
            float totalShotsToDo = phase2 ? 2 : 1;
            ref float doneShots = ref npc.Infernum().ExtraAI[0];
            
            // Set the position at the beginning of the attack.
            if (attackTimer == fadeTime)
            {
                npc.Center = target.Center + (Vector2.One * teleportDistance).RotatedByRandom(MathHelper.TwoPi);
                npc.velocity = Vector2.Zero;

                // Sync the randomness.
                npc.netUpdate = true;
            }
           
            if (attackTimer < initialDelay)
            {
                CreateTeleportVisuals(npc.Center);

                // Fade out rapidly initially
                if (attackTimer <= fadeTime)
                {
                    float interpolant = attackTimer / fadeTime;
                    npc.Opacity = MathHelper.Lerp(1, 0, interpolant);
                }
                // And fade back in.
                else if (attackTimer >= initialDelay - fadeTime)
                {
                    float interpolant = (attackTimer - (initialDelay - fadeTime)) / (initialDelay - fadeTime);
                    npc.Opacity = MathHelper.Lerp(0, 1, interpolant);
                }
            }

            if (attackTimer == initialDelay && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Get the correct velocity for the first lightning.
                float baseRotation = 2;
                Vector2 baseVelocity = npc.Center.DirectionTo(target.Center).RotatedBy(-baseRotation * 0.5f);

                // Fire the lightning along a spread.
                for (int i = 0; i <= lightningAmount; i++)
                {
                    Vector2 velocity = baseVelocity.RotatedBy(baseRotation * (i / lightningAmount));
                    Utilities.NewProjectileBetter(npc.Center, velocity, ModContent.ProjectileType<CloudLightning>(), 150, 0);
                }
            }

            if (attackTimer == initialDelay + lightningSoundDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);

            if (attackTimer >= attackLength)
            {
                if (doneShots < totalShotsToDo - 1)
                {
                    attackTimer = 0;
                    doneShots++;
                }
                else
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_IceBulletHell(NPC npc, Player target, ref float attackTimer)
        {
            float attackLength = 380;
            float telegraphTime = 120;
            float fadeTime = 20;

            // Rapidly come to a halt.
            npc.velocity *= 0.9f;

            // Fade out rapidly initially
            if (attackTimer <= fadeTime)
            {
                float interpolant = attackTimer / fadeTime;
                npc.Opacity = MathHelper.Lerp(1, 0, interpolant);
            }

            if (attackTimer == fadeTime && Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(target.Center + new Vector2(0, -400), Vector2.Zero, ModContent.ProjectileType<LargeCloud>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);

            if (npc.Opacity <= 0)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
            }
            else
                npc.dontTakeDamage = false;

            if (attackTimer >= attackLength - telegraphTime)
            {
                if (attackTimer == attackLength - telegraphTime)
                    npc.Center = target.Center + new Vector2(0, -350);

                CreateTeleportVisuals(npc.Center);
                
                if (attackTimer >= attackLength - fadeTime)
                {
                    float interpolant = (attackTimer - (attackLength - fadeTime)) / attackLength;
                    npc.Opacity = MathHelper.Lerp(0, 1, interpolant);
                }
            }

            if (attackTimer >= attackLength)
            {
                npc.damage = BaseContactDamage;
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_HailChunks(NPC npc, Player target, ref float attackTimer, bool phase2)
        {
            float attackLength = 180f;
            float hailSpawnDelay = 60f;
            int hailAmount = phase2 ? 6 : 4;
            float hailSpeed = phase2 ? 13f : 9f;
            float fadeTime = 20;

            // Float towards the player.
            float amountToMove = MathHelper.Lerp(0.35f, 0.55f, attackTimer / attackLength);
            npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * 20 * amountToMove, amountToMove);

            // Shoot out 4 large hails.
            if ((attackTimer == hailSpawnDelay || attackTimer == (int)(hailSpawnDelay * 2.5f)) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float angleOffset = Main.rand.NextBool() ? 0f : MathHelper.PiOver4;
                for (int i = 0; i <= hailAmount; i++)
                    Utilities.NewProjectileBetter(target.Center, Vector2.UnitY.RotatedBy((MathHelper.TwoPi * i / hailAmount) + angleOffset) * hailSpeed, ModContent.ProjectileType<LargeHail>(), 0, 0, Main.myPlayer, (int)LargeHail.HailType.Shatter);
            }

            if (attackTimer >= attackLength - fadeTime)
            {
                float interpolant = (attackTimer - (attackLength - fadeTime)) / attackLength;
                npc.Opacity = MathHelper.Lerp(1, 0, interpolant);
            }
                
            if (attackTimer >= attackLength)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            // Clear the first 4 extra ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Create a list of possible attacks.
            List<AttackTypes> possibleAttacks = new()
            {
                AttackTypes.LightningShotgun,
                AttackTypes.IceBulletHell,
                AttackTypes.HailChunks
            };

            // Do not directly repeat an attack.
            possibleAttacks.Remove((AttackTypes)npc.ai[1]);

            // Set the new attack, and reset the attack timer.
            npc.ai[1] = (float)possibleAttacks[Main.rand.Next(possibleAttacks.Count)];
            npc.ai[0] = 0;

            // Also reset the opacity.
            npc.Opacity = 1;
            npc.netUpdate = true;
        }
        #endregion

        #region Drawing/Visuals
        public static void CreateTeleportVisuals(Vector2 position)
        {
            // Create some distinct cloud particles as a telegraph.
            for (int i = 0; i < 6; i++)
            {
                Vector2 position2 = position + Main.rand.NextVector2Circular(80, 130);
                Particle smoke = new MediumMistParticle(position2, Vector2.Zero, Color.LightGray, Color.Gray, Main.rand.NextFloat(0.8f, 1.2f), 110);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture;
            if ((AttackTypes)npc.ai[1] == AttackTypes.LightningShotgun)
                texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/NormalNPCs/ThiccWaifuAttack").Value;
            else
                texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/NormalNPCs/ThiccWaifu").Value;

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects direction = (npc.Center.X - Main.player[npc.target].Center.X < 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }
        #endregion
    }
}
