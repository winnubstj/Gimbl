using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.Events;
using System.Text;
namespace Gimbl
{
    // Base Class for handling simple triggers.
    public class MQTTChannel
    {
        public string topic;
        public MQTTClient client;                       //Link to MQTT client object.
        public UnityEvent Event = new UnityEvent();     //Event that will be evoked upon message to subscribed topic.
        //Constructor
        public MQTTChannel(string topicStr, bool isListener = true, byte lvl = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE)
        {
            Init(topicStr,isListener, lvl);
        }
        public void Init(string topicStr, bool isListener,  byte lvl)
        {
            topic = topicStr;
            client = GameObject.Find("MQTT Client").GetComponent<MQTTClient>();
            if (isListener) { client.Subscribe(this, topic, lvl); };
        }
        //Messaging functions.
        public virtual void ReceivedMessage(string msgStr) { Event.Invoke(); }
        public void Send() { client.client.Publish(topic, null); }
        //Status
        public bool isConnected() { return client.client.IsConnected; }
    }
    // Derived class that parses messages of known contents.
    public class MQTTChannel<T> : MQTTChannel
    {
        public class ChannelEvent : UnityEvent<T> { }
        public new ChannelEvent Event = new ChannelEvent(); //Event that will be evoked upon message to subscribed topic.

        // Inherit Constructor
        public MQTTChannel(string topicStr, bool isListener = true, byte lvl=MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE) : base(topicStr,isListener,lvl) { }
        //Messaging functions.
        public override void ReceivedMessage(string msgStr) {Event.Invoke(JsonUtility.FromJson<T>(msgStr)); }
        public void Send(T msg) { client.client.Publish(topic, Encoding.UTF8.GetBytes(JsonUtility.ToJson(msg))); }
    }

    // Derived raw class that simply passes message string.
    public class MQTTRawChannel : MQTTChannel
    {
        public class ChannelEvent : UnityEvent<string> { }
        public new ChannelEvent Event = new ChannelEvent(); //Event that will be evoked upon message to subscribed topic.

        // Inherit Constructor
        public MQTTRawChannel(string topicStr, bool isListener = true, byte lvl = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE) : base(topicStr, isListener, lvl) { }
        //Messaging functions.
        public override void ReceivedMessage(string msgStr) { Event.Invoke(msgStr); }
        public void Send(string msg) { client.client.Publish(topic, Encoding.UTF8.GetBytes(msg)); }
    }

}
