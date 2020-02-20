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


    private KeywordRecognizer recognize;

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
    }

    // Update is called once per frame
    void Update()
    {

    }
}
