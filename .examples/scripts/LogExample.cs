using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gimbl;

public class LogExample : MonoBehaviour
{
    public class Msg
    {
        public int a;
        public int b;
        public string c;
    }
    Msg msg = new Msg();
    LoggerObject logger;
    // Start is called before the first frame update
    void Start()
    {
        // Get instance of logger.
        logger = FindObjectOfType<LoggerObject>();
        // send simple message.
        logger.logFile.Log("Im a test message");
        //More complex message.
        msg.a = 10;
        msg.b = 11;
        msg.c = "AAA";
        logger.logFile.Log("Im a test message",msg);
    }
}
