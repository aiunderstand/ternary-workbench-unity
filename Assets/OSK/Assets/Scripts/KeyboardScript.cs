using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeyboardScript : MonoBehaviour
{

    public TMP_InputField input;
    public GameObject RusLayoutSml, RusLayoutBig, EngLayoutSml, EngLayoutBig, SymbLayout; 

    public void alphabetFunction(string alphabet)
    {


        input.text=input.text + alphabet;

    }

    public void BackSpace()
    {

        if(input.text.Length>0) input.text= input.text.Remove(input.text.Length-1);

    }

    public void Enter()
    {
        OnScreenKeyboardManager.Instance.HideWithSaveInput();
    }

    public void Cancel()
    {
        OnScreenKeyboardManager.Instance.Hide();
    }


    public void CloseAllLayouts()
    {

        //RusLayoutSml.SetActive(false);
        //RusLayoutBig.SetActive(false);
        //EngLayoutSml.SetActive(false);
        //EngLayoutBig.SetActive(false);
        //SymbLayout.SetActive(false);

    }

    //public void ShowLayout(GameObject SetLayout)
    //{

    //    CloseAllLayouts();
    //    SetLayout.SetActive(true);

    //}

}
