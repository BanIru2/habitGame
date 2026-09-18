using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DimCloser : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private UnityEvent onDimTouch;

    public void OnPointerDown(PointerEventData eventData)
    {
        onDimTouch?.Invoke();
    }
}
