namespace InfernumMode.Common.Graphics.Fluids
{
    public readonly struct FluidFieldProperties
    {
        public readonly float VelocityDiffusion;

        public readonly float ColorDiffusion;

        public readonly float DensityDecayFactor;

        public readonly float DensityClumpingFactor;

        public readonly float VorticityAmount;

        public readonly float VelocityPersistence;

        public readonly float DecelerationFactor;

        public FluidFieldProperties(float velocityDiffusion, float colorDiffusion, float densityDecayFactor, float densityClumpingFactor, float vorticityAmount, float velocityPersistence, float decelerationFactor)
        {
            VelocityDiffusion = velocityDiffusion;
            ColorDiffusion = colorDiffusion;
            DensityDecayFactor = densityDecayFactor;
            DensityClumpingFactor = densityClumpingFactor;
            VorticityAmount = vorticityAmount;
            VelocityPersistence = velocityPersistence;
            DecelerationFactor = decelerationFactor;
        }
    }
}
