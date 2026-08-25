using FMODUnity;
using UnityEngine;

namespace SP.Runtime.Core.UI.Notification
{
    #region Structs
    
    public enum NotificationType
    {
        Neutral,
        Warning,
        Info
    }
    
    #endregion

    public class NotificationBlock : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private NotificationElement _notificationElementPrefab;

        [Header("References")] 
        [SerializeField] private StudioEventEmitter _errorEventEmitter;
        
        [Header("Settings")]
        [SerializeField] private GameObject _layoutGroup;

        public NotificationElement AddNotification(string text, NotificationType notificationType, bool toFix = false)
        {
            var output = Instantiate(_notificationElementPrefab, _layoutGroup.transform);

            for (var i = 0; i < _layoutGroup.transform.childCount; i++)
            {
                if (_layoutGroup.transform.GetChild(i).GetComponent<NotificationElement>().IsFixed)
                {
                    output.transform.SetSiblingIndex(i);
                    break;
                }
            }

            output.Initialize(text, notificationType, toFix);

            if (notificationType == NotificationType.Warning)
            {
                _errorEventEmitter.Play();
            }
            
            return output;
        }

        public void SetView(bool value)
        {
            transform.position = new Vector3(
                transform.position.x, 
                value ? transform.position.y - 200 : transform.position.y + 200,
                0);
        }
    }
}