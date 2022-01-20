using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WebGLMobileKeyboardFix : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        if (Application.isMobilePlatform && Application.platform == RuntimePlatform.WebGLPlayer)
        {
            var inputField = eventData.selectedObject.GetComponent<InputField>();
            OnScreenKeyboardManager.Instance.SetInputfield(inputField);
            OnScreenKeyboardManager.Instance.Show();
        }
    }
}
