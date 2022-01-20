using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PanelController : MonoBehaviour
{
    public TMP_Text[] table2lbl;
    public void UpdateLabel()
    {
        var dropdown = this.GetComponent<TMP_Dropdown>();

        foreach (var item in table2lbl)
        {
            item.text = dropdown.options[dropdown.value].text;
        }
         
    }
}
