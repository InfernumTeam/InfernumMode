using CalamityMod;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GiantClamNPC = CalamityMod.NPCs.SunkenSea.GiantClam;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.GiantClam
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

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float attackTimer = ref npc.Infernum().ExtraAI[1];
            ref float hidingInShell = ref npc.Infernum().ExtraAI[2];
            ref float hitCount = ref npc.ai[0];
            bool hardmode = Main.hardMode;


            if (hitCount < HitsRequiredToAnger)
            {
                if (npc.justHit)
                    hitCount++;

                npc.chaseable = false;
                npc.defense = 9999;

                return false;
            }

            if (hitCount >= HitsRequiredToAnger)
            {
                if (npc.Infernum().ExtraAI[5] == 0f)
                {
                    typeof(GiantClamNPC).GetField("hasBeenHit", Utilities.UniversalBindingFlags).SetValue(npc.modNPC, true);
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

            npc.TargetClosest(true);
            Player target = Main.player[npc.target];

            switch ((GiantClamAttackState)(int)attackState)
            {
                case GiantClamAttackState.PearlSwirl:

                    if (attackTimer > 180)
                        hidingInShell = 1f;

                    if (attackTimer == 0 || attackTimer == 180)
                    {
                        int projectileType = ModContent.ProjectileType<PearlSwirl>();
                        for (float angle = 0; angle <= MathHelper.TwoPi; angle += MathHelper.PiOver2)
                            Utilities.NewProjectileBetter(npc.Center, angle.ToRotationVector2() * 10f, projectileType, npc.damage, 0f);
                        if (hardmode)
                        {
                            for (float angle = 0; angle <= MathHelper.TwoPi; angle += MathHelper.PiOver2)
                            {
                                int proj = Utilities.NewProjectileBetter(npc.Center, angle.ToRotationVector2() * 10f, projectileType, npc.damage, 0f);
                                if (Main.projectile.IndexInRange(proj))
                                    Main.projectile[proj].ai[0] = 1f;
                            }

                        }
                    }

                    attackTimer++;
                    if (attackTimer >= 240)
                        GoToNextAttack(npc);
                    break;

                case GiantClamAttackState.PearlRain:
                    if (attackTimer == 0f)
                    {
                        Main.PlaySound(SoundID.Item67, npc.position);
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
                    }
                    if (hardmode)
                    {
                        if (attackTimer == 90f)
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
                    if (attackTimer == 1f)
                        attackSubstate = 1f;

                    if (attackSubstate == 1f)
                    {
                        npc.alpha += 20;

                        npc.noGravity = true;
                        npc.noTileCollide = true;

                        if (npc.alpha >= 255)
                        {
                            npc.alpha = 255;
                            npc.position.X = target.position.X - 60f;
                            npc.position.Y = target.position.Y - 400f;

                            attackSubstate = 2f;
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
                        }
                    }
                    else if (attackSubstate == 3f)
                    {
                        if (npc.Bottom.Y > target.Top.Y || npc.noTileCollide == false)
                        {
                            npc.noTileCollide = false;

                            if (npc.velocity.Y == 0f)
                            {
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ClamImpact"), (int)npc.position.X, (int)npc.position.Y);
                                slamCount++;

                                if (slamCount < (hardmode ? 6f : 3f))
                                {
                                    attackTimer = 0f;
                                    attackSubstate = 1f;
                                }
                                else
                                    attackSubstate = 4f;
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
            Texture2D npcTexture = ModContent.GetTexture("CalamityMod/NPCs/SunkenSea/GiantClam");
            Texture2D glowmaskTexture = ModContent.GetTexture("CalamityMod/NPCs/SunkenSea/GiantClamGlow");

            if ((GiantClamAttackState)(int)npc.Infernum().ExtraAI[0] == GiantClamAttackState.TeleportSlam && npc.velocity.Length() > 1f && CalamityConfig.Instance.Afterimages)
            {
                for (int i = 0; i < 4; i++)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.Transparent, (i + 1f) / 4f));
                    Vector2 drawOffset = -npc.velocity * i * 0.6f;
                    spriteBatch.Draw(npcTexture, drawPosition + drawOffset, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(npcTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            spriteBatch.Draw(glowmaskTexture, drawPosition, npc.frame, Color.LightBlue, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
    }
}
