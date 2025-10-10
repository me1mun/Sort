using UnityEngine;

public class HintService
{
    private readonly GridController _gridController;
    private readonly GameObject _hintButton;

    private string _hintedGroupKey;

    public HintService(GridController gridController, GameObject hintButton)
    {
        _gridController = gridController;
        _hintButton = hintButton;
    }

    public void OnHintButton()
    {
        if (!string.IsNullOrEmpty(_hintedGroupKey)) return;

        AdService.Instance.ShowAd("HintReward", ShowHint);
    }

    private void ShowHint()
    {
        string foundHintKey = _gridController.FindCompletableGroupKey();
        if (!string.IsNullOrEmpty(foundHintKey))
        {
            _hintedGroupKey = foundHintKey;
            _gridController.AnimateHintForGroup(foundHintKey);
            SetHintButtonVisibility(true);
        }
    }

    public void OnGroupCollected(string collectedGroupKey)
    {
        if (!string.IsNullOrEmpty(_hintedGroupKey) && _hintedGroupKey == collectedGroupKey)
        {
            _hintedGroupKey = null;
            _gridController.StopAllHintAnimations();
            SetHintButtonVisibility(false);
        }
    }

    private void SetHintButtonVisibility(bool hideButton)
    {
        if (_hintButton != null)
        {
            _hintButton.SetActive(!hideButton);
        }
    }
}