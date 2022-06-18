using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace InfernumMode.MachineLearning.Architecture
{
    [Serializable]
    public class NeuralNetwork
    {
        internal int InputCount;
        internal Func<double, double, double> CostFunction;
        internal List<Layer> Layers = new List<Layer>();

        public double LearningRate;
        public GeneralMatrix Outputs => Layers.Last().Outputs;

        public NeuralNetwork(double learningRate, int inputCount, Func<double, double, double> costFunction)
        {
            LearningRate = learningRate;
            InputCount = inputCount;
            CostFunction = costFunction;
        }

        /// <summary>
        /// Adds a new layer to the network.
        /// </summary>
        /// <param name="neuronCount">The amount of neurons the network should have.</param>
        /// <param name="activationFunction">The activation function that the layer should use.</param>
        public void AddLayer(int neuronCount, Func<double, double> activationFunction)
        {
            int inputCount;
            if (Layers.Count == 0)
            {
                inputCount = InputCount;
                neuronCount = 1;
            }
            else
                inputCount = Layers.Last().Outputs.TotalRows;

            Layers.Add(new Layer(inputCount, neuronCount, LearningRate, activationFunction, CostFunction));
        }

        /// <summary>
        /// Updates the outputs of the network, allowing optional overriding of the outputs.
        /// </summary>
        /// <param name="newInputs">The new inputs to send. This does not need to be inputted.</param>
        public void UpdateOutputs(GeneralMatrix? newInputs = null)
        {
            // Don't bother doing anything if no layers exist yet.
            if (Layers.Count == 0)
                return;

            Layers[0].UpdateOutputs(newInputs);
            for (int i = 1; i < Layers.Count; i++)
                Layers[i].UpdateOutputs(Layers[i - 1].Outputs);
        }

        public GeneralMatrix? Train(GeneralMatrix newInputs, GeneralMatrix expectedValues, out double loss)
        {
            // Default to 0 for the loss.
            loss = 0D;

            // Don't bother doing anything if no layers exist yet.
            if (Layers.Count == 0)
                return null;

            // Update inputs.
            UpdateOutputs(newInputs);
            GeneralMatrix outputs = Outputs;

            // Define the loss after outputs have been calculated by comparing them to expected values.
            for (int i = 0; i < Layers.Last().Outputs.Length; i++)
                loss += CostFunction(Layers.Last().Outputs[i, 0], expectedValues[i, 0]) / (float)Layers.Last().Outputs.Length;

            // Update all weights and biases in the network.
            Layers.Last().Backpropagate(expectedValues);
            for (int i = Layers.Count - 2; i >= 0; i--)
                Layers[i].Backpropagate(Layers[i + 1]);

            return outputs;
        }

        /// <summary>
        /// Saves this network via binary formatting and gives back the byte array result. This utilizes compression.
        /// </summary>
        public byte[] Save()
        {
            // Get the uncompressed data by serializing this network via binary formatting.
            byte[] uncompressedData;
            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, this);
                uncompressedData = stream.ToArray();
            }

            // Create an intermediate compression and output stream.
            // The compression stream will take the uncompressed data from the formatter, compress it optimally, and
            // then give it to the output stream. Once the output stream has the result, it returns the compressed bytes.
            using (MemoryStream outputStream = new MemoryStream(uncompressedData.Length))
            {
                using (GZipStream compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                    compressionStream.Write(uncompressedData, 0, uncompressedData.Length);

                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Loads this network via binary formatting from a byte array. This utilizes compression.
        /// </summary>
        /// <param name="formattedBytes">The saved bytes of the neural network.</param>
        public static NeuralNetwork Load(byte[] compressedBytes)
        {
            // Create an inputted stream with the compressed bytes and an output stream that would hold the decompressed data.
            using (MemoryStream inputStream = new MemoryStream(compressedBytes))
            using (MemoryStream outputStream = new MemoryStream())
            {
                // And create an intermediate stream that will read the input stream and decompress it.
                // Once down it out move the results to the output stream, where it will be read.
                using (GZipStream compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    compressionStream.CopyTo(outputStream);

                // Go back to the start of the output stream and read the original network from it.
                outputStream.Position = 0;
                return (NeuralNetwork)new BinaryFormatter().Deserialize(outputStream);
            }
        }
    }
}
