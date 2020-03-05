using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;
using UnityEngine.Windows.Speech;

public class UVoiceRec : MonoBehaviour
{
    //Key phrases
    public string[] pwr1 = { "quick", "stop", "paint", "shrink", "invincible" };
    public static bool quick_registered = false;
    public static bool paint_registered = false;
    public static bool stop_registered = false;
    public static bool shrink_registered = false;
    public static bool invincible_registered = false;

    private KeywordRecognizer recognize;
    private int timer = 0;
    private bool timerSet = false;

    // Start is called before the first frame update
    //Listening for phrases
    void Start()
    {
        recognize = new KeywordRecognizer(pwr1);
        recognize.OnPhraseRecognized += OnPhraseRecognized;
        recognize.Start();
    }

    //Handles what occurs when it hears the key phrases
    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
        builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
        builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
        Debug.Log(builder.ToString());

        if(!quick_registered && !shrink_registered && !stop_registered && !invincible_registered && !paint_registered)
        {
            switch (args.text)
            {
                case "quick":
                    quick_registered = true;
                    break;
                case "stop":
                    stop_registered = true;
                    break;
                case "paint":
                    paint_registered = true;
                    break;
                case "invincible":
                    invincible_registered = true;
                    break;
                case "shrink":
                    shrink_registered = true;
                    break;
            }
            timerSet = true;
            timer = 0;
            Debug.Log("Power Up Ready");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        timer++;
        if (timer % 50 == 0 && timerSet) {
            timer = 0;
            quick_registered = false;
            stop_registered = false;
            paint_registered = false;
            invincible_registered = false;
            shrink_registered = false;
            Debug.Log("Power Up Expired");
            timerSet = false;
        }
    }
}
