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
    string relpath;
    // goes to super cherry tomato kartz
    string addon = "FaceDetection\\savedImage.jpg";
    string filepath;
    string filepath2 = "C:\\Users\\Patrick Chau\\Documents\\GitHub\\EE180DB-Project\\Super Cherry Tomato Kart\\FaceDetection\\savedImage.jpg";
    public bool new_image = false;

    public void update_image(bool input)
    {
        new_image = input;
    }

    void pull_image()
    {
        byte[] fileData;
        Texture2D texture = null;
        print(filepath);
        print("Does the file path exist?: " + System.IO.File.Exists(filepath) + "\n");
        if (File.Exists(filepath))
        {
            print("pull image!");
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
        //string filepath = Application.dataPath.Remove(Application.dataPath.Length-7, 7) + addon;
        filepath = Path.GetFullPath(addon);
        print(filepath);
        print("Does the file path exist?: " + System.IO.File.Exists(filepath) + "\n");
        print(filepath2);
        print("Does the file path exist?: " + File.Exists(filepath2) + "\n");
        pull_image();
    }



    // Update is called once per frame
    void Update()
    {
        if (new_image == true)
        {
            try
            {
                pull_image();
            }
            catch (Exception e)
            {
                print(e.ToString());
            }
        }
    }
}
