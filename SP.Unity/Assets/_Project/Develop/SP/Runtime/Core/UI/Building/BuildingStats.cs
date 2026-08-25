using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Building
{
    public class BuildingStats : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private GameObject _protectionBlock;

        public void Initialize(int value, int maxValue, bool isProtected)
        {
            _healthBar.value = (float)value / maxValue;
            _healthText.text = value.ToString() + "/" + maxValue.ToString();
            _protectionBlock.SetActive(isProtected);
        }
    }
}
