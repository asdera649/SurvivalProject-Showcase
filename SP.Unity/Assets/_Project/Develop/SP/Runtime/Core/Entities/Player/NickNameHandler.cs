using Mirror;
using TMPro;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Player
{
    [RequireComponent(typeof(TextMeshPro))]
    public class NickNameHandler : MonoBehaviour
    {
        private enum Target
        {
            Camera,
            LocalPlayer
        }
        
        [Header("Settings")] 
        [SerializeField] private float _hidingDistance = 10;
        [SerializeField] private Target _target = Target.LocalPlayer;
        [SerializeField] private Vector3 _lineCastStartOffset = new(0, 1, 0);
        [SerializeField] private Vector3 _lineCastEndOffset = new(0, -1, 0);
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float _updateRate = 0.25f;

        private TextMeshPro _textMeshPro;
        private TextMeshPro TextMeshPro
        {
            get
            {
                if (_textMeshPro == null)
                {
                    _textMeshPro = GetComponent<TextMeshPro>();
                }

                return _textMeshPro;
            }
        }
        
        private void Start()
        {
            InvokeRepeating(nameof(UpdateHiding), 0, _updateRate);
        }
        
        private void OnDestroy()
        {
            CancelInvoke(nameof(UpdateHiding));
        }

        private void UpdateHiding()
        {
            Transform customStart = null;

            if (_target == Target.LocalPlayer)
            {
                if (NetworkClient.localPlayer != null)
                {
                    customStart = NetworkClient.localPlayer.transform;
                }
            }
            else
            {
                if (UnityEngine.Camera.main != null)
                {
                    customStart = UnityEngine.Camera.main.transform;
                }
            }
            
            if (customStart != null)
            {
                if (Vector3.Distance(
                        customStart.position + _lineCastStartOffset,
                        transform.position + _lineCastEndOffset) > _hidingDistance)
                {
                    TextMeshPro.enabled = false;
                    return;
                }

                if (Physics.Linecast(
                        customStart.position + _lineCastStartOffset,
                        transform.position + _lineCastEndOffset,
                        _layerMask,
                        QueryTriggerInteraction.Ignore))
                {
                    TextMeshPro.enabled = false;
                    return;
                }
            }

            TextMeshPro.enabled = true;
        }

        public void SetNickName(string nickName)
        {
            TextMeshPro.text = nickName;
        }
    }
}
