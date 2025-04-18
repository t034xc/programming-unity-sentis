using System;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;

public class ObjectDetector : MonoBehaviour
{
    [SerializeField]
    private RawImage image = null;

    [SerializeField]
    private ModelAsset weights = null;

    private Model model = null;

    private Worker worker = null;

    private WebCamera webCamera = null;

    private Tensor<float> tensor0 = null;

    private DetectionPreprocessor preprocessor = null;

    private DetectionPostprocessor postprocessor = null;

    private const float IoUThreshold = 0.5f;

    private const float ScoreThreshold = 0.5f;

    private float prevCheckTime = 0.0f;

    private int inferenceCount = 0;

    private Awaitable detectAwaitable = null;

    private Color[] colormap = null;

    private string[] labels = null;

    [SerializeField]
    private DebugView debugView = null;

    [SerializeField]
    private Font font = null;

    private ObjectClassifier classifier = null;

    private async void Start()
    {
        this.model = ModelLoader.Load(this.weights);
        this.worker = new Worker(this.model, SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU);
        this.webCamera = FindFirstObjectByType<WebCamera>();

        int N = this.model.inputs[0].shape.Get(0);
        int C = this.model.inputs[0].shape.Get(1);
        int H = this.model.inputs[0].shape.Get(2);
        int W = this.model.inputs[0].shape.Get(3);

        Debug.Assert(N == 1);
        Debug.Assert(C == 3);
        Debug.Assert(H == 416);
        Debug.Assert(W == 416);
        Debug.Assert(W == H);

        this.tensor0 = new Tensor<float>(new TensorShape(N, C, H, W));
        this.preprocessor = new DetectionPreprocessor();
        this.postprocessor = new DetectionPostprocessor(W, IoUThreshold, ScoreThreshold);

        Debug.Assert(this.model != null);
        Debug.Assert(this.worker != null);
        Debug.Assert(this.webCamera != null);
        Debug.Assert(this.preprocessor != null);
        Debug.Assert(this.postprocessor != null);

        this.debugView.SetDebugParameter("detector.weights.name", this.weights.name);

        this.labels = (Resources.Load("coco") as TextAsset).text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        string[] lines = (Resources.Load("colormap") as TextAsset).text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        this.colormap = new Color[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            ColorUtility.TryParseHtmlString(lines[i], out this.colormap[i]);
        }

        while (true)
        {
            try
            {
                this.detectAwaitable = this.Detect();
                await this.detectAwaitable;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Update()
    {
        // Draw Objects on Unity UI
        //this.Detect();

        float duration = Time.realtimeSinceStartup - this.prevCheckTime;
        if (duration > 0.5f)
        {
            this.debugView.SetDebugParameter("fps (inference)", this.inferenceCount / duration);
            this.inferenceCount = 0;
            this.prevCheckTime = Time.realtimeSinceStartup;
        }
    }

    private async Awaitable Detect()
    {
        Texture2D capture = this.webCamera.GetTexture();
        if (capture == null)
        {
            await Awaitable.NextFrameAsync();
            return;
        }
        if (this.classifier == null)
        {
            this.classifier = base.GetComponent<ObjectClassifier>();
        }

        TextureTransform transform = new TextureTransform()
            .SetTensorLayout(TensorLayout.NCHW)
            .SetChannelSwizzle(ChannelSwizzle.BGRA);
        TextureConverter.ToTensor(capture, this.tensor0, transform);

        var tensor0 = this.preprocessor.Execute(this.tensor0);
        this.worker.Schedule(tensor0);

        using var tensor1 = await this.worker.PeekOutput(0).ReadbackAndCloneAsync() as Tensor<float>;
        using var tensor2 = await this.worker.PeekOutput(1).ReadbackAndCloneAsync() as Tensor<float>;

        (var tensor3, var tensor4, var tensor5) = this.postprocessor.Execute(tensor1, tensor2);
        using var bboxes = tensor3.ReadbackAndClone();
        using var scores = tensor4.ReadbackAndClone();
        using var classIds = tensor5.ReadbackAndClone();

        this.debugView.SetDebugParameter("detector.tensor0.shape", tensor0.shape);
        this.debugView.SetDebugParameter("detector.tensor1.shape", tensor1.shape);
        this.debugView.SetDebugParameter("detector.tensor2.shape", tensor2.shape);
        this.debugView.SetDebugParameter("detector.tensor3.shape", tensor3.shape);
        this.debugView.SetDebugParameter("detector.tensor4.shape", tensor4.shape);
        this.debugView.SetDebugParameter("detector.tensor5.shape", tensor5.shape);

        int numObjects = bboxes.shape[1];
        Debug.Assert(numObjects == bboxes.shape[1]);
        Debug.Assert(numObjects == scores.shape[1]);
        Debug.Assert(numObjects == classIds.shape[1]);

        List<DetectionObject> objects = new List<DetectionObject>(numObjects);
        for (int i = 0; i < numObjects; i++)
        {
            float x1 = bboxes[0, i, 0];
            float y1 = bboxes[0, i, 1];
            float x2 = bboxes[0, i, 2];
            float y2 = bboxes[0, i, 3];
            float score = scores[0, i];
            int classId = classIds[0, i];

            DetectionObject obj = new DetectionObject()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Score = score,
                ClassId = classId,
                Labels = new[] { this.labels[classId] }
            };
            objects.Add(obj);
        }

#if true
        foreach (var obj in objects)
        {
            float x = obj.X1;
            float y = obj.Y2;
            float w = (obj.X2 - obj.X1);
            float h = (obj.Y2 - obj.Y1);
            Vector2 offset = new Vector2(x, -y);
            Vector2 scale = new Vector2(w, h);

            obj.Labels = await this.classifier.Inference(capture, scale, offset);
        }
#endif

        DetectionVisualizer.ClearBoundingBoxes(this.image);
        DetectionVisualizer.ClearLabels(this.image);

        foreach (var obj in objects)
        {
            float x1 = obj.X1 * this.image.texture.width;
            float y1 = obj.Y1 * this.image.texture.height;
            float x2 = obj.X2 * this.image.texture.width;
            float y2 = obj.Y2 * this.image.texture.height;
            Rect rect = new Rect(x1, y1, x2 - x1, y2 - y1);

            Color color = this.colormap[obj.ClassId % this.colormap.Length];
            color.a = (obj.Score - ScoreThreshold);
            DetectionVisualizer.DrawBoudingBox(this.image, rect, color);

            string message = string.Join("\n", obj.Labels);
            DetectionVisualizer.DrawLabel(this.image, rect, Color.green, message, this.font);
        }

        this.inferenceCount++;
    }
    private void OnDestroy()
    {
        this.worker.Dispose();
        this.tensor0.Dispose();
        this.preprocessor.Dispose();
        this.postprocessor.Dispose();
        this.detectAwaitable.Cancel();
    }
}
