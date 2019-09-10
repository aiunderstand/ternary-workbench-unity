using UnityEngine;
using TMPro;

public class SettingInputValidator : MonoBehaviour
{
    public TMP_Dropdown transformDdwn;

    public void validateInput()
    {
        var thisDropdown = GetComponent<TMP_Dropdown>();
        string distanceFunc = thisDropdown.options[thisDropdown.value].text;

        switch (distanceFunc)
        {
            case "Trivial Distance (sum transform result)":
                {
                    transformDdwn.value = 3; //trivial substract
                }
                break;
            case "Engdal Distance (2019)":
                {
                    //allow pairwise transform
                }
                break;
            case "Engdal Compare (2019)":
                {
                    //allow pairwise transform
                }
                break;
            case "Engdal-Bos Distance (2019)":
                {
                    //allow pairwise transform
                }
                break;
            case "Engdal-Bos Direction (2019)":
                {
                    //allow pairwise transform
                }
                break;
            case "Hamming Distance (1950)":
                {
                    transformDdwn.value = 0; //no pairwise transform allowed
                }
                break;
            case "Hamming Distance LengthNorm":
                {
                    transformDdwn.value = 0; //no pairwise transform allowed
                }
                break;

            case "Manhattan Distance (L1)":
                {
                    transformDdwn.value = 0; //no pairwise transform allowed
                }
                break;
            case "Euclidian Distance (L2)":
                {
                    transformDdwn.value = 0; //no pairwise transform allowed
                }
                break;
            case "Levenshtein Distance (1966)":
                {
                    transformDdwn.value = 0; //no pairwise transform allowed
                }
                break;
        }
    }
}
