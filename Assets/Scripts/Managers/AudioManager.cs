using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using System.Linq;
using Sirenix.Serialization;

public enum MusicType
{
    Roam,
    Battle,
    BossBattle,
    Victory,
    Defeat,
    Custom
}

[System.Serializable]
public class Music
{
    public MusicType type;
    public MMF_Player musicFeedback;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Title("Sound Control")]
    [SerializeField] MMSoundManager MMSoundManager;
    [Space(10)]
    [SerializeField] MMF_Player musicFadeInFeedback;
    [SerializeField] MMF_Player musicFadeOutFeedback;
    [Space(10)]
    [SerializeField] SFXPlayer sfxPlayer;
    [Title("Default Volumes")]
    [SerializeField] float defaultMusicVolume = 0.7f;
    [SerializeField] float defaultSFXVolume = 1;
    [SerializeField] float defaultUIVolume = 1;
    [Title("Music Feedbacks")]
    [ListDrawerSettings(Expanded = true)]
    [SerializeField] List<Music> music = new List<Music>();
    [SerializeField, HideInInspector]
    private AudioSettingsState settingsState = new AudioSettingsState();

    const string persistentDataKey = "AudioManager";

    MusicType currentMusicType;
    MMF_Player currentMusic = null;
    SavingLoadingManager savingManager;

    AudioClip customBossBattleMusic = null;
    float fadeOutDuration;

    Coroutine stoppingMusicRoutine = null;

    private void Awake()
    {
        Instance = this;
        savingManager = SavingLoadingManager.Instance;
        fadeOutDuration = musicFadeOutFeedback.GetFeedbackOfType<MMF_MMSoundManagerTrackFade>().FadeDuration;
    }

    private void Start()
    {
        SettingsUI.GameSettingsUpdated += SaveNewSettings;
        savingManager.DataAndSceneLoadComplete += SetRoamMusic;

        LoadVolumes();

#if UNITY_EDITOR
        if (!savingManager.LoadingEnabled && LevelGrid.Instance) //FOR TESTING
        {
            SetRoamMusic();

            if (FantasyCombatManager.Instance.InCombat()) { return; }

            //Play Roam Music
            PlayMusic(MusicType.Roam);
        }
#endif
        
    }

    private void SetRoamMusic()
    {
        SceneData sceneData = FindObjectOfType<SceneData>();
        MMF_Player roamMusic = music.First((item) => item.type == MusicType.Roam).musicFeedback;
        roamMusic.GetFeedbackOfType<MMF_MMSoundManagerSound>().Sfx = sceneData.sceneMusic;
    }

    public void PlayMusic(MusicType musicType)
    {
        if(musicType == MusicType.BossBattle)
        {
            PlayBossBattleMusic();
            return;
        }

        MMF_Player newMusic = music.First((item) => item.type == musicType).musicFeedback;
        PlayMusicFeedback(newMusic, musicType);
    }

    public void PlayBattleMusic(IBattleTrigger battleTrigger)
    {
        PlayMusic(battleTrigger.battleMusicType);
    }
    
    public void PlayCustomMusic(AudioClip musicToPlay)
    {
        MusicType musicType = MusicType.Custom;
        MMF_Player customMusic = music.First((item) => item.type == musicType).musicFeedback;
        customMusic.GetFeedbackOfType<MMF_MMSoundManagerSound>().Sfx = musicToPlay;

        PlayMusicFeedback(customMusic, musicType);
    }

    private void PlayBossBattleMusic()
    {
        MusicType musicType = MusicType.BossBattle;
        MMF_Player bossMusic = music.First((item) => item.type == musicType).musicFeedback;

        bossMusic.GetFeedbackOfType<MMF_MMSoundManagerSound>().Sfx = customBossBattleMusic;
        PlayMusicFeedback(bossMusic, musicType);
    }

    private void PlayMusicFeedback(MMF_Player musicFeedback, MusicType musicType)
    {
        if (currentMusic)
        {
            if(currentMusicType != musicType || stoppingMusicRoutine != null)
            {
                currentMusicType = musicType;
                StartCoroutine(PlayMusicRoutine(musicFeedback));
            }  
        }
        else
        {
            musicFadeInFeedback?.PlayFeedbacks();
            musicFeedback?.PlayFeedbacks();

            currentMusic = musicFeedback;
            currentMusicType = musicType;
        }
    }


    public void StopMusic()
    {
        if (!currentMusic) { return; }

        stoppingMusicRoutine = StartCoroutine(StopMusicRoutine());
    }

    public void SetBossBattleMusic(AudioClip audioClip)
    {
        customBossBattleMusic = audioClip;
    }

    IEnumerator PlayMusicRoutine(MMF_Player musicToPlay)
    {
        if(stoppingMusicRoutine != null)
        {
            StopCoroutine(stoppingMusicRoutine);
            stoppingMusicRoutine = null;
        }

        //Fade Out Current Music
        musicFadeOutFeedback?.PlayFeedbacks();
        yield return new WaitForSecondsRealtime(fadeOutDuration);

        //Stop Current Music
        currentMusic?.StopFeedbacks();

        //Fade In New Music.
        musicToPlay?.PlayFeedbacks();
        currentMusic = musicToPlay;
        musicFadeInFeedback?.PlayFeedbacks();
    }

    IEnumerator StopMusicRoutine()
    {
        musicFadeOutFeedback?.PlayFeedbacks();
        
        yield return new WaitForSecondsRealtime(fadeOutDuration);

        currentMusic?.StopFeedbacks();
        musicFadeInFeedback?.PlayFeedbacks(); // To Reset Volume.

        currentMusic = null;
        stoppingMusicRoutine = null;
    }

    public void PlaySFX(SFXType sfxToPlay)
    {
        sfxPlayer.PlaySFX(sfxToPlay);
    }

    private void OnDisable()
    {
        //SINCE YOU SUBSCRIBE IN START, NO REASON TO UNSUBSCRIBE IN ON DISABLE

        //SettingsUI.GameSettingsUpdated -= SaveNewSettings;
        //FantasyCombatManager.Instance.CombatBegun -= PlayBattleMusic;
    }

    public void SetVolume(MMSoundManager.MMSoundManagerTracks track, float volume)
    {
        MMSoundManager.SetTrackVolume(track, volume);

        if (track == MMSoundManager.MMSoundManagerTracks.Music)
            musicFadeInFeedback.GetFeedbackOfType<MMF_MMSoundManagerTrackFade>().FinalVolume = volume;
    }

    public float GetVolume(MMSoundManager.MMSoundManagerTracks track)
    {
        return MMSoundManager.GetTrackVolume(track, false);
    }

    //Saving & Loading

    [System.Serializable]
    public class AudioSettingsState
    {
        public float musicVolume;
        public float sfxVolume;
        public float uiVolume;
    }

    private void SaveNewSettings()
    {
        settingsState.musicVolume = GetVolume(MMSoundManager.MMSoundManagerTracks.Music);
        settingsState.sfxVolume = GetVolume(MMSoundManager.MMSoundManagerTracks.Sfx);
        settingsState.uiVolume = GetVolume(MMSoundManager.MMSoundManagerTracks.UI);

        savingManager.SavePersistentData(persistentDataKey, SerializationUtility.SerializeValue(settingsState, DataFormat.Binary));
    }

    private void LoadVolumes()
    {
        object loadedData = savingManager.LoadPersistentData(persistentDataKey);

        if (loadedData == null) 
        {
            SetVolume(MMSoundManager.MMSoundManagerTracks.Music, defaultMusicVolume);
            SetVolume(MMSoundManager.MMSoundManagerTracks.Sfx, defaultSFXVolume);
            SetVolume(MMSoundManager.MMSoundManagerTracks.UI, defaultUIVolume);
            return; 
        }

        byte[] bytes = loadedData as byte[];
        settingsState = SerializationUtility.DeserializeValue<AudioSettingsState>(bytes, DataFormat.Binary);

        SetVolume(MMSoundManager.MMSoundManagerTracks.Music, settingsState.musicVolume);
        SetVolume(MMSoundManager.MMSoundManagerTracks.Sfx, settingsState.sfxVolume);
        SetVolume(MMSoundManager.MMSoundManagerTracks.UI, settingsState.uiVolume);
    }

}
