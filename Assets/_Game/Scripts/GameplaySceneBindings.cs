using UnityEngine;

namespace IanGame
{
    public class GameplaySceneBindings : MonoBehaviour
    {
        public GameManager game;
        public UIManager hud;
        public Character2DPresentation character;
        public MissionUI missionUI;
        bool bound;

        void Awake()
        {
            Resolve();
        }

        System.Collections.IEnumerator Start()
        {
            Resolve();
            if (GameContext.Instance == null || game == null)
            {
                yield break;
            }

            var transitions = GameContext.Instance.transitions;
            while (transitions.IsTransitioning)
            {
                yield return null;
            }

            if (
                System.Enum.TryParse<SceneId>(gameObject.scene.name, out var scene)
                && !GrowthAccess.Scene(game, scene, out var reason)
            )
            {
                game.Log(reason);
                transitions.Navigate(SceneId.Home);
            }
        }

        void Resolve()
        {
            if (bound)
            {
                return;
            }

            if (game == null && GameContext.Instance != null)
            {
                game = GameContext.Instance.game;
            }

            if (game == null)
            {
                return;
            }

            bound = true;
            if (hud != null)
            {
                hud.Bind(game);
            }

            if (character != null)
            {
                character.Bind(game);
            }

            if (missionUI != null)
            {
                missionUI.Bind(game.missions);
            }
        }
    }
}
