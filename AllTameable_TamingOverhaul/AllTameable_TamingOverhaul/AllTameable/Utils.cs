using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilAlignUp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.up = Vector3.up;
    }

    // Update is called once per frame
    void Update()
    {
        transform.up = Vector3.up;
    }
}

public class UtilColorFade : MonoBehaviour
{
    // Start is called before the first frame update
    public float roughness = 0.5f;
    private static float startime = 0;
    private float lastTime = 0;
    public float cycletime = 1;
    //private Color curCol = new Color(1, 1, 1, 1);
    public string colType = "_Color";
    public Gradient grad = new Gradient();
    //public GradientColorKey[] clrKeys = { new GradientColorKey() };
    //public GradientAlphaKey[] alpKeys = { new GradientAlphaKey() };
    public Material mat;
    void Start()
    {
        startime = Time.time + Random.Range(-cycletime,cycletime);
        //AllTameable.DBG.blogDebug("Start Random="+startime);
    }


    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastTime > roughness)
        {
            float partTime = ((Time.time+startime) % cycletime)/cycletime;
            //AllTameable.DBG.blogDebug(partTime);
            lastTime = Time.time;
            if ((bool)mat)
            {
                mat.SetColor(colType, grad.Evaluate(partTime));
            }

        }
        
    }
}
