using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace WorldTest
{
    public class Sound
    {
        #region Fields

        //Sound stuff!
        static AudioEngine audioEngine;
        static WaveBank waveBank;
        static protected SoundBank soundBank;

        // 3D audio objects
        AudioEmitter emitter = new AudioEmitter();
        AudioListener listener = new AudioListener();
        Cue cue;
        Cue cue2;

        #endregion

        #region Dictionary



        #endregion

        #region Initialization

        /// <summary>
        /// Load audio content in the constructor
        /// </summary>
        public Sound()
        {
            audioEngine = new AudioEngine("Content/gameAudio.xgs");
            waveBank = new WaveBank(audioEngine, "Content/Wave Bank.xwb");
            soundBank = new SoundBank(audioEngine, "Content/Sound Bank.xsb");
        }

        /// <summary>
        /// Play an audio asset by getting the sound cue associated with 
        /// the passed in string name
        /// </summary>
        /// <param name="sound"></param>
        public virtual void Play(String sound)
        {
            //Get the cue and play it.
            //For 3D cues, you must call Apply3D before calling Play.
            cue = soundBank.GetCue(sound);
            cue.Apply3D(listener, emitter);
            cue.Play();
        }

        /// <summary>
        /// Play an background music asset by getting the sound cue associated with 
        /// the passed in string name
        /// </summary>
        /// <param name="sound"></param>
        public virtual void PlayMusic(String sound)
        {
            // Get the cue and play it.
            // For 3D cues, you must call Apply3D before calling Play.
            if (cue2 != null && cue2.IsPlaying)
            {
                cue2.Stop(AudioStopOptions.AsAuthored);
            }
            cue2 = soundBank.GetCue(sound);
            cue2.Apply3D(listener, emitter);
            cue2.Play();
        }

        public virtual void StopMusic(String sound)
        {
            if (cue2 != null)
            {
                cue2.Stop(AudioStopOptions.AsAuthored);
            }
        }

        public virtual void Stop(String sound)
        {
            cue.Stop(AudioStopOptions.AsAuthored);
        }

        /// <summary>
        /// Load graphics content for the screen.
        /// </summary>
        public virtual void LoadContent() { }


        /// <summary>
        /// Unload content for the screen.
        /// </summary>
        public virtual void UnloadContent() { }

        #endregion

    }
}
