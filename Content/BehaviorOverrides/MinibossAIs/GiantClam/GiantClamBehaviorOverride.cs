using CalamityMod;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using GiantClamNPC = CalamityMod.NPCs.SunkenSea.GiantClam;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.GiantClam
{
    public class GiantClamBehaviorOverride : NPCBehaviorOverride
    {
        public enum GiantClamAttackState
        {
            PearlSwirl = 0,
            PearlRain = 1,
            TeleportSlam = 2,
        }

        public const int HitsRequiredToAnger = 1;

        public override int NPCOverrideType => ModContent.NPCType<GiantClamNPC>();

        public override bool PreAI(NPC npc)
        {
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float attackTimer = ref npc.Infernum().ExtraAI[1];
            ref float hidingInShell = ref npc.Infernum().ExtraAI[2];
            ref float hitCount = ref npc.ai[0];
            bool hardmode = Main.hardMode;

            npc.netAlways = true;

            if (hitCount < HitsRequiredToAnger)
            {
                if (npc.justHit)
                {
                    hitCount++;
                    npc.netUpdate = true;
                }

                npc.chaseable = false;
                npc.defense = 9999;

                return false;
            }

            if (hitCount >= HitsRequiredToAnger)
            {
                if (npc.Infernum().ExtraAI[5] == 0f)
                {
                    typeof(GiantClamNPC).GetField("hasBeenHit", Utilities.UniversalBindingFlags).SetValue(npc.ModNPC, true);
                    npc.Infernum().ExtraAI[5] = 1f;
                    npc.netUpdate = true;
                }

                hitCount++;
                npc.defense = 15;
                npc.damage = 80;

                if (Main.hardMode)
                {
                    npc.defense = 35;
                    npc.damage = 130;
                }

                npc.defDamage = npc.damage;
                npc.defDefense = npc.defense;

                npc.chaseable = true;

                npc.netUpdate = true;
            }

            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Disappear if the target is no longer present
            if (target.dead || !target.active)
            {
                npc.active = false;
                return false;
            }

            // Provide the Boss Effects buff to the target once angry.
            target.AddBuff(ModContent.BuffType<BossEffects>(), 2);

            switch ((GiantClamAttackState)(int)attackState)
            {
                case GiantClamAttackState.PearlSwirl:

                    if (attackTimer > 180)
                        hidingInShell = 1f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && (attackTimer == 0 || attackTimer == 180))
                    {
                        int projectileType = ModContent.ProjectileType<PearlSwirl>();
                        for (float angle = 0; angle <= MathHelper.TwoPi; angle += MathHelper.PiOver2)
                            Utilities.NewProjectileBetter(npc.Center, angle.ToRotationVector2() * 10f, projectileType, npc.damage, 0f);
                        if (hardmode)
                        {
                            for (float angle = 0; angle <= MathHelper.TwoPi; angle += MathHelper.PiOver2)
                                Utilities.NewProjectileBetter(npc.Center, angle.ToRotationVector2() * 10f, projectileType, npc.damage, 0f, -1, 1f);
                        }
                        npc.netUpdate = true;
                    }

                    attackTimer++;
                    if (attackTimer >= 240)
                        GoToNextAttack(npc);
                    break;

                case GiantClamAttackState.PearlRain:
                    if (attackTimer == 0f)
                    {
                        SoundEngine.PlaySound(SoundID.Item67, npc.position);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (float offset = -750f; offset < 750f; offset += 150f)
                            {
                                Vector2 spawnPosition = target.Center + new Vector2(offset, -750f);
                                Vector2 pearlShootVelocity = Vector2.UnitY * 8f;
                                Utilities.NewProjectileBetter(spawnPosition, pearlShootVelocity, ModContent.ProjectileType<PearlRain>(), npc.damage, 0f, Main.myPlayer, 0f, 0f);
                            }
                            for (float offset = -675f; offset < 825f; offset += 150f)
                            {
                                Vector2 spawnPosition = target.Center + new Vector2(offset, 750f);
                                Vector2 pearlShootVelocity = Vector2.UnitY * -8f;
                                Utilities.NewProjectileBetter(spawnPosition, pearlShootVelocity, ModContent.ProjectileType<PearlRain>(), npc.damage, 0f, Main.myPlayer, 0f, 0f);
                            }
                            npc.netUpdate = true;
                        }
                    }
                    if (hardmode)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 90f)
                        {
                            for (float offset = -750f; offset < 750f; offset += 200f)
                            {
                                Vector2 spawnPosition = target.Center + new Vector2(-950f, offset);
                                Vector2 pearlShootVelocity = Vector2.UnitX * 8f;
                                Utilities.NewProjectileBetter(spawnPosition, pearlShootVelocity, ModContent.ProjectileType<PearlRain>(), npc.damage, 0f, Main.myPlayer, 0f, 0f);
                            }
                            for (float offset = -675f; offset < 825f; offset += 200f)
                            {
                                Vector2 spawnPosition = target.Center + new Vector2(950f, offset);
                                Vector2 pearlShootVelocity = Vector2.UnitX * -8f;
                                Utilities.NewProjectileBetter(spawnPosition, pearlShootVelocity, ModContent.ProjectileType<PearlRain>(), npc.damage, 0f, Main.myPlayer, 0f, 0f);
                            }
                            npc.netUpdate = true;
                        }
                    }
                    if (attackTimer >= 180)
                        hidingInShell = 1f;
                    attackTimer++;
                    if (attackTimer >= 210)
                        GoToNextAttack(npc);
                    break;

                case GiantClamAttackState.TeleportSlam:
                    ref float attackSubstate = ref npc.Infernum().ExtraAI[3];
                    ref float slamCount = ref npc.Infernum().ExtraAI[4];
                    npc.damage = hardmode ? 200 : 125;
                    npc.velocity.X = 0f;
                    if (attackTimer == 1f)
                    {
                        attackSubstate = 1f;
                        npc.netUpdate = true;
                    }

                    if (attackSubstate == 1f)
                    {
                        npc.alpha += 20;

                        npc.velocity.Y = 0f;
                        npc.noGravity = true;
                        npc.noTileCollide = true;

                        if (npc.alpha >= 255)
                        {
                            npc.alpha = 255;
                            npc.position.X = target.position.X - 60f;
                            npc.position.Y = target.position.Y - 400f;

                            attackSubstate = 2f;
                            npc.netUpdate = true;
                        }
                    }
                    else if (attackSubstate == 2f)
                    {
                        if (slamCount < 1)
                            npc.alpha -= 6;
                        else
                            npc.alpha -= 16;

                        if (npc.alpha <= 0)
                        {
                            npc.alpha = 0;
                            attackSubstate = 3f;
                            npc.netUpdate = true;
                        }
                    }
                    else if (attackSubstate == 3f)
                    {
                        if (npc.Bottom.Y > target.Top.Y || npc.noTileCollide == false)
                        {
                            npc.noTileCollide = false;

                            if (npc.velocity.Y == 0f)
                            {
                                SoundEngine.PlaySound(GiantClamNPC.SlamSound, npc.Bottom);
                                slamCount++;

                                if (slamCount < (hardmode ? 6f : 3f))
                                {
                                    attackTimer = 0f;
                                    attackSubstate = 1f;
                                }
                                else
                                    attackSubstate = 4f;
                                npc.netUpdate = true;
                            }
                            else
                                npc.velocity.Y += 1f;
                        }
                        else
                            npc.velocity.Y += 1f;
                    }

                    attackTimer++;

                    if (attackSubstate == 4f)
                    {
                        attackSubstate = 0f;
                        slamCount = 0f;
                        GoToNextAttack(npc);
                    }

                    break;
            }

            if (hidingInShell == 0)
            {
                Lighting.AddLight(npc.Center, 0f, npc.Opacity * 2.5f, npc.Opacity * 2.5f);
                npc.defense = npc.defDefense;
            }
            else
                npc.defense = 9999;

            return false;

        }

        public static void GoToNextAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            GiantClamAttackState CurrentAttack = (GiantClamAttackState)(int)npc.Infernum().ExtraAI[0];
            GiantClamAttackState NextAttack = CurrentAttack;

            while (NextAttack == CurrentAttack)
                NextAttack = (GiantClamAttackState)new Random().Next(0, Enum.GetNames(typeof(GiantClamAttackState)).Length);

            npc.Infernum().ExtraAI[0] = (float)NextAttack;
            npc.Infernum().ExtraAI[1] = 0f;

            if (NextAttack == GiantClamAttackState.TeleportSlam)
                npc.damage = Main.hardMode ? 135 : 90;
            else
                npc.damage = npc.defDamage;

            switch (NextAttack)
            {
                case GiantClamAttackState.PearlSwirl:
                case GiantClamAttackState.PearlRain:
                case GiantClamAttackState.TeleportSlam:
                    npc.Infernum().ExtraAI[2] = 0f;
                    break;
            }
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float hitCount = ref npc.ai[0];
            ref float hidingInShell = ref npc.Infernum().ExtraAI[2];
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float attackTimer = ref npc.Infernum().ExtraAI[1];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[3];

            npc.frame.Y = (int)Math.Floor(npc.frameCounter / 5) * frameHeight;
            npc.frameCounter++;
            if (npc.frameCounter > 24)
                npc.frameCounter = 0;

            if (hitCount < HitsRequiredToAnger || hidingInShell == 1f)
                npc.frame.Y = frameHeight * 11;

            if (attackState == (float)GiantClamAttackState.PearlSwirl)
            {
                if (attackTimer > 180)
                    npc.frame.Y = ((int)MathHelper.Clamp(attackTimer - 180f, 0f, 6f) + 5) * frameHeight;
            }

            else if (attackState == (float)GiantClamAttackState.PearlRain)
            {
                if (attackTimer > 180)
                    npc.frame.Y = ((int)MathHelper.Clamp(attackTimer - 180f, 0f, 6f) + 5) * frameHeight;
            }

            else if (attackState == (float)GiantClamAttackState.TeleportSlam)
            {
                if (attackSubstate == 1)
                    npc.frame.Y = ((int)MathHelper.Clamp(attackTimer, 0f, 6f) + 5) * frameHeight;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            Texture2D npcTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SunkenSea/GiantClam").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SunkenSea/GiantClamGlow").Value;

            if ((GiantClamAttackState)(int)npc.Infernum().ExtraAI[0] == GiantClamAttackState.TeleportSlam && npc.velocity.Length() > 1f && CalamityConfig.Instance.Afterimages)
            {
                for (int i = 0; i < 4; i++)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.Transparent, (i + 1f) / 4f));
                    Vector2 drawOffset = -npc.velocity * i * 0.6f;
                    Main.spriteBatch.Draw(npcTexture, drawPosition + drawOffset, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Main.spriteBatch.Draw(npcTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            Main.spriteBatch.Draw(glowmaskTexture, drawPosition, npc.frame, Color.LightBlue, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
    }
}
