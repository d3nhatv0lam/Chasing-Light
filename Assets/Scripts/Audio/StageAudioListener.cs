using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Cầu nối âm thanh cho màn chơi bàn cờ (Game.unity).
    /// Lắng nghe các diễn hoạt từ StageController và gọi AudioManager phát SFX tương ứng.
    /// </summary>
    public sealed class StageAudioListener : MonoBehaviour
    {
        private void Start()
        {
            // Bắt đầu màn chơi: Tự động chuyển BGM sang BGM_Gameplay
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Gameplay);
        }

        /// <summary>Phát tiếng bước chân trên nền đá khi thực thể di chuyển (SFX_Normal)</summary>
        public void PlayStepSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_EnterStone);
        }

        /// <summary>Phát tiếng qua cổng dịch chuyển Teleport (SFX_Normal)</summary>
        public void PlayTeleportSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_Teleport);
        }

        /// <summary>Phát tiếng Shadow xuất hiện (SFX_Major - Kích hoạt Ducking BGM)</summary>
        public void PlayCharAppearanceSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_CharAppearance);
        }

        /// <summary>Phát tiếng nhân vật chết / chạm bóng tối (SFX_Major - Kích hoạt Ducking BGM)</summary>
        public void PlayDieSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_Die);
        }

        /// <summary>Phát tiếng chiến thắng khi vừa chạm ô Đích (SFX_Major - Kích hoạt Ducking BGM)</summary>
        public void PlayGoalReachedSound()
        {
            AudioManager.Instance?.PlaySFX(SoundID.SFX_Victory);
        }

        /// <summary>
        /// Phát tiếng cơ quan công tắc thay đổi trạng thái (SFX_Normal).
        /// Đảm bảo dù Player và nhiều Shadow cùng đạp nhiều switch trong 1 lượt thì âm thanh chỉ phát đúng 1 lần.
        /// </summary>
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
