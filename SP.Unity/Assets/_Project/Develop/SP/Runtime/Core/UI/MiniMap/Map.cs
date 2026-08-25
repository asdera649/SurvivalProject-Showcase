using SP.Runtime.Core.UI.MiniMap.Input;
using UnityEngine;
using UnityEngine.UI;
using Mask = SP.Runtime.Core.UI.Utilities.Mask;

namespace SP.Runtime.Core.UI.MiniMap
{
    [RequireComponent(typeof(Image), typeof(BaseMapInput), typeof(Mask))]
    public class Map : MonoBehaviour
    {
        private Image _image;
        public Image Image
        {
            get
            {
                if (_image == null)
                {
                    _image = GetComponent<Image>();
                }
                
                return _image;
            }
        }
        
        private BaseMapInput _mapInput;
        public BaseMapInput MapInput
        {
            get
            {
                if (_mapInput == null)
                {
                    _mapInput = GetComponent<BaseMapInput>();
                }
                
                return _mapInput;
            }
        }
        
        private Mask _mask;
        public Mask Mask
        {
            get
            {
                if (_mask == null)
                {
                    _mask = GetComponent<Mask>();
                }
                
                return _mask;
            }
        }
    }
}
