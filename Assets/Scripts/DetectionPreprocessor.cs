using System;
using Unity.Sentis;
using UnityEngine;

public class DetectionPreprocessor : IDisposable
{
    private Model model = null;

    private Worker worker = null;

    public DetectionPreprocessor(float multipler = 255.0f)
    {
        var graph = new FunctionalGraph();
        var x = graph.AddInput<float>(new DynamicTensorShape(1, 3, -1, -1));
        var y = x * multipler;
        this.model = graph.Compile(y);

        var backendType = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU;
        this.worker = new Worker(this.model, backendType);
    }

    public void Dispose()
    {
        this.worker.Dispose();
    }

    public Tensor<float> Execute(Tensor<float> x)
    {
        this.worker.Schedule(x);
        return this.worker.PeekOutput() as Tensor<float>;
    }
}
