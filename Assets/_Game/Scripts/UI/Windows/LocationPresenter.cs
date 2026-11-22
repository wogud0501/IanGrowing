using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public class LocationPresenter : MonoBehaviour
    {
        public Button homeButton;
        public Button schoolButton;
        public Button menuButton;

        void OnEnable()
        {
            if (homeButton != null)
            {
                homeButton.onClick.AddListener(Home);
            }

            if (schoolButton != null)
            {
                schoolButton.onClick.AddListener(School);
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(Menu);
            }
        }

        void OnDisable()
        {
            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(Home);
            }

            if (schoolButton != null)
            {
                schoolButton.onClick.RemoveListener(School);
            }

            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(Menu);
            }
        }

        void Start()
        {
            if (GameContext.Instance == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Bootstrap");
            }
        }

        public void Home()
        {
            if (GameContext.Instance != null)
            {
                GameContext.Instance.transitions.Navigate(SceneId.Home);
            }
        }

        public void School()
        {
            if (GameContext.Instance != null)
            {
                GameContext.Instance.transitions.Navigate(SceneId.School);
            }
        }

        public void Menu()
        {
            if (GameContext.Instance != null)
            {
                UIShell.For(this)?.RequestTitle();
            }
        }
    }
}
