using SP.Runtime.Meta.UI.MainMenu.Blocks;
using SP.Runtime.Utilities.SerializableDictionary;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Meta.UI.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SerializableDictionary<BaseBlock, Button> _blocks = new();
        
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _exitNoButton;
        [SerializeField] private Button _exitYesButton;
        [SerializeField] private GameObject _exitBlock;
        [SerializeField] private Animation _exitBlockAnimation;
        
        private BaseBlock _activeBlock;
        public BaseBlock ActiveBlock
        {
            set
            {
                var oldValue = _activeBlock;
                _activeBlock = value;

                if (oldValue != _activeBlock)
                {
                    OnActiveBlockUpdate(oldValue, _activeBlock);
                }
            }
        }

        private void OnEnable()
        {
            foreach (var b in _blocks)
            {
                b.Value.onClick.AddListener(() => 
                { 
                    ActiveBlock = b.Key;
                });
            }
            
            _exitButton.onClick.AddListener(OnExitButtonClick);
            _exitNoButton.onClick.AddListener(OnExitNoButtonClick);
            _exitYesButton.onClick.AddListener(OnExitYesButtonClick);
        }

        private void OnDisable()
        {
            foreach (var b in _blocks)
            {
                b.Value.onClick.RemoveAllListeners();
            }
            
            _exitButton.onClick.RemoveAllListeners();
            _exitNoButton.onClick.RemoveAllListeners();
            _exitYesButton.onClick.RemoveAllListeners();
        }
        
        private void OnExitButtonClick()
        {
            _exitBlock.SetActive(true);
            _exitBlockAnimation.Play();
        }

        private void OnExitNoButtonClick()
        {
            _exitBlock.SetActive(false);
        }

        private void OnExitYesButtonClick()
        {
            Application.Quit();
        }

        private void OnActiveBlockUpdate(BaseBlock oldBlock, BaseBlock newBlock)
        {
            if (oldBlock != null)
            {
                oldBlock.SetView(false);

                _blocks[oldBlock].interactable = true;
            }

            if (newBlock != null)
            {
                newBlock.SetView(true);
                
                _blocks[newBlock].interactable = false;
            }
        }
    }
}
