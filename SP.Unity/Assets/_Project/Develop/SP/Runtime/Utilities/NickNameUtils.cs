using UnityEngine;

namespace SP.Runtime.Utilities
{
    public static class NickNameUtils
    {
        private static readonly string[] _nickName =
        {
            "Bush-Camper",
            "Log-Collector",
            "Stone-Pocket",
            "Campfire-Ghost",
            "Last-Can-Opener",
            "Rusty-Hunter",
            "Silent-Raider",
            "Stick-And-Dreams",
            "Scrap-Nomad",
            "Lost-Backpack",
            "Crafting-Wizard",
            "Fence-Architect",
            "Loot-Gremlin",
            "Cabin-Without-Door",
            "Wooden-Legend",
            "Ash-Walker",
            "Broken-Machete",
            "Forager-King",
            "Night-Watcher",
            "Rusty-Blade"
        };
        
        public static string GetRandomNickName()
        {
            return _nickName.Length == 0 ? string.Empty : _nickName[Random.Range(0, _nickName.Length - 1)];
        }
    }
}