namespace InfernumMode.Common.Graphics.Fluids
{
    public readonly struct FluidFieldProperties(float velocityDiffusion, float colorDiffusion, float densityDecayFactor, float densityClumpingFactor, float vorticityAmount, float velocityPersistence, float decelerationFactor)
    {
        public readonly float VelocityDiffusion = velocityDiffusion;

        public readonly float ColorDiffusion = colorDiffusion;

        public readonly float DensityDecayFactor = densityDecayFactor;

        public readonly float DensityClumpingFactor = densityClumpingFactor;

        public readonly float VorticityAmount = vorticityAmount;

        public readonly float VelocityPersistence = velocityPersistence;

        public readonly float DecelerationFactor = decelerationFactor;
    }
}
