using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DimCloser : MonoBehaviour, IPointerDownHandler
{
    private UnityEvent onDimTouch = new UnityEvent();

    public void AddListener(UnityAction listener)
    {
        onDimTouch.AddListener(listener);
    }

    public void RemoveListener(UnityAction listener)
    {
        onDimTouch.RemoveListener(listener);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onDimTouch.Invoke();
    }
}
