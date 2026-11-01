using Data;
using DefaultNamespace;
using Restorable;
using UnityEngine;

namespace Gameplay.Cardboard
{
    [CreateAssetMenu(menuName = DataPaths.GAMEPLAY + "Package Config", fileName = "PackageConfig_00")]
    public class PackageConfig : IdConfig
    {
        [SerializeField] private string _noteText;
        [SerializeField] private RestorableInteractable _itemPrefab;
        
        public string NoteText => _noteText;
        public RestorableInteractable ItemPrefab => _itemPrefab;
    }
}