using UnityEngine;
using UnityEngine.Audio;
namespace TankAttack
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class VehicleAudio : MonoBehaviour
    {
        public AudioClip engineLoop, turretLoop, destructionClip;
        public EngineAudioSettings engineSettings;
        public AudioMixerGroup mixerGroup;
        [Range(0, 1)] public float engineVolume = .25f;
        AudioSource effects, engine, turret;
        bool playerEngine;

        void Awake()
        {
            effects = GetComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 1;
            effects.maxDistance = 65;
            effects.outputAudioMixerGroup = mixerGroup;
            playerEngine = GetComponent<Damageable>() != null && GetComponent<Damageable>().team == Team.Player;
            if (!playerEngine) engine = Loop(engineLoop);
            turret = Loop(turretLoop);
        }

        void OnEnable()
        {
            if(playerEngine) CrossfadedEngineAudio.Instance.Bind(this,engineSettings,engineLoop,mixerGroup,engineVolume);
        }
        void OnDisable()
        {
            if(playerEngine) CrossfadedEngineAudio.Existing?.Release(this);
        }
        AudioSource Loop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = true; source.clip = clip;
            source.spatialBlend = 1; source.maxDistance = 65;
            source.outputAudioMixerGroup = mixerGroup; source.volume = 0;
            if (clip != null) source.Play();
            return source;
        }

        public void SetMotion(float drive, float turn)
        {
            if(playerEngine) CrossfadedEngineAudio.Existing?.SetDrive(this,drive);
            else if(engine!=null)
            {
                engine.volume = engineVolume * Mathf.Lerp(.3f,1,Mathf.Abs(drive));
                engine.pitch = Mathf.Lerp(.7f,1.3f,Mathf.Abs(drive));
            }
            turret.volume = Mathf.Clamp01(turn) * .2f;
        }

        public void Play(AudioClip clip, float volume = 1)
        { if (clip != null) effects.PlayOneShot(clip, volume); }

        public void DestroySound() => At(destructionClip, transform.position, 1, mixerGroup);
        public void StopLoops()
        {
            if (engine != null) engine.Stop();
            if (turret != null) turret.Stop();
            if (playerEngine) CrossfadedEngineAudio.Existing?.Release(this);
        }

        public static void At(AudioClip clip, Vector3 position, float volume, AudioMixerGroup group = null)
        {
            if (clip == null) return;
            var go = new GameObject("One-shot audio"); go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip; source.spatialBlend = 1; source.volume = volume;
            source.maxDistance = 65; source.outputAudioMixerGroup = group; source.Play();
            Destroy(go, clip.length + .1f);
        }
    }
}
