using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public GameObject[] pages;
    int index = 0;

    // Start is called before the first frame update
    public void NextPage() {
        if ((index + 1) < pages.Length)
        {
            index++;

            ShowHidePages();
        }
    }

    public void PrevPage()
    {
        if ((index - 1) >= 0)
        {
            index--;

            ShowHidePages();
        }
    }

    public void ShowHidePages()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (i == index)
                pages[i].SetActive(true);
            else
                pages[i].SetActive(false);
        }
    }
}
