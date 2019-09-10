using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class CameraController : MonoBehaviour
{
    [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
    public float transitionLerpTime = 0.2f;

    public TMP_Text lblGenerate;
    public TMP_Text lblTransform;
    public TMP_Text lblScore;
    public TMP_Text lblBenchmark;
    public TMP_Text lblPrevBtn;
    public TMP_Text lblNextBtn;
    public GameObject prevBtn;
    public GameObject nextBtn;

    float targetTranslateZ;
    float targetTranslateY;
    float targetRotX;
    float nextTarget = 3.66f;
    int panelId = 1;

    private void Awake()
    {
        targetTranslateZ = transform.position.z;
        targetTranslateY = transform.position.y;
        targetRotX = transform.rotation.eulerAngles.x;
    }
    void Update()
    {
        // Framerate-independent interpolation
        // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
        var deltaTransition = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / transitionLerpTime) * Time.deltaTime);


        var tZ = Mathf.Lerp(transform.position.z, targetTranslateZ, deltaTransition);
        var tY = Mathf.Lerp(transform.position.y, targetTranslateY, deltaTransition);

        var rX = Mathf.Lerp(transform.rotation.eulerAngles.x, targetRotX, deltaTransition);

        transform.position = new Vector3(transform.position.x, tY, tZ);
        transform.rotation = Quaternion.Euler(new Vector3(rX, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z));
    }

    public void NextSlide()
    {
        if ((panelId + 1) < 5)
            panelId++;
        
        UpdateUI();
    }


    public void PreviousSlide()
    {
        if ((panelId - 1) >= 1)
            panelId--;
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        lblGenerate.fontStyle = FontStyles.UpperCase;
        lblTransform.fontStyle = FontStyles.UpperCase;
        lblScore.fontStyle = FontStyles.UpperCase;
        lblBenchmark.fontStyle = FontStyles.UpperCase;

        switch (panelId)
        {
            case 1:
                lblGenerate.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

                prevBtn.SetActive(false);
                nextBtn.SetActive(true);

                lblPrevBtn.text = "";
                lblNextBtn.text = "Transform";

                targetRotX = 0;
                targetTranslateY = 1;
                targetTranslateZ = (3.66f * 0) -0.09f;
                break;
            case 2:
                lblTransform.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

                prevBtn.SetActive(true);
                nextBtn.SetActive(true);

                lblPrevBtn.text = "Generate";
                lblNextBtn.text = "Score";

                targetRotX = 0;
                targetTranslateY = 1;
                targetTranslateZ = (3.66f * 1) - 0.09f;
                break;
            case 3:
                lblScore.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

                prevBtn.SetActive(true);
                nextBtn.SetActive(true);

                lblPrevBtn.text = "Transform";
                lblNextBtn.text = "Benchmark";

                targetRotX = 0;
                targetTranslateY = 1;
                targetTranslateZ = (3.66f * 2) - 0.09f;
                break;
            case 4:
                lblBenchmark.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

                prevBtn.SetActive(true);
                nextBtn.SetActive(false);

                lblPrevBtn.text = "Score";
                lblNextBtn.text = "";

                targetRotX = 90;
                targetTranslateY = 2.5f;
                targetTranslateZ = (3.66f * 2) - 0.09f;
                break;
        }
    }
}
