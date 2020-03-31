using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gimbl;

namespace Gimbl
{
    public class FloatArray { public float[] array; }
    public class FloatMsg { public float value; }
    public class BoolMsg { public bool flag; }
    public class StringMessage { public string strMsg; }
    public class BlinkMsg { public int darkTime; public int fadeTime; public bool disable; }
    public class GimblObjectChan<T>
    {
        public bool flag = false;
        public T lastMsg;
        public System.Action<T> action;
        public GimblObjectChan(string name, System.Action<T> reqFunc)
        {
            string chanString = string.Format("Gimbl/{0}/{1}", name, reqFunc.Method.Name);
            MQTTChannel<T> chan = new MQTTChannel<T>(chanString);
            action = reqFunc;
            chan.Event.AddListener(Callback);
        }

        public void Callback(T msg) { flag = true; lastMsg = msg; }
        public void Test() { if (flag) {action(lastMsg); flag = false; } }
    }

    [AddComponentMenu("Gimbl/GimblObject")]
    public class GimblObject : MonoBehaviour
    {
        public class ColliderMessage { public string collider; }
        MQTTChannel<ColliderMessage> ColChan;
        public class PositionMessage { public float[] position; }
        MQTTChannel<PositionMessage> PosChan;
        GimblObjectChan<FloatArray> GetPosChan;




        GimblObjectChan<FloatArray> MoveChan;
        GimblObjectChan<FloatArray> MoveToChan;
        GimblObjectChan<FloatMsg> RotateChan;
        GimblObjectChan<FloatMsg> RotateToChan;
        GimblObjectChan<FloatArray> SetColorChan;
        GimblObjectChan<StringMessage> SetMatChan;
        GimblObjectChan<StringMessage> SetTexChan;
        GimblObjectChan<FloatMsg> SetOpacChan;
        GimblObjectChan<BoolMsg> SetVisChan;
        GimblObjectChan<BoolMsg> PlaySoundChan;
        GimblObjectChan<FloatMsg> SetBrightChan;
        GimblObjectChan<BlinkMsg> BlinkChan;
        // Start is called before the first frame update
        void Start()
        {
            //Collision events.
            ColChan = new MQTTChannel<ColliderMessage>(string.Format("Gimbl/{0}/Collision", name), false);
            PosChan = new MQTTChannel<PositionMessage>(string.Format("Gimbl/{0}/Position", name), false);
            GetPosChan = new GimblObjectChan<FloatArray>(name, GetPosition);

            MoveChan = new GimblObjectChan<FloatArray>(name, Move);
            MoveToChan = new GimblObjectChan<FloatArray>(name, MoveTo);
            RotateChan = new GimblObjectChan<FloatMsg>(name, Rotate);
            RotateToChan = new GimblObjectChan<FloatMsg>(name, RotateTo);
            SetColorChan = new GimblObjectChan<FloatArray>(name, SetColor);
            SetMatChan = new GimblObjectChan<StringMessage>(name, SetMaterial);
            SetTexChan = new GimblObjectChan<StringMessage>(name, SetTexture);
            SetOpacChan = new GimblObjectChan<FloatMsg>(name, SetOpacity);
            SetVisChan = new GimblObjectChan<BoolMsg>(name, SetVisibility);
            PlaySoundChan = new GimblObjectChan<BoolMsg>(name, PlaySound);
            SetBrightChan = new GimblObjectChan<FloatMsg>(name, SetBrightness);
            BlinkChan = new GimblObjectChan<BlinkMsg>(name, Blink);
        }

        void Update()
        {
            GetPosChan.Test();
            MoveChan.Test();
            MoveToChan.Test();
            SetColorChan.Test();
            SetMatChan.Test();
            SetTexChan.Test();
            RotateChan.Test();
            RotateToChan.Test();
            SetOpacChan.Test();
            SetVisChan.Test();
            PlaySoundChan.Test();
            SetBrightChan.Test();
            BlinkChan.Test();
        }

        void Move(FloatArray msg)
        {
            if (msg.array[3] == 1)
                transform.Translate(msg.array[0], msg.array[1], msg.array[2],Space.Self);
            else
                transform.Translate(msg.array[0], msg.array[1], msg.array[2], Space.World);
        }

        void MoveTo(FloatArray msg)
        {
            transform.position = new Vector3(msg.array[0], msg.array[1], msg.array[2]); ;
        }

        void Rotate(FloatMsg msg) { transform.Rotate(new Vector3(0, msg.value, 0)); }
        void RotateTo(FloatMsg msg) { transform.rotation = Quaternion.Euler(0, msg.value, 0); }

        void SetColor(FloatArray msg)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                if (material != null)
                {

                    Color color = material.color;
                    color.r = msg.array[0];
                    color.g = msg.array[1];
                    color.b = msg.array[2];
                    material.color = color;
                }
            }
            else
            {
                Debug.LogError(string.Format("{0}: Tried to set color but object has no direct mesh (Actor?)", name));
            }
        }

        void SetMaterial(StringMessage msg)
        {
            Material mat = Resources.Load<Material>(msg.strMsg);
            if (mat != null)
                GetComponent<MeshRenderer>().material = mat;
            else
                Debug.LogError(string.Format("{0}: Could not find Material {1}", name, msg.strMsg));
        }

        void SetTexture(StringMessage msg)
        {
            Texture2D tex = Resources.Load<Texture2D>(msg.strMsg);
            if (tex != null)
                GetComponent<MeshRenderer>().material.mainTexture = tex;
            else
                Debug.LogError(string.Format("{0}: Could not find Texture {1}", name, msg.strMsg));
        }

        void OnTriggerEnter(Collider other) { Debug.Log(name); ColChan.Send(new ColliderMessage() { collider = other.name }); }
        void OnControllerColliderHit(ControllerColliderHit hit) { ColChan.Send(new ColliderMessage() { collider = hit.collider.name}); }
        void GetPosition(FloatArray msg)
        {
            PosChan.Send(new PositionMessage() { position = new float[] { transform.position.x, transform.position.y, transform.position.z } });
        }

        void SetOpacity(FloatMsg msg)
        {
            Material mat = GetComponent<MeshRenderer>().material;
            Color color = mat.color;
            color.a = msg.value;
            mat.color = color;
        }

        void SetVisibility(BoolMsg msg) { GetComponent<MeshRenderer>().enabled = msg.flag; }

        void PlaySound(BoolMsg msg)
        {
            if (msg.flag)
                GetComponent<AudioSource>().Play();
            else
                GetComponent<AudioSource>().Stop();
        }

        void SetBrightness(FloatMsg msg)
        {
            ActorObject act = GetComponent<ActorObject>();
            if (act != null)
            {
                DisplayObject disp = act.display;
                if (disp != null)
                {
                    disp.currentBrightness = msg.value;
                }
                else { Debug.LogError(string.Format("{0}: Tried to set screen brightness but object has no display attached.", name)); }
            }
            else
            {
                Debug.LogError(string.Format("{0}: Tried to set screen brightness but object is not an actor.", name));
            }

        }

        void Blink(BlinkMsg msg)
        {
            ActorObject act = GetComponent<ActorObject>();
            if (act != null)
            {
                act.Blink(msg.darkTime, msg.fadeTime, msg.disable);
            }
            else
            {
                Debug.LogError(string.Format("{0}: Tried to blink but object is not an actor.", name));
            }
        }
    }
}