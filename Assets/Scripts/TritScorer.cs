using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TritScorer : MonoBehaviour
{
    public TMP_Dropdown scoreFunction1Ddwn;
    public TMP_Dropdown scoreFunction2Ddwn;
    public TMP_Dropdown transformFunction1Ddwn;
    public TMP_Dropdown transformFunction2Ddwn;

    //input
    public GameObject tableA; //table for function output
    public GameObject tableB; //table for function output
    InputField[] inputA;
    InputField[] inputB;

    //output
    public GameObject tableFunc1;
    public GameObject tableFunc2;

    InputField[] score1Output; 
    InputField[] score2Output;

    public string[] tableAScore1Data;
    public string[] tableBScore1Data; 

    public string[] tableAScore2Data; 
    public string[] tableBScore2Data;

    TritGenerator generator;


    public void Awake()
    {
        inputA = tableA.GetComponentsInChildren<InputField>();
        inputB = tableB.GetComponentsInChildren<InputField>();

        score1Output = tableFunc1.GetComponentsInChildren<InputField>();
        score2Output = tableFunc2.GetComponentsInChildren<InputField>();

        //get reference to data 
        generator = GameObject.FindObjectOfType<TritGenerator>();
    }

    public void UpdateInputField(string id)
    {
        var index = int.Parse(id[1].ToString());

        if (id.Contains("a"))
        {
            bool isBalanced = generator.balancedDdwn.options[generator.balancedDdwn.value].text.Equals("Balanced") ? true : false;

            if (generator.isValidInput(inputA[index].text, 3, isBalanced))
                generator.tableAcodes[generator.currentScrollStartIndex + index] = generator.SanitizeInput(inputA[index].text);

        }
        else
        {
            int radix = int.Parse(generator.compareToDdwn.options[generator.compareToDdwn.value].text.Split(' ')[1]);

            if (generator.isValidInput(inputB[index].text, radix, false))
                generator.tableBcodes[generator.currentScrollStartIndex + index] = generator.SanitizeInput(inputB[index].text);
        }

        //use the generator updateUI which will call the local UpdateUI (should be refactored to a UI manager)
        generator.UpdateUI();
    }

    public void Score()
    {
        //clear all computations across panels
        ClearData();

        //get score function 
        string s1 = scoreFunction1Ddwn.options[scoreFunction1Ddwn.value].text;
        string s2 = scoreFunction2Ddwn.options[scoreFunction2Ddwn.value].text;
        string t1 = transformFunction1Ddwn.options[transformFunction1Ddwn.value].text;
        string t2 = transformFunction2Ddwn.options[transformFunction2Ddwn.value].text;

        //get radix number
        int radix = int.Parse(generator.compareToDdwn.options[generator.compareToDdwn.value].text.Split(' ')[1]);

        tableAScore1Data = ComputeScore(t1, s1, 3, generator.tableAcodes);
        tableAScore2Data = ComputeScore(t2, s2, 3, generator.tableAcodes);

        tableBScore1Data = ComputeScore(t1, s1, radix, generator.tableBcodes);
        tableBScore2Data = ComputeScore(t2, s2, radix, generator.tableBcodes);

        UpdateUI();

    }

    public string[] ComputeScore(string transformName, string distanceName, int radix, string[] data)
    {
        inputA = tableA.GetComponentsInChildren<InputField>();
        inputB = tableB.GetComponentsInChildren<InputField>();
        
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

            //add to array. 
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
                switch (distanceName)
                {
                    case "Trivial Distance (sum transform result)":
                        {
                            result[i] = TrivialScore(transformName, parsedInputX[i], parsedInputY[i]).ToString();
                        }
                        break;
                    case "Engdal Distance (2019)":
                        {
                            result[i] = EngdalDistanceScore_2019(radix, transformName, parsedInputX[i], parsedInputY[i]).ToString();
                        }
                        break;
                    case "Engdal Compare (2019)":
                        {
                            result[i] = EngdalCompareScore_2019(radix, transformName, parsedInputX[i], parsedInputY[i]).ToString();
                        }
                        break;
                    case "Engdal-Bos Distance (2019)":
                        {
                            result[i] = EngdalBosDistanceScore_2019(radix, transformName, parsedInputX[i], parsedInputY[i]).ToString();
                        }
                        break;
                    case "Hamming Distance (1950)":
                        {
                            result[i] = HammingDistance_1950(parsedInputX[i], parsedInputY[i], false).ToString();
                        }
                        break;
                    case "Hamming Distance LengthNorm":
                        {
                            result[i] = HammingDistance_1950(parsedInputX[i], parsedInputY[i], true).ToString();
                        }
                        break;
                    case "Manhattan Distance (L1)":
                        {
                            result[i] = ManhattanDistance_L1(parsedInputX[i], parsedInputY[i]).ToString();
                        }
                        break;
                    case "Euclidian Distance (L2)":
                        {
                            result[i] = EuclidianDistance_L2(parsedInputX[i], parsedInputY[i]).ToString();
                        }
                        break;
                    case "Levenshtein Distance (1966)":
                        {
                            result[i] = LevenshteinDistance_1966(parsedInputX[i], parsedInputX[i].Length, parsedInputY[i], parsedInputY[i].Length).ToString();
                        }
                        break;
                }
            }
            else
            {
                result[i] = ""; //invalid input for score
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
        for (int i = 0; i < inputA.Length; i++)
        {
            if (generator.currentScrollStartIndex + i < generator.tableAcodes.Length)
            {
                inputA[i].text = generator.tableAcodes[generator.currentScrollStartIndex + i];
            }
        }

        //repopulate tableB of panel 1
        for (int i = 0; i < inputB.Length; i++)
        {
            if (generator.currentScrollStartIndex + i < generator.tableBcodes.Length)
            {
                inputB[i].text = generator.tableBcodes[generator.currentScrollStartIndex + i];
            }
        }

        //note we hackishly reuse the table for both A (top 5 entries ) and B (bottom 5 entries) data
        //table A func 1
        for (int i = 0; i < score1Output.Length / 2; i++)
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableAScore1Data.Length)
            {
                score1Output[i].text = tableAScore1Data[generator.currentScrollStartIndex / 2 + i];
            }
        }

        //table A func 2
        for (int i = 0; i < score2Output.Length / 2; i++)
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableAScore2Data.Length)
            {
                score2Output[i].text = tableAScore2Data[generator.currentScrollStartIndex / 2 + i];
            }
        }

        //table B func 1
        for (int i = 0; i < score1Output.Length / 2; i++)
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableBScore1Data.Length)
            {
                score1Output[(score1Output.Length / 2) + i].text = tableBScore1Data[generator.currentScrollStartIndex / 2 + i];
            }
        }

        //table B func 2
        for (int i = 0; i < score2Output.Length / 2; i++)
        {
            if ((generator.currentScrollStartIndex / 2) + i < tableBScore2Data.Length)
            {
                score2Output[(score2Output.Length / 2) + i].text = tableBScore2Data[generator.currentScrollStartIndex / 2 + i];
            }
        }
    }

    public void ClearData()
    {
        tableAScore1Data = new string[10];
        tableBScore1Data = new string[10];

        tableAScore2Data = new string[10];
        tableBScore2Data = new string[10];

        foreach (var f in score1Output)
            f.text = "";

        foreach (var f in score2Output)
            f.text = "";
    }

    private float EngdalBosDistanceScore_2019(int radix, string transformName, float[] a, float[] b)
    {
        float score = 0;

        //first check if pairwise transform is set
        var tritTransformer = GameObject.FindObjectOfType<TritTransfomer>();
        string transformedInput = tritTransformer.PairwiseTransform(transformName, a, b);

        //parse sequence into numbers
        var sequence = transformedInput.Split(' ');

        //inverse sequence (for weighting like in big/little endian?)
        string[] inverseSequence = new string[sequence.Length];
        for (int i = 0; i < sequence.Length; i++)
        {
            inverseSequence[i] = sequence[sequence.Length - 1 - i];
        }

        //compute score, see ported code from [Engdal 2019] p. 48
        for (int j = 0; j < inverseSequence.Length; j++)
        {
            var value = float.Parse(inverseSequence[j]);
            var index = j + 1;

            switch (value)
            {
                case 0:
                    score += 0;
                    break;
                case 1:
                    {
                        var l = Mathf.Pow(radix, inverseSequence.Length) -1;
                        var t = Mathf.Pow(radix, (inverseSequence.Length - (inverseSequence.Length - (index - 1))));
                        var k = t / l;
                        score += k;
                    }
                    break;
                case 2:
                    {
                        var l = Mathf.Pow(radix, inverseSequence.Length) -1;
                        var t = Mathf.Pow(radix, (inverseSequence.Length - (inverseSequence.Length - (index - 1))));
                        var k = (2 * t) / l;
                        score += k;
                    }
                    break;
            }
        }

        return score;
    }

    private float TrivialScore(string transformName, float[] a, float[] b)
    {
        float score = 0;

        //first check if pairwise transform is set
        if (transformName.Equals("No Transform"))
        {
           //break; should not be possible, work on input validation
        }
        else
        {
            var tritTransformer = GameObject.FindObjectOfType<TritTransfomer>();
            string transformedInput = tritTransformer.PairwiseTransform(transformName, a, b);

            //parse sequence into numbers
            var sequenceParts = transformedInput.Split(' ');

            //compute trivial distance by summing
            for (int j = 0; j < sequenceParts.Length; j++)
            {
                score += Math.Abs(float.Parse(sequenceParts[j]));
            }
        }

        return score;
    }

    private float EngdalDistanceScore_2019(int radix, string transformName, float[] a, float[] b)
    {
        float score = 0;

        //first check if pairwise transform is set
        var tritTransformer = GameObject.FindObjectOfType<TritTransfomer>();
        string transformedInput = tritTransformer.PairwiseTransform(transformName, a, b);

        //parse sequence into numbers
        var sequence = transformedInput.Split(' ');

        //inverse sequence (for weighting like in big/little endian?)
        string[] inverseSequence = new string[sequence.Length];
        for (int i = 0; i < sequence.Length; i++)
        {
            inverseSequence[i] = sequence[sequence.Length-1 -i];
        }

        //compute score, see ported code from [Engdal 2019] p. 48
        for (int j = 0; j < inverseSequence.Length ; j++)
        {
            var value= float.Parse(inverseSequence[j]);
            var index = j + 1;
            
            switch (value)
            {
                case 0:
                    score += 0;
                    break;
                case 1:
                    {
                        var l = Mathf.Pow(radix, inverseSequence.Length);
                        var t = Mathf.Pow(radix, (inverseSequence.Length - (inverseSequence.Length - (index - 1))));
                        var k = t / l;
                        score += k;
                    }
                    break;
                case 2:
                    {
                        var l = Mathf.Pow(radix, inverseSequence.Length);
                        var t = Mathf.Pow(radix, (inverseSequence.Length - (inverseSequence.Length - (index - 1))));
                        var k = (2*t) / l;
                        score += k;
                    }
                    break;
            }
        }

        return score;
    }

    private float EngdalCompareScore_2019(int radix, string transformName, float[] a, float[] b)
    {
        float score = 0;

        //first check if pairwise transform is set
        var tritTransformer = GameObject.FindObjectOfType<TritTransfomer>();
        string transformedInput = tritTransformer.PairwiseTransform(transformName, a, b);

        //parse sequence into numbers
        var sequence = transformedInput.Split(' ');

        List<float> s = new List<float>();
        for (int i = 0; i < sequence.Length; i++)
        {
            s.Add(float.Parse(sequence[i]));
        }

        List<float> sDiff = new List<float>();
        //do a sliding diff on the sequence to compare neighbouring values
        for (int i = 0; i < s.Count-1; i++)
        {
            sDiff.Add(Math.Abs(s[i] - s[i + 1]));
        }
        
        //inverse sequence (for weighting like in big/little endian?)
        float[] inverseSequence = new float[sDiff.Count];
        for (int i = 0; i < inverseSequence.Length; i++)
        {
            inverseSequence[i] = sDiff[sDiff.Count - 1 - i];
        }

        //compute score, see ported code from [Engdal 2019] p. 48
        for (int j = 0; j < inverseSequence.Length; j++)
        {
            var value = inverseSequence[j];
            var index = j + 1;

            switch (value)
            {
                case 0:
                    score += 0;
                    break;
                case 1:
                    {
                        var l = Mathf.Pow(radix, sequence.Length); //shouldnt this be inverseSequence.length since array is 1 shorter
                        var t = Mathf.Pow(radix, (sequence.Length - (sequence.Length - index)));
                        var k = t / l;
                        score += k;
                    }
                    break;
                case 2:
                    {
                        var l = Mathf.Pow(radix, sequence.Length);
                        var t = Mathf.Pow(radix, (sequence.Length - (sequence.Length - index)));
                        var k = (2 * t) / l;
                        score += k;
                    }
                    break;
            }
        }

        return score;
    }
    
    private float HammingDistance_1950(float[] a, float[] b, bool isNormalized)
    {
        float score = 0;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                score += 1;
            else
                score += 0;
        }
        if (isNormalized)
            return score / a.Length;
        else
            return score;
    }

    private float ManhattanDistance_L1(float[] a, float[] b)
    {
        float score = 0;

        for (int i = 0; i < a.Length; i++)
        {
            score += Mathf.Abs(a[i] - b[i]);
        }

        return score;
    }

    private float EuclidianDistance_L2(float[] a, float[] b)
    {
        float score = 0;
        for (int i = 0; i < a.Length; i++)
        {
            score += Mathf.Pow((a[i] - b[i]), 2);
        }

        return Mathf.Sqrt(score);
    }

    private float LevenshteinDistance_1966(float[] a, int lengthA, float[] b, int lengthB)
    {
        float score = 0;

        //recursive example from https://en.wikipedia.org/wiki/Levenshtein_distance
        /* base case: empty strings */
        if (lengthA == 0)
            return lengthA;

        if (lengthB == 0)
            return lengthB;

        /* test if last characters of the strings match */
        if (a[lengthA - 1] == b[lengthB - 1])
            score = 0;
        else
            score = 1;

        /* return minimum of delete char from s, delete char from t, and delete char from both */
        var min = Math.Min(LevenshteinDistance_1966(a, lengthA - 1, b, lengthB) + 1, LevenshteinDistance_1966(a, lengthA, b, lengthB - 1) + 1);

        return Math.Min(min, LevenshteinDistance_1966(a, lengthA - 1, b, lengthB - 1) + score);
    }
}
