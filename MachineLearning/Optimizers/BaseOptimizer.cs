using System;

namespace InfernumMode.MachineLearning.Optimizers
{
    [Serializable]
    public abstract class BaseOptimizer
    {
        public abstract double[] DecideUpdateValues(double[] updateValues, double learningRate);
    }
}
