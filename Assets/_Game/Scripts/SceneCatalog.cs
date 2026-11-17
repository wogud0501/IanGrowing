using System;
using UnityEngine;

namespace IanGame
{
    public enum SceneId
    {
        Bootstrap,
        MainMenu,
        Home,
        School,
        Workplace,
        City,
        Battle,
        StockMarket,
        Gacha,
        Shop,
        Housing,
        Ending,
        LifeWorld,
        RoomFamilyLiving,
        RoomFamilyKitchen,
        RoomFamilyParents,
        RoomFamilyIan,
        RoomFamilyBathroom,
        RoomIndependentHome,
        RoomIndependentBathroom,
        RoomCity,
        RoomAlley,
        RoomWorkspace,
        RoomWorkspaceBathroom,
        RoomSchool,
        RoomShop,
        RoomFinance,
        RoomRealEstate,
        RoomResidence,
    }

    [Serializable]
    public class SceneRoute
    {
        public SceneId id;
        public string sceneName;
        public bool implemented;
    }

    [CreateAssetMenu(menuName = "Ian/Scene Catalog")]
    public class SceneCatalog : ScriptableObject
    {
        public SceneRoute[] routes = new SceneRoute[0];

        public SceneRoute Find(SceneId id) => Array.Find(routes, r => r != null && r.id == id);
    }
}
