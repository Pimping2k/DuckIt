using UnityEngine;

namespace Gameplay.Cardboard
{
    [CreateAssetMenu (menuName = "Cardboard/Box Config", fileName = "BoxConfig_00")]
    public class BoxConfig : ScriptableObject
    {
        [SerializeField] private string _noteText;
        
        public string NoteText => _noteText;
    }
}