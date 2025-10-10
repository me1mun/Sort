using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    [Header("Actions")]
    public UnityEvent OnClick;
    
    [Header("Feedback")]
    [SerializeField] private bool playSoundOnClick = true;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (playSoundOnClick)
        {
            AudioManager.Instance.Play("Tap");
            //Debug.Log("click sound play");
        }
    }
}