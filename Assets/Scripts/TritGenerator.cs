using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

//optimize html code
//github


//UI voor confusion table
//UI voor plotter
//confusion table code
//det curve
//roc curve
//code voor benchmark van ingvild repliceren


//ideeen voor volgende versie. Scheiden in 5 verschillende apps. 
//    1 voor base 3 converter (met benchmark voor conversion speed) //fixen tenary binary convert bug! 
//    1 voor base 3 comparison (met custom function upload func)
//    1 voor base 3 compression incl. plot van ternary proof
//    1 voor base 3 logic tables vergeleken met base 2
//    1 voor base 3 applications (jpg, wav, movie, custom data)(if this media in ternary benefit is this compared to binary)
//    1 voor base 3 biometric
public class TritGenerator : MonoBehaviour
{
    public GameObject tableA; //panel1
    public GameObject tableB; //panel1
   
    public TMP_Dropdown balancedDdwn;
    public TMP_Dropdown tritLengthDdwn;
    public TMP_Dropdown duplicatesDdwn;
    public TMP_Dropdown compareToDdwn;
    public TMP_Dropdown AmountOfDigitsDdwn;

    public int currentScrollStartIndex = 0;

    List<string> tempListForDuplicatePrevention = new List<string>();

    InputField[] inputfieldsTableA;
    InputField[] inputfieldsTableB;

    //the data (refactor to class!)
    public string[] tableAcodes;   
    public string[] tableBcodes;

    TritTransfomer transformer;
    TritScorer scorer;

    public void Awake()
    {
        //get table fields
        inputfieldsTableA = tableA.GetComponentsInChildren<InputField>();
        inputfieldsTableB = tableB.GetComponentsInChildren<InputField>();

        //get references to other panels (refactor to instance)
        transformer = GameObject.FindObjectOfType<TritTransfomer>();
        scorer = GameObject.FindObjectOfType<TritScorer>();
    }

    public void Start()
    {
        ClearData();
    }

    public void UpdateInputField(string id)
    {
        var index = int.Parse(id[1].ToString());

        if (id.Contains("a"))
        {
            bool isBalanced = balancedDdwn.options[balancedDdwn.value].text.Equals("Balanced") ? true : false;

            if (isValidInput(inputfieldsTableA[index].text, 3, isBalanced))
                tableAcodes[currentScrollStartIndex + index] = SanitizeInput(inputfieldsTableA[index].text);

        }
        else
        {
            int radix = int.Parse(compareToDdwn.options[compareToDdwn.value].text.Split(' ')[1]);

            if (isValidInput(inputfieldsTableB[index].text, radix, false))
                tableBcodes[currentScrollStartIndex + index] = SanitizeInput(inputfieldsTableB[index].text);
        }

        UpdateUI();
    }

    public bool isValidInput(string sequence, int radix, bool balancedTernary)
    {
        //format is digit digit digit ... digit with spaces between digits. Rules:
        
        //negative digits are allowed
        //digits should be in correct base
        //every even position is a digit 
        //every odd position is a space
        bool validInput = false;

        if (sequence != null)
        {
            var sequenceParts = sequence.Split(' ');

            float[] s = new float[sequenceParts.Length];
            for (int j = 0; j < sequenceParts.Length; j++)
            {
                //check if empty
                if (sequenceParts[j].Equals(""))
                {
                    validInput = true; //continue              
                }
                else
                {
                    //check first if numerical 
                    int n;
                    bool isNumeric = int.TryParse(sequenceParts[j], out n);

                    //if valid input check if in correct base
                    if (isNumeric)
                    {
                        int min = 0;
                        int max = radix - 1;

                        if (balancedTernary)
                        {
                            min = -1;
                            max = 1;
                        }

                        if (n >= min && n <= max)
                            validInput = true;
                        else
                            return validInput = false;   //fail validation break early
                    }
                    else
                    {
                        return validInput = false;   //fail validation break early
                    }
                }
            }
        }

        return validInput;
        }

    private bool isEven(int j)
    {
        if (j % 2 == 0)
            return true;
        else
            return false;
    }

    public void ScrollUp(int offset)
    {
            if (currentScrollStartIndex - offset >= 0)
                currentScrollStartIndex -= offset;

            UpdateUI();
      
    }

    public void ScrollDown(int offset)
    {
        if (currentScrollStartIndex + offset + inputfieldsTableA.Length <= tableAcodes.Length ||
            currentScrollStartIndex + offset + inputfieldsTableB.Length <= tableBcodes.Length)
        {
            currentScrollStartIndex += offset;
        }

        UpdateUI();
       
    }

