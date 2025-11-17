using UnityEngine;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    [SerializeField] private double loopPoint;

    [SerializeField] private VideoClip localSource;

    [SerializeField] private string webSource;

    private bool isPlaying = false;

    private VideoPlayer videoPlayer;

    private float updateInterval = 1f;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.isLooping = false;

#if UNITY_EDITOR
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = localSource;
#else
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = webSource;
#endif
    }

    public void Play()
    {
        videoPlayer.Play();
        videoPlayer.time = 0;
        isPlaying = true;
    }

    private void Update()
    {
        if (isPlaying && !videoPlayer.isPlaying)
        {
            if (updateInterval > 0)
            {
                updateInterval -= Time.deltaTime;
                return;
            }

            videoPlayer.Play();
            videoPlayer.time = loopPoint;
        }
    }
}
