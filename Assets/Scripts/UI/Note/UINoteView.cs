using EventSub;
using EventSub.Implementation;
using Gameplay.Cardboard;
using TMPro;
using UnityEngine;
using View;

namespace UI.Note
{
    public class UINoteView : BaseView
    {
        [SerializeField] private TMP_Text _noteDescrition;

        private PackageConfig _config;

        protected override void OnRequested(params object[] payload)
        {
            _config = payload[0] as PackageConfig;
            _noteDescrition.text = _config.NoteText;
        }

        protected override void OnHide()
        {
            EventBus.Publish(new NoteViewHideEvent());
            base.OnHide();
        }
    }
}