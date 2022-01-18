using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiveGraphingTool : MonoBehaviour
{
    // This is just an example script for now, ideally I want to
    // implement reflection on this just as I have with the data saving
    // script - then we can view live data as well as save it


    public float timePeriod = 0.1f;
    public AnimationCurve alphaPlot;
    public AnimationCurve clPlot;
    public AnimationCurve alphaCLPlot;
    public AnimationCurve alphaCoPZPlot;
    Aerodynamics aero;
    float time = 0f;
    float timer = 0f;

    // Start is called before the first frame update
    void Start()
    {
        aero = GetComponent<Aerodynamics>();

        // Put on the initial conditions, this generally shoots off so might not be worth doing
        //PlotVars();
    }


    void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        timer += Time.fixedDeltaTime;
        if (timer >= timePeriod)
        {
            PlotVars();
            timer -= timePeriod;
        }
    }

    void PlotVars()
    {
        clPlot.AddKey(time, aero.CL);
        alphaPlot.AddKey(time, aero.alpha);
        alphaCLPlot.AddKey(aero.alpha, aero.CL);
        alphaCoPZPlot.AddKey(aero.alpha, aero.CoP_z);
    }
}
