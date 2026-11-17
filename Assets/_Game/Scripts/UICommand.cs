using UnityEngine;

namespace IanGame
{
    public class UICommand : MonoBehaviour
    {
        public enum Command
        {
            Open,
            Close,
            Pause,
            Settings,
            Status,
            Route,
            Motion,
            TextSize,
            ConfirmLife,
            AcceptLife,
        }

        public Command command;
        public UIShell shell;
        public GameObject panel;
        public SceneId route;

        public void Invoke()
        {
            if (shell == null)
            {
                shell = UIShell.For(this);
            }

            switch (command)
            {
                case Command.Open:
                    shell.Push(panel);
                    break;
                case Command.Close:
                    shell.Close(panel);
                    break;
                case Command.Pause:
                    shell.OpenPause();
                    break;
                case Command.Settings:
                    shell.OpenSettings();
                    break;
                case Command.Status:
                    shell.OpenStatus();
                    break;
                case Command.Route:
                    if (GameContext.Instance != null)
                    {
                        GameContext.Instance.transitions.Navigate(route);
                    }

                    break;
                case Command.Motion:
                    shell.ToggleMotion();
                    break;
                case Command.TextSize:
                    shell.ToggleText();
                    break;
                case Command.ConfirmLife:
                    shell.ConfirmNewLife();
                    break;
                case Command.AcceptLife:
                    shell.AcceptNewLife();
                    break;
            }
        }
    }
}
