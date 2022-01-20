using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TabManager : MonoBehaviour
{
    public Button[] tabs;
    public Color activeColor; //eg. orange
    public Color passiveColor; //eg. grey

    public void OnTabSelect(int callingTabIndex)
    {
        //disable all tabs
        for (int i = 0; i < tabs.Length; i++)
        {
            //disable text
            tabs[i].transform.GetChild(0).gameObject.SetActive(false);

            //color button to grey
            tabs[i].gameObject.GetComponent<Image>().color = passiveColor;
        }

        //enable text selected tab
        tabs[callingTabIndex].transform.GetChild(0).gameObject.SetActive(true);
        
        //color button to grey
        tabs[callingTabIndex].gameObject.GetComponent<Image>().color = activeColor;
    }
}
