using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class DebugView : MonoBehaviour
{
    private TextMeshProUGUI textMeshPro = null;

    private SortedDictionary<string, object> debugParams = new SortedDictionary<string, object>();

    private void Start()
    {
        this.textMeshPro = base.GetComponent<TextMeshProUGUI>();

        // Change the target frame rate to the maximize value from default value.
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;

        this.SetDebugParameter("SystemInfo.supportsComputeShaders", SystemInfo.supportsComputeShaders);
        this.SetDebugParameter("SystemInfo.deviceName", SystemInfo.deviceName);
        this.SetDebugParameter("Application.targetFrameRate", Application.targetFrameRate);
        this.SetDebugParameter("Screen.currentResolution.refreshRateRatio", Screen.currentResolution.refreshRateRatio);
        this.SetDebugParameter("QualitySettings.vSyncCount", QualitySettings.vSyncCount);
        this.SetDebugParameter("FrameTimingManager.GetVSyncsPerSecond()", FrameTimingManager.GetVSyncsPerSecond());
    }

    private void Update()
    {
#if false
        FrameTimingManager.CaptureFrameTimings();

        FrameTiming[] frameTimings = new FrameTiming[60];
        FrameTimingManager.GetLatestTimings((uint)frameTimings.Length, frameTimings);

        var cpuFrameTime = frameTimings.Select(o => o.cpuFrameTime).Average();
        var cpuMainThreadFrameTime = frameTimings.Select(o => o.cpuMainThreadFrameTime).Average();
        var cpuRenderThreadFrameTime = frameTimings.Select(o => o.cpuRenderThreadFrameTime).Average();
        var cpuMainThreadPresentWaitTime = frameTimings.Select(o => o.cpuMainThreadPresentWaitTime).Average();
        var gpuFrameTime = frameTimings.Select(o => o.gpuFrameTime).Average();

        this.SetDebugParameter("frameTiming.cpuFrameTime", $"{cpuFrameTime:0.0000}");
        this.SetDebugParameter("frameTiming.cpuMainThreadFrameTime", $"{cpuMainThreadFrameTime:0.0000}");
        this.SetDebugParameter("frameTiming.cpuRenderThreadFrameTime", $"{cpuRenderThreadFrameTime:0.0000}");
        this.SetDebugParameter("frameTiming.cpuMainThreadPresentWaitTime", $"{cpuMainThreadPresentWaitTime:0.0000}");
        this.SetDebugParameter("frameTiming.gpuFrameTime", $"{gpuFrameTime:0.0000}");
#endif

        var lines = this.debugParams.Select(pair => $"{pair.Key}: {pair.Value}");
        this.textMeshPro.text = string.Join('\n', lines);
    }

    public void SetDebugParameter<T>(string name, T value)
    {
        this.debugParams[name] = value.ToString();
    }
}
