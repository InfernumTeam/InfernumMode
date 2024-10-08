﻿using System;
using Microsoft.Xna.Framework;
using Terraria.WorldBuilding;

namespace InfernumMode.Common.Worldgen
{
    public class CustomInfernumShapes
    {
        public class HalfCircle(int radius) : GenShape
        {
            private readonly int radius = radius;

            public override bool Perform(Point origin, GenAction action)
            {
                for (int i = origin.Y - radius; i <= origin.Y; i++)
                {
                    int a = Math.Min(radius, (int)Math.Sqrt((i - origin.Y) * (i - origin.Y)));
                    for (int j = origin.X - a; j <= origin.X + a; j++)
                    {
                        if (!UnitApply(action, origin, j, i + radius, []) && _quitOnFail)
                            return false;
                    }
                }
                return true;
            }
        }
    }
}
