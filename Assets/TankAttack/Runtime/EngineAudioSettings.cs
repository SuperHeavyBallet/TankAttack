using UnityEngine;
using UnityEngine.Audio;
namespace TankAttack
{
    [CreateAssetMenu(menuName = "Tank Attack/Engine Audio")]
    public sealed class EngineAudioSettings : ScriptableObject
    {
        public AudioClip rumbleClip;
        public AudioMixerGroup mixerGroup;
        [Range(0,1)] public float volume = .25f;
        [Range(0,1)] public float idleVolume = .35f;
        [Tooltip("End and beginning of the clip overlap using two scheduled audio sources. Clamped to at most 45% of clip duration.")]
        [Min(.01f)] public float overlapSeconds = .3f;
        [Min(.01f)] public float fadeInSeconds = .35f;
        [Min(.01f)] public float fadeOutSeconds = .45f;
    }
}
