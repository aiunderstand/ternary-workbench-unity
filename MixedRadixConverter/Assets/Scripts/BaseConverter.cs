using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class BaseConverter : MonoBehaviour
{
    public TMP_Dropdown BaseADropbox;
    public TMP_Dropdown BaseBDropbox;
    public TMP_InputField BaseAInput;
    public TMP_InputField Base2Output;
        
    public enum Radix
    {
        Base2Unsigned,
        Base2Signed2C,
        Base3Unbalanced,
        Base3Balanced,
        Base10
    }

    public void Convert()
    {
        //parse the enums
        var partsA = BaseADropbox.options[BaseADropbox.value].text.Split(' ');
        var partsB = BaseBDropbox.options[BaseBDropbox.value].text.Split(' ');

        string partA ="";
        for (int i = 0; i < partsA.Length; i++)
        {
            partA += partsA[i];     
        }

        string partB = "";
        for (int i = 0; i < partsB.Length; i++)
        {
            partB += partsB[i];
        }

        Radix baseA;
        Enum.TryParse(partA, out baseA);
        Radix baseB;
        Enum.TryParse(partB, out baseB);

        Regex regexItem = new Regex("^-?[0-9]*$");
        switch (baseA)
        {
            case Radix.Base10:
                regexItem = new Regex("^-?[0-9]*$");
                break;
            case Radix.Base3Balanced:
                regexItem = new Regex("^[-0+]*$");
                break;
            case Radix.Base3Unbalanced:
                regexItem = new Regex("^[0-2]*$");
                break;
            case Radix.Base2Unsigned:
            case Radix.Base2Signed2C:
                regexItem = new Regex("^[01]*$");
                break;
        }

        //check if input uses correct symbols from that base (input sanitization)
        if (regexItem.IsMatch(BaseAInput.text) && !BaseAInput.text.Equals(""))
        {
            //parse input into individual symbols
            string inputAsString = BaseAInput.text;

            List<string> symbolList = new List<string>();
            if (baseA == Radix.Base3Balanced)
            {
                for (int i = 0; i < inputAsString.Length; i++)
                {
                    string symbol = inputAsString[i].ToString();
                    switch (symbol)
                    {
                        case "-":
                            symbolList.Add("-1");
                            break;
                        case "0":
                            symbolList.Add("0");
                            break;
                        case "+":
                            symbolList.Add("1");
                            break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < inputAsString.Length; i++)
                {
                    string symbol = inputAsString[i].ToString();
                    if (symbol.Equals("-"))
                    {
                        symbol += inputAsString[i + 1].ToString();
                        i++;
                    }

                    symbolList.Add(symbol);
                }
            }
            //reverse input as we want to have the Least Significant Symbol first
            symbolList.Reverse();

            //convert symbols to int array
            bool inputError = false;
            List<int> intList = new List<int>();
            for (int i = 0; i < symbolList.Count; i++)
            {
                int result;
                bool succesful = Int32.TryParse(symbolList[i], out result);

                if (succesful)
                    intList.Add(result);
                else
                    inputError = true;
            }

            //convert From base A To Base B if no errors.
            if (!inputError)
                Base2Output.text = ConvertFromTo(baseA, baseB, intList.ToArray());
        }
        else
        {
            if (BaseAInput.text.Equals(""))
                Debug.Log("Empty input detected");
            else
                Debug.Log("Bad input detected");
        }
    }


    private string ConvertFromTo(Radix SourceBase, Radix TargetBase, int[] symbol)
    {
        string result = "";

        int source = 10;
        switch (SourceBase)
        {
            case Radix.Base10:
                source = 10;
                break;
            case Radix.Base3Balanced:
            case Radix.Base3Unbalanced:
                source = 3;
                break;
            case Radix.Base2Unsigned:
            case Radix.Base2Signed2C:
                source = 2;
                break;
        }
        
        int target = 10;
        int maxsymbol = 0;
        bool inputMustBePositive = false;
        switch (TargetBase)
        {
            case Radix.Base10:
                target = 10;
                maxsymbol = 9;
                break;
            case Radix.Base3Balanced:
                target = 3;
                maxsymbol = 1;
                break;
            case Radix.Base3Unbalanced:
                target = 3;
                maxsymbol = 2;
                inputMustBePositive = true;
                break;
            case Radix.Base2Unsigned:
                inputMustBePositive = true;
                target = 2;
                maxsymbol = 1;
                break;
            case Radix.Base2Signed2C:
                target = 2;
                maxsymbol = 1;
                break;
        }

        //convert input to base 10
        Int64 decimalResult = 0;
        switch (SourceBase)
        {
            case Radix.Base2Signed2C:
                {
                    for (int i = 0; i < symbol.Length - 1; i++)
                        decimalResult += (Int64)(symbol[i] * Math.Pow(source, i));
                    
                    decimalResult = (Int64)(-1 * symbol[symbol.Length - 1] * Math.Pow(source, symbol.Length - 1)) + decimalResult;
                }
                break;
            case Radix.Base10: //instead of computing, we could also just copy from input. Since base10 is signed, we need adjust for negative numbers 
                {
                    for (int i = 0; i < symbol.Length; i++)
                        decimalResult += (Int64)(Math.Abs(symbol[i]) * Math.Pow(source, i));

                    decimalResult = symbol[symbol.Length-1] < 0 ? -decimalResult : decimalResult;
                }
                break;
            default:
                {
                    for (int i = 0; i < symbol.Length; i++)
                        decimalResult += (Int64)(symbol[i] * Math.Pow(source, i));
                }
                break;
        }
        
        //2do: do a check to limit the input number to int64.maxvalue

        //check sign of decimalResult
        bool isPos = false;
        if (decimalResult >= 0)
            isPos = true;

        if (inputMustBePositive && !isPos)
        {
            Debug.Log("input must be positive for unsigned numbers");
        }
        else
        {
            //find #symbols or positions needed. We need one position extra for Base 2 Signed 2C
            bool found = false;
            int symbolCount = 0;
            Int64 sum = 0;

            while (!found)
            {
                symbolCount++;
                sum += (Int64)(maxsymbol * Math.Pow(target, symbolCount - 1));
                Int64 diff = isPos ? sum - decimalResult : sum + decimalResult;

                if (diff >= 0)
                    found = true;
            }

            Debug.Log("symbols needed: " + symbolCount);
            Debug.Log("decimal: " + decimalResult);

            //do the conversion
            List<int> outputSymbols = new List<int>();
            Int64 remainingSum = decimalResult;

            switch (TargetBase)
            {
                case Radix.Base10:
                    return decimalResult.ToString();                 
                case Radix.Base2Unsigned:
                    {
                        //convert
                        for (int i = symbolCount-1; i >= 0; i--)
                        {
                            if (remainingSum - 1 * Math.Pow(target, i) >= 0)
                            {
                                outputSymbols.Add(1);
                                remainingSum -=  1 * (Int64) Math.Pow(target, i);
                            }
                            else
                                outputSymbols.Add(0);
                        }
                    }
                    break;
                case Radix.Base2Signed2C:
                    {
                        //convert
                        if (isPos) //add the sign bit
                        {
                            outputSymbols.Add(0);
                        }
                        else
                        {
                            outputSymbols.Add(1);
                            remainingSum = (Int64)Math.Pow(target, symbolCount) + remainingSum;
                        }

                        for (int i = symbolCount - 1; i >= 0; i--)
                        {
                            if (remainingSum - 1 * Math.Pow(target, i) >= 0)
                            {
                                outputSymbols.Add(1);
                                remainingSum -= 1 * (Int64)Math.Pow(target, i);
                            }
                            else
                                outputSymbols.Add(0);
                        }
                    }
                    break;
                case Radix.Base3Unbalanced:
                    {
                        //convert
                        for (int i = symbolCount - 1; i >= 0; i--)
                        {
                            if (remainingSum - 2 * Math.Pow(target, i) >= 0)
                            {
                                outputSymbols.Add(2);
                                remainingSum -= 2 * (Int64)Math.Pow(target, i);
                            }
                            else
                            {
                                if (remainingSum - 1 * Math.Pow(target, i) >= 0)
                                {
                                    outputSymbols.Add(1);
                                    remainingSum -= 1 * (Int64)Math.Pow(target, i);
                                }
                                else
                                    outputSymbols.Add(0);
                            }
                        }
                    }
                    break;
                case Radix.Base3Balanced:
                    {
                        //convert by finding the symbol that lowers the remainingsum the best
                        for (int i = symbolCount - 1; i >= 0; i--)
                        {
                            int bestSymbol = 0;

                            //try +
                            if (Math.Abs(remainingSum + 1 * Math.Pow(target, i)) < (Int64)Math.Abs(remainingSum))
                            {
                                bestSymbol = -1;
                                remainingSum += 1 * (Int64) Math.Pow(target, i);
                            }

                            if (Math.Abs(remainingSum - 1 * Math.Pow(target, i)) < (Int64)Math.Abs(remainingSum))
                            {
                                bestSymbol = 1;
                                remainingSum -= 1 * (Int64) Math.Pow(target, i);
                            }

                            outputSymbols.Add(bestSymbol);
                        }
                    }
                    break;
            }

            //transform list of output symbols to an output string
            for (int i = 0; i < outputSymbols.Count; i++)
            {
                if (TargetBase != Radix.Base3Balanced)
                    result += outputSymbols[i].ToString();
                else
                {
                    switch(outputSymbols[i])
                    {
                        case -1:
                            result += "-";
                            break;
                        case 0:
                            result += "0";
                            break;
                        case 1:
                            result += "+";
                            break;
                    }
                }
            }
        }

        return result;
    }
}
