using System;
using Terraria;

namespace InfernumMode.MachineLearning.Architecture
{
    [Serializable]
    public class Layer
    {
        public double LearningRate;
        public GeneralMatrix Inputs;
        public GeneralMatrix Weights;
        public GeneralMatrix Biases;
        public GeneralMatrix WeighedInputs;
        public GeneralMatrix Outputs;
        public GeneralMatrix Deltas;
        public Func<double, double> ActivationFunction;
        public Func<double, double, double> CostFunction;

        public Layer(int inputCount, int neuronCount, double learningRate, Func<double, double> activationFunction, Func<double, double, double> costFunction)
        {
            LearningRate = learningRate;
            Inputs = new GeneralMatrix(inputCount, 1);
            Weights = Main.rand.GenerateRandomMatrix(neuronCount, inputCount, -0.1, 0.1);
            Biases = new GeneralMatrix(neuronCount, 1);
            WeighedInputs = new GeneralMatrix(neuronCount, 1);
            Outputs = new GeneralMatrix(neuronCount, 1);
            Deltas = new GeneralMatrix(neuronCount, 1);
            ActivationFunction = activationFunction;
            CostFunction = costFunction;

            UpdateOutputs();
        }

        /// <summary>
        /// Updates the outputs of the layer, allowing optional overriding of the outputs.
        /// </summary>
        /// <param name="newInputs">The new inputs to send. This does not need to be inputted.</param>
        public void UpdateOutputs(GeneralMatrix? newInputs = null)
        {
            // Attempt to change the inputs if necessary.
            Inputs = newInputs ?? Inputs;

            // Compute outputs.
            WeighedInputs = Weights * Inputs + Biases;
            Outputs = Utilities.ApplyFunctionToMatrix(WeighedInputs, ActivationFunction);
        }

        /// <summary>
        /// Updates weights and biases of the network based on a list of expected values.
        /// </summary>
        /// <param name="expectedValues">The list of expected values.</param>
        /// <param name="optimizer">An optional optimizer to use.</param>
        public void Backpropagate(GeneralMatrix expectedValues)
        {
            Deltas = Utilities.ApproximatePartialDerivative(CostFunction, expectedValues, Outputs, 1).ElementwiseMultiplication(Utilities.ApproximateDerivative(ActivationFunction, WeighedInputs));
            GeneralMatrix updateValues = Deltas;
            double length = updateValues.Length;
            if (length > 0.25D)
                updateValues *= 0.25D / length;

            Biases -= updateValues * LearningRate;
            for (int i = 0; i < Weights.TotalRows; i++)
            {
                for (int j = 0; j < Weights.TotalColumns; j++)
                    Weights[i, j] -= updateValues[i, 0] * Inputs[j, 0] * LearningRate;
            }
        }

        /// <summary>
        /// Updates weights and biases of the network based on an ahead layer.
        /// </summary>
        /// <param name="aheadLayer">The ahead layer.</param>
        /// <param name="optimizer">An optional optimizer to use.</param>
        public void Backpropagate(Layer aheadLayer)
        {
            GeneralMatrix aheadLayerWeights = aheadLayer.Weights.Transpose();
            Deltas = (aheadLayerWeights * aheadLayer.Deltas).ElementwiseMultiplication(Utilities.ApproximateDerivative(ActivationFunction, WeighedInputs));
            GeneralMatrix updateValues = Deltas;
            double length = updateValues.Length;
            if (length > 0.25D)
                updateValues *= 0.25D / length;

            Biases -= updateValues * LearningRate;
            for (int i = 0; i < Weights.TotalRows; i++)
            {
                for (int j = 0; j < Weights.TotalColumns; j++)
                    Weights[i, j] -= updateValues[i, 0] * Inputs[j, 0] * LearningRate;
            }
        }
    }
}
