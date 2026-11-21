using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // SpriteSwap follows pointer, keyboard, canvas-group and interactability changes.
    // Resolve the corresponding foreground when Unity dirties the image.
    [DisallowMultipleComponent]
    public sealed class RecordButtonInk : MonoBehaviour
    {
        Image face;
        TMP_Text[] labels;
        bool chosen;
        RecordSurface restFace;

        public void Bind(Button button, Image target, bool selected, RecordSurface normal)
        {
            if (!face)
            {
                face = target;
                labels = GetComponentsInChildren<TMP_Text>(true);
                face.RegisterDirtyVerticesCallback(UpdateInk);
            }
            else if (chosen == selected && restFace == normal)
            {
                return;
            }

            chosen = selected;
            restFace = normal;
            var highlight = selected ? RecordSurface.InvertedFocus : RecordSurface.Hover;
            var focus = selected ? RecordSurface.InvertedFocus : RecordSurface.Focus;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = LifeRecordUI.Frame(highlight),
                selectedSprite = LifeRecordUI.Frame(focus),
                pressedSprite = LifeRecordUI.Frame(RecordSurface.Pressed),
                disabledSprite = LifeRecordUI.Frame(RecordSurface.Disabled)
            };
            // Preserve the active SpriteSwap state while replacing the normal face.
            face.type = Image.Type.Sliced;
            face.color = Color.white;
            face.canvasRenderer.SetColor(Color.white);
            face.sprite = LifeRecordUI.Frame(selected ? RecordSurface.Inverted : normal);
            UpdateInk();
        }

        void UpdateInk()
        {
            if (!face || labels == null)
            {
                return;
            }

            var sprite = face.overrideSprite;
            bool inverse = sprite == LifeRecordUI.Frame(RecordSurface.Inverted)
                || sprite == LifeRecordUI.Frame(RecordSurface.InvertedFocus);
            Color ink = inverse ? LifeRecordUI.ReverseInk : LifeRecordUI.Body;
            if (sprite == LifeRecordUI.Frame(RecordSurface.Disabled))
            {
                ink = LifeRecordUI.Inactive;
            }

            foreach (var label in labels)
            {
                if (label && label.color != ink)
                {
                    label.color = ink;
                }
            }
        }

        void OnDestroy()
        {
            if (face)
            {
                face.UnregisterDirtyVerticesCallback(UpdateInk);
            }
        }
    }
}
