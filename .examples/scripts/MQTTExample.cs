using System;
using System.Collections;
using System.Collections.Generic;
using Gimbl;
using UnityEngine;

public class MQTTExample: MonoBehaviour
{
    // class describing the message format.
    public class MSG
    {
        public string a;
        public int b;
    }
    // Start is called before the first frame update
    void Start()
    {
        // Example with message parsing.
        // Setup Listener.
        MQTTChannel<MSG> channel = new MQTTChannel<MSG>("Unity/");
        channel.Event.AddListener(OnMessage);
        // Send data
        MSG msg = new MSG() { a = "Hello!", b = 200 };
        channel.Send(msg);

        // Trigger example.
        MQTTChannel trigger = new MQTTChannel("Unity2/");
        trigger.Event.AddListener(OnTrigger);
        trigger.Send();

    }
    void OnMessage(MSG msg)
    {
        Debug.Log(msg.a);
    }

    void OnTrigger()
    {
        Debug.Log("Got Triggered!");
    }

}