    public void Generate()
    {
        //clear all computations across panels
        ClearData();

        //get settings
        bool isBalanced = balancedDdwn.options[balancedDdwn.value].text.Equals("Balanced") ? true : false;
        bool allowDuplicates = duplicatesDdwn.options[duplicatesDdwn.value].text.Equals("No Duplicates") ? false : true;
        int tritLength = int.Parse(tritLengthDdwn.options[tritLengthDdwn.value].text.Split(' ')[0]);
        int compareToBase = int.Parse(compareToDdwn.options[compareToDdwn.value].text.Split(' ')[1]);
        int n = int.Parse(AmountOfDigitsDdwn.options[AmountOfDigitsDdwn.value].text.Split(' ')[0]);
        
        //generate trits based on settings
        tableAcodes = GenerateCodes(n, 3, isBalanced, allowDuplicates, tritLength);
        tableBcodes = GenerateCodes(n, compareToBase, isBalanced, allowDuplicates, tritLength);
        
        //refresh UI
        UpdateUI();
    }

    private void ClearData()
    {
        tableAcodes = new string[10];
        tableBcodes = new string[10];

        transformer.ClearData();
        scorer.ClearData();
    }

    public void UpdateUI()
    {
        //repopulate tableA of panel 1
        for (int i = 0; i < inputfieldsTableA.Length; i++)
        {
            if (currentScrollStartIndex + i < tableAcodes.Length)
            {
                inputfieldsTableA[i].text = tableAcodes[currentScrollStartIndex + i];
            }
        }


        //repopulate tableB of panel 1
        for (int i = 0; i < inputfieldsTableB.Length; i++)
        {
            if (currentScrollStartIndex + i < tableBcodes.Length)
            {
                inputfieldsTableB[i].text = tableBcodes[currentScrollStartIndex + i];
            }
        }
        
        //update panel 2
        transformer.UpdateUI();

        //update panel 3
        scorer.UpdateUI();
    }

    private string[] GenerateCodes(int n, int radix, bool isBalanced, bool allowDuplicates, int tritLength)
    {
        string[] code = new string[n];
        tempListForDuplicatePrevention.Clear();

        for (int i = 0; i < n; i++)
        {
                code[i] = GenerateCode(radix, isBalanced, allowDuplicates, tritLength);            
        }

        return code;
    }

    private string GenerateCode(int radix, bool isBalanced, bool allowDuplicates, int length)
    {
        var code = GenerateRandomCode(radix, isBalanced, length);

        if (!allowDuplicates)
        {
            if (Mathf.Pow(radix, length) >= 10) //first check if #unique posibilities is >= 10)
            {
                bool solutionFound = false;
                while (!solutionFound)
                {
                    //check if current random value is in generated solutions
                    if (tempListForDuplicatePrevention.Contains(code))
                    {
                        code = GenerateRandomCode(radix, isBalanced, length);
                    }
                    else
                    {
                        solutionFound = true;
                        tempListForDuplicatePrevention.Add(code);
                    }
                }
            }
            else
            {
                //Error: cannot generate unique numbers because solutionspace is smaller then amount of rows (10).
                //Debug.Log("solution space to small");
            }
        }

        return code;
    }

    private string GenerateRandomCode(int radix, bool isBalanced, int length)
    {
        string digitSequence = "";
        int min = 0;
        int max = radix; //note in random.range, max is exclusive, but we start at 0.

        for (int i = 0; i < length; i++)
        {
            if (isBalanced)
            {
                if (radix % 2 != 0) //if oneven number, balance around zero
                {
                    int offset = (radix - 1) / 2;
                    min = -offset;
                    max = offset + 1; //+1 because max is exclusive
                }
            }

            var randomValue = UnityEngine.Random.Range(min, max);

            digitSequence += (randomValue.ToString() +" ");
        }

        return digitSequence.TrimEnd();
    }

    public void ProcessUploadedFile(string file)
    {
        try
        {
            string[] lines = file.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] resultsTableA = new string[lines.Length - 1];
            string[] resultsTableB = new string[lines.Length - 1];

            //parse header
            string[] header = lines[0].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //column A is always ternary. Should have balanced or unbalanced label followed by " ternary"
            string[] tableAHeader = header[0].Split(' ');
            bool isBalanced = tableAHeader[0].ToLower().Equals("balanced") ? true : false;

            //column B is radix n. Should start with "radix "
            string[] tableBHeader = header[1].Split(' ');
            int radix = int.Parse(tableBHeader[1]);

            for (int i = 1; i < lines.Length; i++) //start at 1, skip header
            {
                string[] parts = lines[i].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                if (isValidInput(parts[0], 3, isBalanced))
                {
                    resultsTableA[i - 1] = SanitizeInput(parts[0]);
                }

                if (isValidInput(parts[0], radix, false))
                {
                    resultsTableB[i - 1] = SanitizeInput(parts[1]);
                }
            }

            tableAcodes = resultsTableA;
            tableBcodes = resultsTableB;

            currentScrollStartIndex = 0;
            UpdateUI();
        }
        catch
        {
            //error!
        }
    }

    public string SanitizeInput(string v)
    {
        //remove spaces at the start and end and remove double spaces between codes
        var result = Regex.Replace(v, @"\s+", " ");

        return result.Trim();
    }
}
