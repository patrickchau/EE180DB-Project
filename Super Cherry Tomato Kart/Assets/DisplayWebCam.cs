using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using System.Text;
using System.Threading;

public class DisplayWebCam : MonoBehaviour
{
    // goes to super cherry tomato kartz
    string addon = "savedImage.jpg";
    string filepath;
    public bool new_image = false;

    public void update_image(bool input)
    {
        new_image = input;
    }

    void pull_image()
    {
        byte[] fileData;
        Texture2D texture = null;
        if (File.Exists(filepath))
        {
            fileData = File.ReadAllBytes(filepath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
        }
        Renderer rend = this.GetComponentInChildren<Renderer>();
        rend.material.mainTexture = texture;
    }
    // Start is called before the first frame update
    void Start()
    {
        filepath = Path.GetFullPath(addon);
        pull_image();
    }



    // Update is called once per frame
    void Update()
    {
        if (new_image == true)
        {
            try
            {
                print("trying to pull image");
                pull_image();
            }
            catch (Exception e)
            {
                print(e.ToString());
            }
        }
    }
}
