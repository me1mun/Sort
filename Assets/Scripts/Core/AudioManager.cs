using System;
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound  Banks")]
    [SerializeField] private Sound[] musicTracks;
    [SerializeField] private Sound[] soundEffects;

    private Dictionary<string, Sound> _musicTracksDict;
    private Dictionary<string, Sound> _soundEffectsDict;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicTracksDict = new Dictionary<string, Sound>();
        foreach (var track in musicTracks)
        {
            track.source = musicSource;
            _musicTracksDict[track.name] = track;
        }

        _soundEffectsDict = new Dictionary<string, Sound>();
        foreach (var sfx in soundEffects)
        {
            _soundEffectsDict[sfx.name] = sfx;
        }
    }

    public void PlayMusic(string name)
    {
        if (_musicTracksDict.TryGetValue(name, out Sound track))
        {
            musicSource.clip = track.clip;
            musicSource.loop = track.loop;
            musicSource.volume = track.volume;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"Music track with name '{name}' not found!");
        }
    }

    // Стандартный метод для проигрывания звука
    public void Play(string name)
    {
        if (_soundEffectsDict.TryGetValue(name, out Sound sfx))
        {
            sfxSource.PlayOneShot(sfx.clip, sfx.volume);
        }
        else
        {
            Debug.LogWarning($"Sound effect with name '{name}' not found!");
        }
    }

    public void PlayWithPitch(string name, float pitch)
    {
        if (_soundEffectsDict.TryGetValue(name, out Sound sfx))
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(sfx.clip, sfx.volume);
        }
        else
        {
            Debug.LogWarning($"Sound effect with name '{name}' not found!");
        }
    }

    public void SetMusicVolume(float volume)
    {
        if(musicSource != null)
            musicSource.volume = volume;
    }

    public void SetSoundVolume(float volume)
    {
        if(sfxSource != null)
            sfxSource.volume = volume;
    }
}