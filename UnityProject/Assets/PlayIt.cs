using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayIt : MonoBehaviour
{
    public void PlayScene(string sceneName){
    	Debug.Log("load another scene");
    	SceneManager.LoadScene(sceneName);
    }
}
