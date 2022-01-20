using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class TritTransfomer : MonoBehaviour
{
    public TMP_Dropdown function1Ddwn;
    public TMP_Dropdown function2Ddwn;
    
    //input
    public GameObject tableBase3; 
    public GameObject tableBaseN; 
   
    //output
    public GameObject tableFunc1;
    public GameObject tableFunc2;

    InputField[] inputBase3;
    InputField[] inputBaseN;

    InputField[] function1Output;
    InputField[] function2Output;

    public string[] tableAFunc1Data;
    public string[] tableBFunc1Data;

    public string[] tableAFunc2Data;
    public string[] tableBFunc2Data;

    TritGenerator generator;

    public void Awake()
    {
        inputBase3 = tableBase3.GetComponentsInChildren<InputField>();
        inputBaseN = tableBaseN.GetComponentsInChildren<InputField>();

        function1Output = tableFunc1.GetComponentsInChildren<InputField>();
        function2Output = tableFunc2.GetComponentsInChildren<InputField>();

        //get reference to data 
        generator = GameObject.FindObjectOfType<TritGenerator>();
    }

    public void UpdateInputField(string id)
    {
        var index = int.Parse(id[1].ToString());

        if (id.Contains("a"))
        {
            bool isBalanced = generator.balancedDdwn.options[generator.balancedDdwn.value].text.Equals("Balanced") ? true : false;

            if (generator.isValidInput(inputBase3[index].text, 3, isBalanced))
                generator.tableAcodes[generator.currentScrollStartIndex + index] = generator.SanitizeInput(inputBase3[index].text);

        }
        else
        {
            int radix = int.Parse(generator.compareToDdwn.options[generator.compareToDdwn.value].text.Split(' ')[1]);

            if (generator.isValidInput(inputBaseN[index].text, radix, false))
                generator.tableBcodes[generator.currentScrollStartIndex + index] = generator.SanitizeInput(inputBaseN[index].text);
        }

        //use the generator updateUI which will call the local UpdateUI (should be refactored to a UI manager)
        generator.UpdateUI();
    }

    public void Transform()
    {
        //clear all computations across panels
        ClearData();

        //get function 
        string f1 = function1Ddwn.options[function1Ddwn.value].text;
        string f2 = function2Ddwn.options[function2Ddwn.value].text;

        //compare a with b of same base using func set in dropdowns
            tableAFunc1Data = PairwiseTransform(f1, generator.tableAcodes);
            tableAFunc2Data = PairwiseTransform(f2, generator.tableAcodes);
       
            tableBFunc1Data = PairwiseTransform(f1, generator.tableBcodes);
            tableBFunc2Data = PairwiseTransform(f2, generator.tableBcodes);
   
        UpdateUI();
    }

    public string[] PairwiseTransform(string functionName, string[] data) //transform is done on two inputs, not on single input. Since we only do pairwise maybe refactor to pairwise transformer?
    {
        //parse and divide input of tables into two arrays (so we can compare same bases)
        List<float[]> parsedInputX = new List<float[]>();
        List<float[]> parsedInputY = new List<float[]>();
        
        for (int i = 0; i < data.Length; i++)
        {
            var sequence = data[i];

            float[] s = null;

            //parse sequence into numbers
            if (sequence != null && !sequence.Equals(""))
            {
                var sequenceParts = sequence.Split(' ');
                s = new float[sequenceParts.Length];

                for (int j = 0; j < sequenceParts.Length; j++)
                {
                    s[j] = float.Parse(sequenceParts[j].ToString());
                }
            }

            //add to array
            if (i % 2 == 0)
                parsedInputX.Add(s);
            else
                parsedInputY.Add(s);
        }

        string[] result = new string[parsedInputX.Count];

        for (int i = 0; i < parsedInputX.Count; i++)
        {
            if (isValidTransform(parsedInputX[i], parsedInputY[i]))
            {

                switch (functionName)
                {
                    case "KeirPermutation5 (1964)":
                        {
                            result[i] = KeirPermutation5_1964(parsedInputX[i], parsedInputY[i]);
                        }
                        break;
                    case "Engdal Compare (2019)":
                        {
                            result[i] = EngdalCompare_2019(parsedInputX[i], parsedInputY[i]);
                        }
                        break;
                    case "Trivial Substract":
                        {
                            result[i] = TrivialSubstract(parsedInputX[i], parsedInputY[i]);
                        }
                        break;
                }
            }
            else
            {
                result[i] = ""; //invalid input for transform
            }
        }

        return result;
    }

    public bool isValidTransform(float[] v1, float[] v2)
    {
        //valid input for transform needs to conform to
        // have same length
        // are both non-zero
        bool validInput = false;

        if (v1 != null && v2 != null)
        {
            if (v1.Length == v2.Length)
                validInput = true;
        }

        return validInput;
    }

    public void UpdateUI()
    {
        //repopulate tableA of panel 1
        for (int i = 0; i < inputBase3.Length; i++)
        {
            if (generator.currentScrollStartIndex + i < generator.tableAcodes.Length)
            {
                inputBase3[i].text = generator.tableAcodes[generator.currentScrollStartIndex + i];
            }
        }

        //repopulate tableB of panel 1
        for (int i = 0; i < inputBaseN.Length; i++)
        {
            if (generator.currentScrollStartIndex + i < generator.tableBcodes.Length)
            {
                inputBaseN[i].text = generator.tableBcodes[generator.currentScrollStartIndex + i];
            }
        }
        
        //note we hackishly reuse the table for both A (top 5 entries ) and B (bottom 5 entries) data
        //table A func 1
        for (int i = 0; i < function1Output.Length / 2; i++) 
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableAFunc1Data.Length)
            {
                function1Output[i].text = tableAFunc1Data[generator.currentScrollStartIndex / 2 + i];
            }
        }

        //table A func 2
        for (int i = 0; i < function2Output.Length / 2; i++)
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableAFunc2Data.Length)
            {
                function2Output[i].text = tableAFunc2Data[generator.currentScrollStartIndex / 2 + i];
            }
        }

        //table B func 1
        for (int i = 0; i < function1Output.Length / 2; i++)
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableBFunc1Data.Length)
            {
                function1Output[(function1Output.Length / 2) +i].text = tableBFunc1Data[generator.currentScrollStartIndex / 2 + i];
            }
        }

        //table B func 2
        for (int i = 0; i < function2Output.Length / 2; i++)
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableBFunc2Data.Length)
            {
                function2Output[(function1Output.Length / 2) + i].text = tableBFunc2Data[generator.currentScrollStartIndex / 2 + i];
            }
        }
    }

    public void ClearData()
    {
        tableAFunc1Data = new string[10]; 
        tableBFunc1Data = new string[10];

        tableAFunc2Data = new string[10];
        tableBFunc2Data = new string[10];

        foreach (var f in function1Output)
            f.text = "";

        foreach (var f in function2Output)
            f.text = "";

    }

    private string KeirPermutation5_1964(float[] a, float[] b)
    {
        string score = "";

        for (int i = 0; i < a.Length; i++)
        {
            var distance = Math.Abs(a[i] - b[i]);

            if (distance == 0)
            {
                score += "0 ";
            }
            else
            {
                if (distance == 1)
                {
                    score += "1 "; //actually -1 and then transformed to 1 (using permutation 5, see Keir, Y. 1964 Algebraic Properties of 3-Valued Compositions)
                }
                else //distance > 1
                    score += "2 ";//actually 1 and then transformed to 2 (using permutation 5, see Keir, Y. 1964 Algebraic Properties of 3-Valued Compositions)
            }
        }

        return score.TrimEnd();
    }

    private string EngdalCompare_2019(float[] a, float[] b)
    {
        string score = "";

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == b[i])
            {
                score += "0 ";
            }
            else
            {
                if (a[i] > b[i])
                    score += "-1 ";
                else // a < b
                    score += "1 ";
            }
        }
        return score.TrimEnd();
    }

    private string TrivialSubstract(float[] a, float[] b)
    {
        string score = "";

        for (int i = 0; i < a.Length; i++)
        {
            var distance = a[i] - b[i];

            score += (distance + " ");
        }

        return score.TrimEnd();
    }

    public string PairwiseTransform(string functionName, float[] a, float[] b) //refactor, duplicate and overloading code with pairwiseTransform
    {
        string result = "";
        switch (functionName)
            {
                case "KeirPermutation5 (1964)":
                    {
                    result = KeirPermutation5_1964(a, b);
                    }
                    break;
                case "Engdal Compare (2019)":
                    {
                    result = EngdalCompare_2019(a, b);
                    }
                    break;
                case "Trivial Substract":
                    {
                    result = TrivialSubstract(a, b);
                    }
                    break;
            }

        return result;
    }
}
