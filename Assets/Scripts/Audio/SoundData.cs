using System;
using UnityEngine;
using UnityEngine.Audio;

namespace ChasingLight
{

    [Serializable]
    public class SoundData
    {
        [Tooltip("Sound ID")]
        public SoundID id;

        [Tooltip("List of clips")]
        public AudioClip[] clips;

        [Range(0f, 1f)]
        [Tooltip("Base volume of this sound")]
        public float volume = 1f;

        [Range(0.5f, 1.5f)]
        [Tooltip("Max pitch when playing sound")]
        public float minPitch = 0.95f;

        [Range(0.5f, 1.5f)]
        [Tooltip("Max pitch when playing this sound")]
        public float maxPitch = 1.05f;

        [Tooltip("Mixer group")]
        public AudioMixerGroup mixerGroup;

        [Tooltip("Cooldown between two sounds (same Sound ID)")]
        public float cooldown = 0.05f;

        [NonSerialized]
        public float lastPlayedTime = -1f;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }
}
