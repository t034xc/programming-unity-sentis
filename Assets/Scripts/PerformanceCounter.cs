using UnityEngine;

public class PerformanceCounter : MonoBehaviour
{
    private int frameCount;
    private float prevTime;

    [SerializeField]
    private DebugView debugView = null;

    private void Update()
    {
        this.frameCount++;

        float duration = Time.realtimeSinceStartup - this.prevTime;
        if (duration >= 0.5f)
        {
            float frequency = this.frameCount / duration;
            this.frameCount = 0;
            this.prevTime = Time.realtimeSinceStartup;
            this.debugView.SetDebugParameter("fps (update)", frequency);
        }
    }
}
