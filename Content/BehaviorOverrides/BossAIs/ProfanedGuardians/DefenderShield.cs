using CalamityMod;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderShield : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public NPC Owner => Main.npc[(int)Projectile.ai[1]];

        public DefenderShieldStatus Status => (DefenderShieldStatus)Owner.Infernum().ExtraAI[DefenderShieldStatusIndex];

        public static int GlowTime => 30;

        public Vector2 PositionOffset;

        public static Texture2D[] ChunkTextures => new Texture2D[]
        { 
            ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/Rocks/ShieldChunk1").Value,
            ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/Rocks/ShieldChunk2").Value,
            ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/Rocks/ShieldChunk3").Value,
            ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/Rocks/ShieldChunk4").Value,
            ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/Rocks/ShieldChunk5").Value,
        };

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/DefenderShield";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Rock Shield");

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 110;
            Projectile.hostile = true;
            Projectile.Opacity = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2000;
        }

        public override void AI()
        {
            bool shouldKill = Status is DefenderShieldStatus.MarkedForRemoval;
            if (!Owner.active || Owner.type != ModContent.NPCType<ProfanedGuardianDefender>() || shouldKill)
            {
                // Reset this index.
                Owner.Infernum().ExtraAI[DefenderShieldStatusIndex] = 0;
                Projectile.Kill();
                return;
            }

            // Move where the defender is aiming.
            if (Status is DefenderShieldStatus.ActiveAndAiming)
            {
                Vector2 idealOffset = Owner.SafeDirectionTo(Main.player[Owner.target].Center) * 60f;
                PositionOffset = CalamityUtils.MoveTowards(PositionOffset, idealOffset,7f);
                Projectile.rotation = PositionOffset.ToRotation();
                Projectile.netUpdate = true;
                Projectile.netSpam = 0;
                Projectile.spriteDirection = (Projectile.SafeDirectionTo(Main.player[Owner.target].Center).X > 0f).ToDirectionInt();
            }
            Projectile.Center = Owner.Center + PositionOffset;

            //if (Timer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            //{
            //    for (int i = 0; i < ChunkAmount; i++)
            //    {
            //        Texture2D chunkTexture = ChunkTextures[i];

            //        // Get a random velocity in front of the shield to spawn the chunk at.
            //        Vector2 direction = Owner.SafeDirectionTo(Projectile.Center);
            //        Vector2 position = Projectile.Center + (direction * Main.rand.NextFloat(1f, 1.5f)).RotatedBy(Main.rand.NextFloat(-0.3f, 0.3f));
            //        float speed = (position - Projectile.Center).Length() / ChunkMoveTime;
            //        Vector2 velocity = position.DirectionTo(Projectile.Center) * speed;
            //        DefenderShieldChunk chunk = new(Projectile.whoAmI, chunkTexture, position , velocity, 0f, 1f, 0f, ChunkMoveTime + GlowTime);
            //        Chunks.Add(chunk);
            //    }
            //}

            //// Update any active chunks.
            //foreach (DefenderShieldChunk chunk in Chunks)
            //{
            //    chunk.Update();
            //    float maxChange = MathHelper.Lerp((Projectile.rotation - chunk.Rotation) * 0.1f, Projectile.rotation - chunk.Rotation, chunk.LifetimeCompletion);
            //    chunk.Rotation = chunk.Rotation.AngleTowards(Projectile.rotation, maxChange);
            //}
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.05f, 0f, 1f);
            //Chunks.RemoveAll((DefenderShieldChunk chunk) => chunk.Time >= chunk.Lifetime);
            Projectile.timeLeft = 2000;
            Timer++;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(PositionOffset.X);
            writer.Write(PositionOffset.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            PositionOffset.X = reader.ReadSingle();
            PositionOffset.Y = reader.ReadSingle();
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;//Projectile.rotation is >= MathHelper.PiOver2 and <= MathHelper.TwoPi - MathHelper.PiOver2 ? SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically : SpriteEffects.FlipHorizontally;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            float backglowAmount = 12f;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                Main.spriteBatch.Draw(texture, Projectile.Center + backglowOffset - Main.screenPosition, null, backglowColor * MathHelper.Clamp(Projectile.Opacity * 2f, 0f, 1f), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, spriteEffects, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, spriteEffects, 0);

            //// Draw any active chunks.
            //foreach (DefenderShieldChunk chunk in Chunks)
            //{
            //    Color chunkColor = Lighting.GetColor((int)chunk.Center.X, (int)chunk.Position.Y);
            //    chunk.Draw(Main.spriteBatch, chunkColor, spriteEffects);
            //}

            return false;
        }
    }

    //// Using projectiles for these is a bit unecessary.
    //public class DefenderShieldChunk
    //{
    //    public int ShieldIndex;
    //    // This wont ever get called if the shield doesnt exist.
    //    public Projectile ShieldProjectile => Main.projectile[ShieldIndex];
    //    public Texture2D Texture;
    //    public Vector2 Position;
    //    public Vector2 Velocity;
    //    public Vector2 Center => Position + Texture.Size() * 0.5f;
    //    public float Opacity;
    //    public float Scale;
    //    public float Rotation;
    //    public float Time;
    //    public int Lifetime;
    //    public float LifetimeCompletion => Time / Lifetime;
    //    public bool ReverseFade;

    //    public DefenderShieldChunk(int shieldIndex, Texture2D texture, Vector2 position, Vector2 velocity, float opacity, float scale, float rotation, int lifetime, bool reverseFade = false)
    //    {
    //        ShieldIndex = shieldIndex;
    //        Texture = texture;
    //        Position = position;
    //        Velocity = velocity;
    //        Opacity = opacity;
    //        Scale = scale;
    //        Rotation = rotation;
    //        Lifetime = lifetime;
    //        ReverseFade = reverseFade;
    //    }

    //    public void Update()
    //    {
    //        Position += Velocity;

    //        if (ReverseFade)
    //            Opacity = MathHelper.Clamp(Opacity - 0.05f, 0f, 1f);
    //        else
    //            Opacity = MathHelper.Clamp(Opacity + 0.05f, 0f, 1f);
    //        Time++;
    //    }

    //    public void Draw(SpriteBatch spriteBatch, Color lightColor, SpriteEffects spriteEffects)
    //    {
    //        spriteBatch.Draw(Texture, Center - Main.screenPosition, null, Color.White, Rotation, Texture.Size() * 0.5f, Scale, spriteEffects, 0f);
    //        if (Time >= DefenderShield.ChunkMoveTime)
    //        {
    //            float interpolant = (Time - DefenderShield.ChunkMoveTime) / (DefenderShield.ChunkMoveTime + DefenderShield.GlowTime - Time);
    //            float colorScalar = MathF.Sin(interpolant * MathF.PI);
    //            spriteBatch.Draw(Texture, Center - Main.screenPosition, null, WayfinderSymbol.Colors[1] with { A = 0 } * colorScalar, Rotation, Texture.Size() * 0.5f, Scale * 1.1f, spriteEffects, 0f);
    //        }
    //    }
    //}
}
