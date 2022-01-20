using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnScreenKeyboardManager : Singleton<OnScreenKeyboardManager>
{
    [HideInInspector]
    public KeyboardScript keyboard;

    [HideInInspector]
    public GameObject keyboardPanel;

    private InputField selectedInputfield;

    TritGenerator generator;

    private void Awake()
    {
        keyboard = GetComponent<KeyboardScript>();
        keyboardPanel = gameObject.transform.GetChild(0).gameObject;
        Hide();

        generator = GameObject.FindObjectOfType<TritGenerator>();
    }

    public  void SetInputfield(InputField inputfield)
    {
        selectedInputfield = inputfield;
        keyboard.input.text = selectedInputfield.text;
    }

    public void Show()
    {
        keyboardPanel.SetActive(true);
    }

    public void Hide()
    {
        keyboardPanel.SetActive(false);
    }

    public void HideWithSaveInput() {

        var index = int.Parse(selectedInputfield.name[1].ToString());

        //store keyboard input to global storage
        if (selectedInputfield.name.Contains("a"))
        {
            bool isBalanced = generator.balancedDdwn.options[generator.balancedDdwn.value].text.Equals("Balanced") ? true : false;

            if (generator.isValidInput(keyboard.input.text, 3, isBalanced))
                generator.tableAcodes[index] = generator.SanitizeInput(keyboard.input.text);
        }
        else
        {
            int radix = int.Parse(generator.compareToDdwn.options[generator.compareToDdwn.value].text.Split(' ')[1]);

            if (generator.isValidInput(keyboard.input.text, radix, false))
                generator.tableBcodes[index] = generator.SanitizeInput(keyboard.input.text);
        }
        //clear
        selectedInputfield = null;
        keyboardPanel.SetActive(false);

        generator.UpdateUI();
    }
}
