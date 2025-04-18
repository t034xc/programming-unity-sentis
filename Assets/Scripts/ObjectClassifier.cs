using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using UnityEngine;

public class ObjectClassifier : MonoBehaviour
{
    [SerializeField]
    private ModelAsset weights = null;

    private Model model = null;

    private Worker worker = null;

    private RenderTexture renderTexture = null;

    private Tensor<float> tensor0 = null;

    private string[] labels = null;

    [SerializeField]
    private DebugView debugView = null;

    private void Start()
    {
        this.model = ModelLoader.Load(this.weights);
        this.worker = new Worker(this.model, SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU);

        int N = this.model.inputs[0].shape.Get(0);
        int C = this.model.inputs[0].shape.Get(1);
        int H = this.model.inputs[0].shape.Get(2);
        int W = this.model.inputs[0].shape.Get(3);

        Debug.Assert(N == 1);
        Debug.Assert(C == 3);
        Debug.Assert(H == 224);
        Debug.Assert(W == 224);

        this.renderTexture = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGBHalf);

        this.tensor0 = new Tensor<float>(new TensorShape(N, C, H, W));

        Debug.Assert(this.model != null);
        Debug.Assert(this.worker != null);

        this.debugView.SetDebugParameter("classifier.weights.name", this.weights.name);

        this.labels = (Resources.Load("imagenet-1k") as TextAsset).text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }
    private static IEnumerable<int> topK(float[] values, int k)
    {
        for (int i = 0; i < k; i++)
        {
            float max = values.Max();
            int index = Array.IndexOf(values, max);
            values[index] = float.MinValue;
            yield return index;
        }
    }

    public async Awaitable<string[]> Inference(Texture2D image, Vector2 scale, Vector2 offset)
    {
        Graphics.Blit(image, this.renderTexture, scale, offset);

        TextureTransform transform = new TextureTransform()
            .SetTensorLayout(TensorLayout.NCHW)
            .SetChannelSwizzle(ChannelSwizzle.RGBA);
        TextureConverter.ToTensor(this.renderTexture, this.tensor0, transform);

        this.worker.Schedule(this.tensor0);
        using var tensor1 = await this.worker.PeekOutput().ReadbackAndCloneAsync() as Tensor<float>;

        this.debugView.SetDebugParameter("classifier.tensor0.shape", this.tensor0.shape);
        this.debugView.SetDebugParameter("classifier.tensor1.shape", tensor1.shape);

        float[] outputs = tensor1.AsReadOnlySpan().ToArray();
        string[] labels = topK(outputs, 5).Select(i => this.labels[i]).ToArray();
        return labels;
    }

    private void OnDestroy()
    {
        this.worker.Dispose();
        this.tensor0.Dispose();

        RenderTexture.ReleaseTemporary(this.renderTexture);
    }
}
