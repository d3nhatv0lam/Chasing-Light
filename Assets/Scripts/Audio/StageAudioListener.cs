using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChasingLight
{
    public sealed class StageAudioListener : MonoBehaviour
    {
        private void Start()
        {
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Gameplay);
        }

        public void PlayStepSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_EnterStone);
        }

        public void PlayTeleportSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_Teleport);
        }

        public void PlayCharAppearanceSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_CharAppearance);
        }

        public void PlayDieSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_Die);
        }

        public void PlayGoalReachedSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_Victory);
        }

        public void PlayMechanismChanges(IEnumerable<ISimulationEvent> mechEvents)
        {
            if (mechEvents == null) return;

            if (mechEvents.Any(evt => evt is SwitchToggledEvent))
            {
                AudioManager.Instance?.PlaySFX(SoundID.SFX_Switch);
            }
        }
    }
}
