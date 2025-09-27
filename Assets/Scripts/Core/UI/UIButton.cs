using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Actions")]
    public UnityEvent OnClick;

    private void Awake()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("click");
        OnClick.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.Instance.Play("Tap");      
    }

    public void OnPointerUp(PointerEventData eventData)
    {

    }

}