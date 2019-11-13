using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scenes.scripts
{
    public class LoadAudioManager : MonoBehaviour
    {
        public Dictionary<string, AudioClip> playerClips;

        private void Awake()
        {
            playerClips = new Dictionary<string, AudioClip>();
            loadAllAudio();
        }

        private void loadAllAudio()
        {
            var audioClips = LoadManager.Single.loadAll<AudioClip>("Audio/Player/");
            foreach (var clip in audioClips)
            {
                playerClips[clip.name] = clip;
            }
        }
    }

}