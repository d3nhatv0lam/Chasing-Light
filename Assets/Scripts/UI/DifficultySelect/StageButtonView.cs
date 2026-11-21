using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Component View quản lý hiển thị cho từng nút màn chơi (Stage Item) trong danh sách chọn màn.
    /// Quản lý trạng thái khóa (LockOverlay) và tiến trình số sao (Star0 - Star3).
    /// </summary>
    public class StageButtonView : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        [Header("Lock Status")]
        [SerializeField] private GameObject _lockOverlay;

        [Header("Stars Display")]
        [SerializeField] private GameObject _starsContainer;
        [SerializeField] private GameObject _star0;
        [SerializeField] private GameObject _star1;
        [SerializeField] private GameObject _star2;
        [SerializeField] private GameObject _star3;

        public void Setup(StageAsset stage, bool isUnlocked, int stars, Action onClick)
        {
            _label.text = $"Stage {stage.StageNumber}";
            _button.interactable = isUnlocked;

            _lockOverlay.SetActive(!isUnlocked);
            _starsContainer.SetActive(isUnlocked);

            if (isUnlocked)
            {
                _star0.SetActive(stars == 0);
                _star1.SetActive(stars == 1);
                _star2.SetActive(stars == 2);
                _star3.SetActive(stars == 3);
            }

            _button.onClick.RemoveAllListeners();
            if (isUnlocked && onClick != null)
            {
                _button.onClick.AddListener(() => onClick());
            }
        }
    }
}
