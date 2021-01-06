using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class PerspectiveProjection : MonoBehaviour
{
    public GameObject projectionScreen;
    public Gimbl.DisplayObject dispObj;
    public bool estimateViewFrustrum = true;
    public bool setNearClipPlane = true;
    public float nearClipDistanceOffset = -0.01f;
    public bool isDebug = false;
    private string meshType;
    private Camera cameraComponent;
    private Matrix4x4 p; // projection matrix.
    private Matrix4x4 rm; // rotation matrix.
    private Matrix4x4 tm; // translation matrix.
    private Quaternion q;

    public Material material;

    // Start is called before the first frame update
    void Awake()
    {
        if (material == null) { material = new Material(Shader.Find("Hidden/BrightnessShader")); }
        dispObj = GetComponentInParent<Gimbl.DisplayObject>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        UpdateView();
    }

    public void UpdateView()
    {
        // Get components.
        if (meshType == null) { meshType = projectionScreen.GetComponent<MeshFilter>().sharedMesh.name; }
        if (cameraComponent == null) { cameraComponent = gameObject.GetComponent<Camera>(); }

        // Get view.
        if (projectionScreen != null && cameraComponent != null)
        {
            Vector3 pa = new Vector3(); Vector3 pb = new Vector3(); Vector3 pc = new Vector3();
            switch (meshType)
            {
                case "Plane":
                    // lower-left.
                    pa = projectionScreen.transform.TransformPoint(new Vector3(-5.0f, 0.0f, -5.0f));
                    // lower-right.
                    pb = projectionScreen.transform.TransformPoint(new Vector3(5.0f, 0.0f, -5.0f));
                    // upper-left.
                    pc = projectionScreen.transform.TransformPoint(new Vector3(-5.0f, 0.0f, 5.0f));
                    break;
                case "Quad":
                    // lower-left.
                    pa = projectionScreen.transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0.0f));
                    // lower-right.
                    pb = projectionScreen.transform.TransformPoint(new Vector3(0.5f, -0.5f, 0.0f));
                    // upper-left.
                    pc = projectionScreen.transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0.0f));
                    break;
            }

            //eye position.
            Vector3 pe = transform.position;
            //distance near/far clipping plane.
            float n = cameraComponent.nearClipPlane;
            float f = cameraComponent.farClipPlane;

            //distances.
            Vector3 vr = pb - pa; // right axis of screen.
            Vector3 vu = pc - pa; // up axis of screen.
            Vector3 va = pa - pe; // from pe to pa.
            Vector3 vb = pb - pe; // from pe to pb.
            Vector3 vc = pc - pe; // from pe to pc.

            //Check facing backface of plane.
            if (Vector3.Dot(-Vector3.Cross(va, vc), vb) < 0.0)
            {
                if (isDebug)
                {
                    Debug.Log("Facing backface of plane");
                }
                //mirror points along z axis (most users expect x axis to stay fixed).
                vu = -vu;
                pa = pc;
                pb = pa + vr;
                pc = pa + vu;
                va = pa - pe;
                vb = pb - pe;
                vc = pc - pe;
            }
            else
            {
                if (isDebug)
                {
                    Debug.Log("Not Facing backface of plane");
                }
            }

            // Screen distances.
            vr.Normalize();
            vu.Normalize();
            Vector3 vn = -Vector3.Cross(vr, vu); // Normal vector of screen. Need minus sign because of unities left-handed coordinate system.

            float d = -Vector3.Dot(va, vn); //distance from eye to screen.
            if (setNearClipPlane)
            {
                n = d + nearClipDistanceOffset;
                cameraComponent.nearClipPlane = n;
            }
            float l = Vector3.Dot(vr, va) * n / d; //distance to left screen edge.
            float r = Vector3.Dot(vr, vb) * n / d; //distance to right screen edge.
            float b = Vector3.Dot(vu, va) * n / d; //distance to bottom screen edge.
            float t = Vector3.Dot(vu, vc) * n / d; //distance to top screen edge.

            //Projection matrix.

            p[0, 0] = 2.0f * n / (r - l);
            p[0, 1] = 0.0f;
            p[0, 2] = (r + l) / (r - l);
            p[0, 3] = 0.0f;

            p[1, 0] = 0.0f;
            p[1, 1] = 2.0f * n / (t - b);
            p[1, 2] = (t + b) / (t - b);
            p[1, 3] = 0.0f;

            p[2, 0] = 0.0f;
            p[2, 1] = 0.0f;
            p[2, 2] = (f + n) / (n - f);
            p[2, 3] = 2.0f * f * n / (n - f);

            p[3, 0] = 0.0f;
            p[3, 1] = 0.0f;
            p[3, 2] = -1.0f;
            p[3, 3] = 0.0f;

            //Rotation matrix.
            rm[0, 0] = vr.x;
            rm[0, 1] = vr.y;
            rm[0, 2] = vr.z;
            rm[0, 3] = 0.0f;

            rm[1, 0] = vu.x;
            rm[1, 1] = vu.y;
            rm[1, 2] = vu.z;
            rm[1, 3] = 0.0f;

            rm[2, 0] = vn.x;
            rm[2, 1] = vn.y;
            rm[2, 2] = vn.z;
            rm[2, 3] = 0.0f;

            rm[3, 0] = 0.0f;
            rm[3, 1] = 0.0f;
            rm[3, 2] = 0.0f;
            rm[3, 3] = 1.0f;

            //translation matrix.
            tm[0, 0] = 1.0f;
            tm[0, 1] = 0.0f;
            tm[0, 2] = 0.0f;
            tm[0, 3] = -pe.x;

            tm[1, 0] = 0.0f;
            tm[1, 1] = 1.0f;
            tm[1, 2] = 0.0f;
            tm[1, 3] = -pe.y;

            tm[2, 0] = 0.0f;
            tm[2, 1] = 0.0f;
            tm[2, 2] = 1.0f;
            tm[2, 3] = -pe.z;

            tm[3, 0] = 0.0f;
            tm[3, 1] = 0.0f;
            tm[3, 2] = 0.0f;
            tm[3, 3] = 1.0f;

            // set matrices
            cameraComponent.projectionMatrix = p;
            cameraComponent.worldToCameraMatrix = rm * tm;
            // The original paper puts everything into the projection 
            // matrix (i.e. sets it to p * rm * tm and the other 
            // matrix to the identity), but this doesn't appear to 
            // work with Unity's shadow maps.

            if (estimateViewFrustrum)
            {
                // rotate camera to screen for culling to work
                q.SetLookRotation((0.5f * (pb + pc) - pe), vu);
                // look at center of screen
                cameraComponent.transform.rotation = q;

                // set fieldOfView to a conservative estimate 
                // to make frustum tall enough
                if (cameraComponent.aspect >= 1.0)
                {
                    cameraComponent.fieldOfView = Mathf.Rad2Deg *
                       Mathf.Atan(((pb - pa).magnitude + (pc - pa).magnitude)
                       / va.magnitude);
                }
                else
                {
                    // take the camera aspect into account to 
                    // make the frustum wide enough 
                    cameraComponent.fieldOfView =
                       Mathf.Rad2Deg / cameraComponent.aspect *
                       Mathf.Atan(((pb - pa).magnitude + (pc - pa).magnitude)
                       / va.magnitude);
                }
            }
        }
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (dispObj.settings.isActive) { material.SetFloat("_brightness", dispObj.currentBrightness); }
        else { material.SetFloat("_brightness", 0); }
        Graphics.Blit(source, destination, material);
    }

}
