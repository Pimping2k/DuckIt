using Gameplay.Cardboard;
using TMPro;
using UnityEngine;
using View;

namespace UI.Note
{
    public class UINoteView : BaseView
    {
        [SerializeField] private TMP_Text _noteDescrition;

        private BoxConfig _config;

        protected override void OnRequested(params object[] payload)
        {
            _config = payload[0] as BoxConfig;
            _noteDescrition.text = _config.NoteText;
        }
    }
}