using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Implementation of a Holt Double Exponential Smoothing filter. The double exponential
/// smooths the curve and predicts.  There is also noise jitter removal
/// </summary>
public class SingleNumberFilter
{
    // The history data.
    private FilterDoubleExponentialData[] history;

    // The transform smoothing parameters for this filter.
    private SmoothParameters smoothParameters;

    // True when the filter parameters are initialized.
    private bool init;
	
	
    /// Initializes a new instance of the class.
    public SingleNumberFilter()
    {
        this.init = false;
    }

    // Initialize the filter with a default set of TransformSmoothParameters.
    public void Init()
    {
        // Specify some defaults
		this.Init(0.5f, 0.5f, 0.5f, 0.05f, 0.04f);
    }

    /// <summary>
    /// Initialize the filter with a set of manually specified TransformSmoothParameters.
    /// </summary>
    /// <param name="smoothingValue">Smoothing = [0..1], lower values is closer to the raw data and more noisy.</param>
    /// <param name="correctionValue">Correction = [0..1], higher values correct faster and feel more responsive.</param>
    /// <param name="predictionValue">Prediction = [0..n], how many frames into the future we want to predict.</param>
    /// <param name="jitterRadiusValue">JitterRadius = The deviation distance in m that defines jitter.</param>
    /// <param name="maxDeviationRadiusValue">MaxDeviation = The maximum distance in m that filtered positions are allowed to deviate from raw data.</param>
    public void Init(float smoothingValue, float correctionValue, float predictionValue, float jitterRadiusValue, float maxDeviationRadiusValue)
    {
        this.smoothParameters = new SmoothParameters();

        this.smoothParameters.fSmoothing = smoothingValue;                   // How much soothing will occur.  Will lag when too high
        this.smoothParameters.fCorrection = correctionValue;                 // How much to correct back from prediction.  Can make things springy
        this.smoothParameters.fPrediction = predictionValue;                 // Amount of prediction into the future to use. Can over shoot when too high
        this.smoothParameters.fJitterRadius = jitterRadiusValue;             // Size of the radius where jitter is removed. Can do too much smoothing when too high
        this.smoothParameters.fMaxDeviationRadius = maxDeviationRadiusValue; // Size of the max prediction radius Can snap back to noisy data when too high
        
		this.Reset();
        this.init = true;
    }

    // Initialize the filter with a set of TransformSmoothParameters.
    public void Init(SmoothParameters smoothingParameters)
    {
        this.smoothParameters = smoothingParameters;
		
        this.Reset();
        this.init = true;
    }

    // Resets the filter to default values.
    public void Reset()
    {
        this.history = new FilterDoubleExponentialData[1];
    }

    // Update the filter with a new data value and smooth.
    public void UpdateFilter(ref float fValue)
    {
        if (this.init == false)
        {
            this.Init();    // initialize with default parameters                
        }

        // Check for divide by zero. Use an epsilon of a 10th of a millimeter
        smoothParameters.fJitterRadius = Math.Max(0.0001f, smoothParameters.fJitterRadius);
        FilterValues(ref fValue, ref smoothParameters);
    }

    // Update the filter for one set of values.  
    protected void FilterValues(ref float fValue, ref SmoothParameters smoothingParameters)
    {
        float filteredState;
        float trend;
        float diffVal;

        float rawState = fValue;
        float prevFilteredState = history[0].FilteredState;
        float prevTrend = history[0].Trend;
        float prevRawState = history[0].RawState;

        // If value is invalid, reset the filter
        if (rawState < 0f)
        {
            history[0].FrameCount = 0;
        }

        // Initial start values
        if (history[0].FrameCount == 0)
        {
            filteredState = rawState;
            trend = 0f;
        }
        else if (this.history[0].FrameCount == 1)
        {
            filteredState = (rawState + prevRawState) * 0.5f;
            diffVal = filteredState - prevFilteredState;
            trend = (diffVal * smoothingParameters.fCorrection) + (prevTrend * (1.0f - smoothingParameters.fCorrection));
        }
        else
        {              
//            // First apply jitter filter
//            diffVal = rawState - prevFilteredState;
//
//            if (diffVal <= smoothingParameters.fJitterRadius)
//            {
//                filteredState = (rawState * (diffVal / smoothingParameters.fJitterRadius)) + (prevFilteredState * (1.0f - (diffVal / smoothingParameters.fJitterRadius)));
//            }
//            else
//            {
//                filteredState = rawState;
//            }
			
            filteredState = rawState;

            // Now the double exponential smoothing filter
            filteredState = (filteredState * (1.0f - smoothingParameters.fSmoothing)) + ((prevFilteredState + prevTrend) * smoothingParameters.fSmoothing);

            diffVal = filteredState - prevFilteredState;
            trend = (diffVal * smoothingParameters.fCorrection) + (prevTrend * (1.0f - smoothingParameters.fCorrection));
        }      

        // Predict into the future to reduce latency
        float predictedState = filteredState + (trend * smoothingParameters.fPrediction);

        // Check that we are not too far away from raw data
        diffVal = predictedState - rawState;

        if (diffVal > smoothingParameters.fMaxDeviationRadius)
        {
            predictedState = (predictedState * (smoothingParameters.fMaxDeviationRadius / diffVal)) + (rawState * (1.0f - (smoothingParameters.fMaxDeviationRadius / diffVal)));
        }

        // Save the data from this frame
        history[0].RawState = rawState;
        history[0].FilteredState = filteredState;
        history[0].Trend = trend;
        history[0].FrameCount++;
        
        // Set the filtered data back into the value
		fValue = predictedState;
    }
	

    // Historical Filter Data.  
    private struct FilterDoubleExponentialData
    {
        // Gets or sets Historical Tracking State.  
        public float RawState;

        // Gets or sets Historical Filtered Tracking State.  
        public float FilteredState;

        // Gets or sets Historical Trend.  
        public float Trend;

        // Gets or sets Historical FrameCount.  
        public uint FrameCount;
    }
}
