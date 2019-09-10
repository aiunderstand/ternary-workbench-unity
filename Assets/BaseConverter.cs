using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class BaseConverter : MonoBehaviour
{
    public TMP_Dropdown base1Ddwn;
    public TMP_Dropdown base2Ddwn;
    public TMP_InputField base1Input;
    public TMP_InputField base2Output;
    bool balanceTernaryOutput=false;
    public void Convert()
    {
        int baseA = int.Parse(base1Ddwn.options[base1Ddwn.value].text.Split(' ')[1]);
        int baseB = int.Parse(base2Ddwn.options[base2Ddwn.value].text.Split(' ')[1]);

        if (baseB == 3)
            balanceTernaryOutput = !base2Ddwn.options[base2Ddwn.value].text.Split(' ')[2].Contains("Unbalanced");
        
        var sequence = base1Input.text;
 
        //parse sequence into numbers
        var sequenceParts = sequence.Split(' ');

        int[] s = new int[sequenceParts.Length];
        for (int j = 0; j < sequenceParts.Length; j++)
        {
            s[j] = int.Parse(sequenceParts[j].ToString());
        }
       
        base2Output.text = ConvertFromTo(baseA, baseB, s);
    }

    private string ConvertFromTo(int sourceBase, int targetBase, int[] s)
    {
        //we convert from baseA to base10
        int sum = 0;
        if (sourceBase != 3)
        {
            //the multiplication algorithm (https://www.cs.nmsu.edu/~hdp/cs273/notes/binary.html)
            for (int i = 0; i < s.Length; i++)
                sum = (sum * sourceBase) + s[i];
        }
        else
        {
            //the trivial addition algorithm (which explodes with large n)
            for (int i = 0; i < s.Length; i++)
                sum += (int) (s[s.Length -i -1]* Math.Pow(3,i));
        }

        //unbalanced to unbalanced OK
        //unbalanced to balanced ERR
        //balanced to balanced OK 
        //balanced to unbalanced if sum>0 OK
        //balanced to unbalanced if sum<0 ERR
        //-b3 1 -1 1  b3 unbal -1 0 -2

        //we convert from base10 to base B
        var targetBaseNumber = DivisionAlgorithm(sum, targetBase);

        //we convert to balanced ternary if checked
        if (balanceTernaryOutput && targetBase == 3)
        {
            //see algorithm explained https://youtu.be/DLfO_6sTvjo?t=592
            //It is already reversed. Go from left to right and check if there is a 2. If there is replace with -1. Set carry flag. Next digit adds carry and reset flag. Repeat
            bool carrySignal = false;
            for (int i = 0; i < targetBaseNumber.Count; i++)
            {
                switch (targetBaseNumber[i])
                {
                    case 2:
                        {
                            targetBaseNumber[i] = -1;
                            carrySignal = true;
                        }
                        break;
                    case 1:
                        {
                            if (carrySignal) //because 1+1 =2 :D
                            {
                                targetBaseNumber[i] = -1;
                                carrySignal = true;
                            }
                        }
                        break;
                    case 0:
                        {
                            if (carrySignal)
                            {
                                if (sum>=0)
                                    targetBaseNumber[i] = 1;
                                else
                                    targetBaseNumber[i] = -1;
                                carrySignal = false;
                            }
                        }
                        break;
                    case -2:
                        {
                            targetBaseNumber[i] = 1;
                            carrySignal = true;
                        }
                        break;
                    case -1:
                        {
                            if (carrySignal) //because 1+1 =2 :D
                            {
                                targetBaseNumber[i] = 1;
                                carrySignal = true;
                            }
                        }
                        break;
                }
            }

            //finally add one more symbol if a carrysignal is still set
            if (carrySignal)
                targetBaseNumber.Add(1);

            if (sum<0)
                targetBaseNumber.Reverse();
        }

        var result = "";
        for (int i = 0; i < targetBaseNumber.Count; i++)
        {
            result += targetBaseNumber[targetBaseNumber.Count -i -1].ToString() + " ";
        }

        return result.TrimEnd();
    }

    private List<int> DivisionAlgorithm(int base10, int targetBase)
    {
        List<int> result = new List<int>();
        while (base10 != 0)
        {
            result.Add(base10 % targetBase); //the digit for the sequence
            base10 = (base10 / targetBase);
        }

        return result;
    }
}
