using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BaseConverter;

public class KeyboardManager : MonoBehaviour
{
    public InputField BaseAInput;
    public TMP_Dropdown BaseADropbox;



    public void OnButtonPress(string value)
    {
        if (value.Equals("backspace"))
        {
            if (BaseAInput.text.Length > 0)
            {
                BaseAInput.text = BaseAInput.text.Remove(BaseAInput.text.Length - 1);
                BaseAInput.onEndEdit.Invoke(BaseAInput.text);
            }
        }
        else
        {
            //parse the enums
            var partsA = BaseADropbox.options[BaseADropbox.value].text.Split(' ');
        
            string partA = "";
            for (int i = 0; i < partsA.Length; i++)
            {
                partA += partsA[i];
            }

        
            Radix baseA;
            Enum.TryParse(partA, out baseA);
            
            if (value.Equals("-") && baseA == Radix.Base10)
            {
                if (BaseAInput.text.Length == 0)
                {
                    BaseAInput.text = BaseAInput.text + value;
                    BaseAInput.onEndEdit.Invoke(BaseAInput.text);
                }
            }
            else
            {
                BaseAInput.text = BaseAInput.text + value;
                BaseAInput.onEndEdit.Invoke(BaseAInput.text);
            }
        }
    }
}
