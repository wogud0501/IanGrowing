using UnityEngine;using UnityEngine.UI;
namespace IanGame {
[ExecuteAlways]
public class PixelMenuLayout:MonoBehaviour {public LayoutElement character;int width;void OnEnable(){Apply();}void Update(){if(width!=Screen.width)Apply();}void Apply(){width=Screen.width;if(character!=null)character.preferredWidth=Screen.height>Screen.width?150:240;}}
}
