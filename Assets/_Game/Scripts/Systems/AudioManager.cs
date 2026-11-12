using System;
using System.Collections.Generic;
using UnityEngine;

namespace RongRanChamCham
{
    public enum SoundType
    {
        Pickup,
        ShieldBlock,
        Whoosh,
        Victory,
        Defeat,
        ChamBeat,
        Dodge,
        UIClick
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("BGM Clips")]
        [SerializeField] private AudioClip bgmGameplay;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip sfxPickup;
        [SerializeField] private AudioClip sfxShieldBlock;
        [SerializeField] private AudioClip sfxWhoosh;
        [SerializeField] private AudioClip sfxVictory;
        [SerializeField] private AudioClip sfxDefeat;
        [SerializeField] private AudioClip sfxChamBeat;
        [SerializeField] private AudioClip sfxDodge;
        [SerializeField] private AudioClip sfxUIClick;

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AudioListener.volume = 1.0f;
            AudioListener.pause = false;

            EnsureAudioSources();
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (bgmGameplay == null) bgmGameplay = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/BGM/BGM_Gameplay.wav");
            if (sfxPickup == null) sfxPickup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Pickup.wav");
            if (sfxShieldBlock == null) sfxShieldBlock = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Shield_Block.wav");
            if (sfxWhoosh == null) sfxWhoosh = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Whoosh.wav");
            if (sfxVictory == null) sfxVictory = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Victory.wav");
            if (sfxDefeat == null) sfxDefeat = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Defeat.wav");
            if (sfxChamBeat == null) sfxChamBeat = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Cham_Beat.wav");
            if (sfxDodge == null) sfxDodge = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Dodge.wav");
            if (sfxUIClick == null) sfxUIClick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_UI_Click.wav");
#endif
            PlayGameplayBGM();
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.spatialBlend = 0f; // 2D flat audio
            }
            bgmSource.volume = bgmVolume;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D flat audio
            }
            sfxSource.volume = sfxVolume;
        }

        public void PlayGameplayBGM()
        {
            if (bgmSource == null) EnsureAudioSources();
            if (bgmGameplay != null)
            {
                if (bgmSource.clip == bgmGameplay && bgmSource.isPlaying) return;
                bgmSource.clip = bgmGameplay;
                bgmSource.volume = bgmVolume;
                bgmSource.Play();
            }
        }

        public void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        public void PauseBGM()
        {
            if (bgmSource != null && bgmSource.isPlaying) bgmSource.Pause();
        }

        public void ResumeBGM()
        {
            if (bgmSource != null && !bgmSource.isPlaying) bgmSource.UnPause();
        }

        public void PlaySFX(SoundType sound, float volumeScale = 1f)
        {
            if (sfxSource == null) EnsureAudioSources();
            AudioClip clip = GetClipForSound(sound);
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
            }
        }

        private AudioClip GetClipForSound(SoundType sound)
        {
            switch (sound)
            {
                case SoundType.Pickup: return sfxPickup;
                case SoundType.ShieldBlock: return sfxShieldBlock;
                case SoundType.Whoosh: return sfxWhoosh;
                case SoundType.Victory: return sfxVictory;
                case SoundType.Defeat: return sfxDefeat;
                case SoundType.ChamBeat: return sfxChamBeat;
                case SoundType.Dodge: return sfxDodge;
                case SoundType.UIClick: return sfxUIClick;
                default: return null;
            }
        }
    }
}
