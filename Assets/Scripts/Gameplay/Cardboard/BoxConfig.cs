using Data;
using Restorable;
using UnityEngine;

namespace Gameplay.Cardboard
{
    [CreateAssetMenu(menuName = DataPaths.GAMEPLAY + "Box Config", fileName = "BoxConfig_00")]
    public class BoxConfig : ScriptableObject
    {
        [SerializeField] private string _noteText;
        [SerializeField] private RestorableItem _itemPrefab;
        
        public string NoteText => _noteText;
        public RestorableItem ItemPrefab => _itemPrefab;
    }
}