using UnityEngine;
using UnityEngine.Video;


// VideoPlayer의 PlayOnAwake 대신 이 스크립트를 사용하세요.
// Awake()에서 Prepare()를 호출해 최대한 빨리 버퍼링을 시작하고,
// 준비가 완료되는 즉시 Play()합니다.
[RequireComponent(typeof(VideoPlayer))]
public class TitleVideoController : MonoBehaviour
{
    private VideoPlayer _videoPlayer;

    private void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();

        // PlayOnAwake는 반드시 false로 두고 이 스크립트가 직접 제어합니다.
        _videoPlayer.playOnAwake = false;

        _videoPlayer.prepareCompleted += OnPrepareCompleted;
        _videoPlayer.Prepare();
    }

    private void OnPrepareCompleted(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPrepareCompleted;
        vp.Play();
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
            _videoPlayer.prepareCompleted -= OnPrepareCompleted;
    }
}
