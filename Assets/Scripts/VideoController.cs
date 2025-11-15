using UnityEngine;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    [SerializeField] private double loopPoint;

    private VideoPlayer videoPlayer;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.isLooping = false;
    }

    private void Update()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            videoPlayer.time = loopPoint;
        }
    }
}
