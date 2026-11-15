using UnityEngine;
using UnityEngine.Audio;
namespace TankAttack
{
    public sealed class CrossfadedEngineAudio : MonoBehaviour
    {
        static CrossfadedEngineAudio instance;
        public static CrossfadedEngineAudio Existing => instance;
        public static CrossfadedEngineAudio Instance
        {
            get
            {
                if (instance == null) new GameObject("Persistent Engine Audio").AddComponent<CrossfadedEngineAudio>();
                return instance;
            }
        }
        public Object Owner { get; private set; }
        public bool IsLooping { get; private set; }
        public bool IsOverlapping
        {
            get { double now=AudioSettings.dspTime; return IsLooping && now>=starts[0] && now<starts[0]+length && now>=starts[1] && now<starts[1]+length; }
        }
        public float CurrentLevel => envelope;
        readonly AudioSource[] voices = new AudioSource[2];
        readonly double[] starts = new double[2];
        AudioClip clip;
        double length, overlap, period;
        float volume=.25f, idle=.35f, fadeIn=.35f, fadeOut=.45f, drive, envelope;
        bool requested;
        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance=this; DontDestroyOnLoad(gameObject);
            for(int i=0;i<2;i++)
            {
                voices[i]=gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake=false;
                voices[i].loop=false; voices[i].spatialBlend=0; voices[i].volume=0;
            }
            AudioSettings.OnAudioConfigurationChanged += AudioDeviceChanged;
        }
        public void Bind(Object owner,EngineAudioSettings settings,AudioClip fallback=null,AudioMixerGroup fallbackMixer=null,float fallbackVolume=.25f)
        {
            Owner=owner; requested=owner!=null; drive=0;
            var nextClip=settings!=null && settings.rumbleClip!=null ? settings.rumbleClip : fallback;
            volume=settings!=null ? settings.volume : fallbackVolume;
            idle=settings!=null ? settings.idleVolume : .35f;
            fadeIn=settings!=null ? Mathf.Max(.01f,settings.fadeInSeconds) : .35f;
            fadeOut=settings!=null ? Mathf.Max(.01f,settings.fadeOutSeconds) : .45f;
            foreach(var voice in voices) voice.outputAudioMixerGroup=settings!=null && settings.mixerGroup!=null ? settings.mixerGroup : fallbackMixer;
            double nextLength=nextClip!=null ? nextClip.length : 0;
            double nextOverlap=nextLength>0 ? Mathf.Clamp(settings!=null ? settings.overlapSeconds : .3f,.001f,(float)nextLength*.45f) : 0;
            if(clip==nextClip && IsLooping && System.Math.Abs(overlap-nextOverlap)<.0001) return;
            StopVoices(); clip=nextClip; length=nextLength; overlap=nextOverlap; period=length-overlap; envelope=0;
            if(clip!=null && clip.loadState==AudioDataLoadState.Unloaded) clip.LoadAudioData();
            BeginIfReady();
        }
        public void SetDrive(Object owner,float amount) { if(Owner==owner) drive=Mathf.Clamp01(Mathf.Abs(amount)); }
        public void Release(Object owner) { if(Owner==owner) { Owner=null; requested=false; } }
        public void StopEngine() { Owner=null; requested=false; }
        void BeginIfReady()
        {
            if(IsLooping || !requested || clip==null || length<=0 || clip.loadState!=AudioDataLoadState.Loaded) return;
            double first=AudioSettings.dspTime+.08;
            for(int i=0;i<2;i++) { voices[i].clip=clip; Schedule(i,first+i*period); }
            IsLooping=true;
        }
        void Schedule(int index,double start)
        {
            starts[index]=start; voices[index].volume=0;
            voices[index].PlayScheduled(start); voices[index].SetScheduledEndTime(start+length);
        }
        void Update()
        {
            if(Owner==null || (BattleSession.Instance!=null && !BattleSession.IsRunning)) requested=false;
            BeginIfReady();
            float target=requested && clip!=null ? 1 : 0;
            envelope=Mathf.MoveTowards(envelope,target,Time.unscaledDeltaTime/(target>envelope ? fadeIn : fadeOut));
            if(!requested && envelope<=0) { StopVoices(); return; }
            if(!IsLooping) return;
            double now=AudioSettings.dspTime;
            float level=volume*Mathf.Lerp(idle,1,drive)*envelope;
            for(int i=0;i<2;i++)
            {
                if(now>=starts[i]+length) Schedule(i,System.Math.Max(starts[i]+period*2,now+.03));
                float into=(float)((now-starts[i])/overlap), remaining=(float)((starts[i]+length-now)/overlap);
                float phase=Mathf.Clamp01(Mathf.Min(into,remaining));
                voices[i].volume=level*Mathf.Sin(phase*Mathf.PI*.5f);
            }
        }
        void StopVoices() { foreach(var voice in voices) if(voice!=null) voice.Stop(); IsLooping=false; }
        void AudioDeviceChanged(bool changed) { StopVoices(); BeginIfReady(); }
        void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged -= AudioDeviceChanged;
            if(instance==this) instance=null;
        }
    }
}
