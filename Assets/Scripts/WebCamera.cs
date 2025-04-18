using System;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class WebCamera : MonoBehaviour
{
    [SerializeField] private RawImage image = null;
    private WebCamTexture webCamTexture = null;
    private Texture2D texture = null;
    private RenderTexture renderTexture = null;

    [SerializeField]
    private DebugView debugView = null;

    int updateCount = 0;
    private float prevTime;

    private async Awaitable Start()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            bool condition = false;
            Action<string> callback = (permission) => { condition = true; };

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += callback;
            callbacks.PermissionGranted += callback;
            callbacks.PermissionRequestDismissed += callback;

            Permission.RequestUserPermission(Permission.Camera, callbacks);

            while (!condition)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.LogError("error : not authorized access to camera devices.");
            return;
        }
#endif

#if UNITY_IOS
        await Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogError("error : not authorized access to camera devices.");
            return;
        }
#endif

        this.webCamTexture = new WebCamTexture();
        this.image.texture = this.webCamTexture;
        this.webCamTexture.Play();

        this.debugView.SetDebugParameter("webcam.width", this.webCamTexture.width);
        this.debugView.SetDebugParameter("webcam.height", this.webCamTexture.height);
    }

    private void Update()
    {
#if UNITY_IOS
        this.image.uvRect = new Rect(0f, 1f, 1f, -1f);
#endif

        if (this.webCamTexture != null && this.webCamTexture.didUpdateThisFrame)
        {
            this.updateCount++;
        }

        float duration = Time.realtimeSinceStartup - this.prevTime;
        if (duration >= 0.5f)
        {
            float frequency = this.updateCount / duration;
            this.updateCount = 0;
            this.prevTime = Time.realtimeSinceStartup;
            this.debugView.SetDebugParameter("fps (webcam)", frequency);
        }
    }

    public Texture2D GetTexture()
    {
        if (this.webCamTexture == null || !this.webCamTexture.isPlaying || this.webCamTexture.width < 100)
        {
            return null;
        }
        if (!this.webCamTexture.didUpdateThisFrame)
        {
            return null;
        }

        if (this.texture == null)
        {
            this.texture = Texture2D.CreateExternalTexture(
                this.webCamTexture.width,
                this.webCamTexture.height,
                TextureFormat.ARGB32,
                false,
                false,
                this.webCamTexture.GetNativeTexturePtr()
            );
        }
        else
        {
            this.texture.UpdateExternalTexture(this.webCamTexture.GetNativeTexturePtr());
        }
        if (this.renderTexture == null)
        {
            this.renderTexture = RenderTexture.GetTemporary(
                this.webCamTexture.width,
                this.webCamTexture.height,
                0,
                RenderTextureFormat.ARGB32
            );
        }

        var previousTexture = RenderTexture.active;
        RenderTexture.active = this.renderTexture;

#if !UNITY_IOS
        Graphics.Blit(this.webCamTexture, this.renderTexture);
#else
        Graphics.Blit(this.webCamTexture, this.renderTexture, new Vector2(1, -1), new Vector2(0, 1));
#endif

        RenderTexture.active = previousTexture;

        return this.texture;
    }

    private void OnDestroy()
    {
        if (this.webCamTexture != null && this.webCamTexture.isPlaying)
        {
            this.webCamTexture.Stop();
            this.webCamTexture = null;
        }

        if (this.texture != null)
        {
            Destroy(this.texture);
            this.texture = null;
        }

        if (this.renderTexture != null)
        {
            RenderTexture.ReleaseTemporary(this.renderTexture);
            this.renderTexture = null;
        }
    }
}
