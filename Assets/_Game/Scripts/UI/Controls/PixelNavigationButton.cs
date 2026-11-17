using UnityEngine;

namespace IanGame
{
    public class PixelNavigationButton : MonoBehaviour
    {
        public enum Target
        {
            Home,
            Life,
            Growth,
            Activity,
            Menu,
            Inventory,
            Mission,
            Location,
            GrowthTab,
        }

        public UINavigationService service;
        public Target target;
        public SceneId location;
        public int tab;

        public void Invoke()
        {
            if (service == null)
            {
                service = GetComponentInParent<UINavigationService>();
            }

            if (service == null)
            {
                return;
            }

            switch (target)
            {
                case Target.Home:
                    service.Home();
                    break;
                case Target.Life:
                    service.Life();
                    break;
                case Target.Growth:
                    service.Growth();
                    break;
                case Target.Activity:
                    service.Activity();
                    break;
                case Target.Menu:
                    service.Menu();
                    break;
                case Target.Inventory:
                    service.Inventory();
                    break;
                case Target.Mission:
                    service.Mission();
                    break;
                case Target.Location:
                    service.Location(location);
                    break;
                case Target.GrowthTab:
                    service.GrowthTab(tab);
                    break;
            }
        }
    }
}
