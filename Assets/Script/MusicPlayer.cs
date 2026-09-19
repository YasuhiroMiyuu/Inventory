using UnityEngine;

namespace CaseFit
{
    /// Background music for one scene.
    /// Drop this on an empty GameObject, put your audio file in 'Track', done.
    ///
    /// Only one MusicPlayer is ever heard at a time. If a track with the same
    /// Track Id is already playing, this one steps aside instead of stacking a
    /// second copy on top - so no two scenes can ever double up.
    ///
    /// The volume slider in Settings still applies on top of this, because it
    /// drives AudioListener.volume for the whole game.
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class MusicPlayer : MonoBehaviour
    {
        [Header("Track")]
        [Tooltip("Drop your music file here (.mp3, .wav, .ogg).")]
        [SerializeField] AudioClip track;

        [Tooltip("Loudness of this track on its own. The Settings slider scales it further.")]
        [Range(0f, 1f)][SerializeField] float volume = 0.6f;

        [SerializeField] bool loop = true;

        [Header("Between Scenes")]
        [Tooltip("On: the music keeps playing when the scene changes, instead of restarting. " +
                 "Give every scene that shares this track the same Track Id.")]
        [SerializeField] bool keepPlayingBetweenScenes = true;

        [Tooltip("Scenes with a matching id keep the current track playing. A different id swaps the track.")]
        [SerializeField] string trackId = "bgm";

        static MusicPlayer current;

        AudioSource source;

        public bool IsPlaying => source != null && source.isPlaying;
        public string TrackId => trackId;

        void Awake()
        {
            source = GetComponent<AudioSource>();

            // Someone may have dropped the clip straight onto the AudioSource instead.
            if (track == null && source.clip != null) track = source.clip;

            // Kill any automatic start, otherwise 'Play On Awake' gives us a second voice.
            source.playOnAwake = false;
            source.Stop();

            source.clip = track;
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = 0f;

            WarnAboutSiblings();

            if (current != null && current != this)
            {
                // Same track already running anywhere in the game - do not stack on top of it.
                if (current.trackId == trackId && current.IsPlaying)
                {
                    Destroy(gameObject);
                    return;
                }

                // Different track - the old one hands over.
                current.StopAndRemove();
            }

            current = this;

            if (keepPlayingBetweenScenes)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            Play();
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }

        void Play()
        {
            if (track == null)
            {
                Debug.LogWarning("[Case Fit] MusicPlayer has no track yet. Drop an audio file into the 'Track' field.", this);
                return;
            }

            source.Play();
        }

        void StopAndRemove()
        {
            if (source != null) source.Stop();
            if (current == this) current = null;
            Destroy(gameObject);
        }

        void WarnAboutSiblings()
        {
            MusicPlayer[] all = FindObjectsByType<MusicPlayer>(FindObjectsSortMode.None);
            int inThisScene = 0;

            foreach (MusicPlayer player in all)
                if (player.gameObject.scene == gameObject.scene) inThisScene++;

            if (inThisScene > 1)
                Debug.LogWarning($"[Case Fit] This scene has {inThisScene} Music objects. " +
                                 "Keep one and delete the rest.", this);
        }

        public void SetVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            if (source != null) source.volume = volume;
        }

        public void Stop()
        {
            if (source != null) source.Stop();
        }

        void OnValidate()
        {
            if (source == null) source = GetComponent<AudioSource>();
            if (source == null) return;

            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume;
        }
    }
}
