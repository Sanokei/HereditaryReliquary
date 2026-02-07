using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    public class SoundFeedback : MonoBehaviour
    {
        [SerializeField]
        private AudioClip clickSound, placeSound, removeSound, wrongPlacementSound;

        [SerializeField]
        private AudioSource audioSource;

        public void PlaySound(SoundType soundType)
        {
            if (audioSource == null)
                return;
            
            AudioClip clipToPlay = null;
            switch (soundType)
            {
                case SoundType.Click:
                    clipToPlay = clickSound;
                    break;
                case SoundType.Place:
                    clipToPlay = placeSound;
                    break;
                case SoundType.Remove:
                    clipToPlay = removeSound;
                    break;
                case SoundType.WrongPlacement:
                    clipToPlay = wrongPlacementSound;
                    break;
                default:
                    break;
            }
            
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }
    }

    public enum SoundType
    {
        Click,
        Place,
        Remove,
        WrongPlacement
    }
}