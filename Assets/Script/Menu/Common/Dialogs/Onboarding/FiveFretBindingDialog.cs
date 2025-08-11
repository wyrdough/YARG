using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Dialogs
{
    public class FiveFretBindingDialog : InputBindingDialog
    {
        [SerializeField]
        private FiveFretBindingImages _images;

        protected override void OnEnable()
        {
            _images.GreenFret.gameObject.SetActive(false);
            _images.RedFret.gameObject.SetActive(false);
            _images.YellowFret.gameObject.SetActive(false);
            _images.BlueFret.gameObject.SetActive(false);
            _images.OrangeFret.gameObject.SetActive(false);
            _images.StrumUp.gameObject.SetActive(false);
            _images.StrumDown.gameObject.SetActive(false);
            _images.Whammy.gameObject.SetActive(false);

            base.OnEnable();
        }


        [System.Serializable]
        public struct FiveFretBindingImages
        {
            public Image FiveFretBase;
            public Image GreenFret;
            public Image RedFret;
            public Image YellowFret;
            public Image BlueFret;
            public Image OrangeFret;
            public Image StrumUp;
            public Image StrumDown;
            public Image Whammy;
        }
    }
}