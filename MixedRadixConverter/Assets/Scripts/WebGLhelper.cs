using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class WebGLhelper : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void Fullscreen(int mode);

    [DllImport("__Internal")]
    private static extern void OpenWindow(string url);


    bool isFullscreen = false;
    public string sourceRepoURL;

    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        int mode = isFullscreen ? 1 : 0;
        Fullscreen(mode);
    }

    public void OpenExternalWindow()
    {
        OpenWindow(sourceRepoURL);
    }
}
