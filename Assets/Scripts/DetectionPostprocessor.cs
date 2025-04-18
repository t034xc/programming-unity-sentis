using System;
using Unity.Sentis;
using UnityEngine;

public class DetectionPostprocessor : IDisposable
{
    private Model model = null;

    private Worker worker = null;

    public DetectionPostprocessor(int imageSize, float iouThreshold, float scoreThreshold)
    {
        var graph = new FunctionalGraph();
        var bboxes = graph.AddInput<float>(new DynamicTensorShape(1, -1, 4)) / imageSize;
        var labels = graph.AddInput<float>(new DynamicTensorShape(1, -1, -1));
        var scores = Functional.ReduceMax(labels, 2);
        var indices = Functional.NMS(bboxes.Reshape(new[] { -1, 4 }), scores.Reshape(new[] { -1 }), iouThreshold, scoreThreshold);
        var classIds = Functional.ArgMax(labels, 2);

        var _bboxes = Functional.IndexSelect(bboxes, 1, indices);
        var _scores = Functional.IndexSelect(scores, 1, indices);
        var _classIds = Functional.IndexSelect(classIds, 1, indices);

        this.model = graph.Compile(_bboxes, _scores, _classIds);

        var backendType = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU;
        this.worker = new Worker(this.model, backendType);
    }

    public void Dispose()
    {
        this.worker.Dispose();
    }

    public Tuple<Tensor<float>, Tensor<float>, Tensor<int>> Execute(Tensor<float> bboxes, Tensor<float> labels)
    {
        this.worker.Schedule(bboxes, labels);
        var _bboxes = this.worker.PeekOutput(0) as Tensor<float>;
        var _scores = this.worker.PeekOutput(1) as Tensor<float>;
        var _classIds = this.worker.PeekOutput(2) as Tensor<int>;
        return Tuple.Create(_bboxes, _scores, _classIds);
    }
}
