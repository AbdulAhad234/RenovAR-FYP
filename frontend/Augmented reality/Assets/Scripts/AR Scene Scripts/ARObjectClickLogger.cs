using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ARObjectClickLogger : MonoBehaviour
{
    void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.screenPosition);
                RaycastHit[] hits = Physics.RaycastAll(ray);
                
                Debug.Log($"[ClickLogger] Screen tapped at {touch.screenPosition}. Found {hits.Length} objects under finger.");
                
                bool hitMe = false;
                foreach (var hit in hits)
                {
                    Debug.Log($"[ClickLogger] -> Hit: {hit.transform.name} (Layer: {hit.transform.gameObject.layer}, Collider: {hit.collider.GetType().Name})");
                    if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                    {
                        hitMe = true;
                    }
                }

                if (hitMe)
                {
                    Debug.Log($"[ClickLogger] SUCCESS! Raycast successfully penetrated and hit {gameObject.name}!");
                }
                else if (hits.Length > 0)
                {
                    Debug.Log($"[ClickLogger] FAILED: Raycast hit something, but missed {gameObject.name}. Are colliders attached?");
                }
            }
        }
    }
}
