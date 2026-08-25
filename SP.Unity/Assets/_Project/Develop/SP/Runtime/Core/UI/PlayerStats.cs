using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Animator _healthBarAnimation;
        [SerializeField] private Image _healthBar;
        [SerializeField] private Image _damageBar;
        [SerializeField] private TMP_Text _healthText;

        [Header("Settings")] 
        [SerializeField] private float _damageBarFadeTime = 0.5f;

        private float _time;
        
        private static readonly int _health = Animator.StringToHash("Health");
        private static readonly int _takeDamage = Animator.StringToHash("TakeDamage");

        private void Update()
        {
            _time -= Time.deltaTime;

            if (_time <= 0)
            {
                _damageBar.fillAmount = Mathf.Lerp(_damageBar.fillAmount, 0, Time.deltaTime);
            }
        }

        public void SetHealth(int value, int maxValue)
        {
            _healthBarAnimation.SetFloat(_health, value);
            _healthBarAnimation.ResetTrigger(_takeDamage);
            _healthBarAnimation.SetTrigger(_takeDamage);
            
            _damageBar.fillAmount = _healthBar.fillAmount;
            
            _healthBar.fillAmount = (float)value / maxValue;
            _healthText.text = value.ToString();

            _time = _damageBarFadeTime;
        }
    }
}
