using UnityEngine;
using UnityEngine.Audio;

public class BGMPlayer : MonoBehaviour
{
    private static BGMPlayer _instance;

    [Header("BGM")]
    public AudioClip bgm;                           // 拖你的背景音乐
    [Range(0f,1f)] public float volume = 0.6f;      // 音量
    public bool loop = true;                        // 是否循环
    public bool playOnStart = true;                 // 进入游戏就播放
    public AudioMixerGroup outputMixerGroup;        // 可选：路由到你的 Music 组

    private AudioSource _src;

    private void Awake()
    {
        // 单例 + 常驻
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 配置 AudioSource（2D 声音，避免位置影响）
        _src = GetComponent<AudioSource>();
        if (_src == null) _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = loop;
        _src.spatialBlend = 0f; // 2D
        _src.volume = volume;
        if (outputMixerGroup) _src.outputAudioMixerGroup = outputMixerGroup;

        if (playOnStart && bgm)
        {
            _src.clip = bgm;
            _src.Play();
        }
    }

    // 如有需要，外部也可以手动开始/停止
    public void Play()
    {
        if (bgm && _src.clip != bgm) _src.clip = bgm;
        if (!_src.isPlaying) _src.Play();
    }

    public void Stop()
    {
        if (_src.isPlaying) _src.Stop();
    }
}
