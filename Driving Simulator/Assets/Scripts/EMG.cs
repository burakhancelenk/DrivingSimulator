using System.Collections;
using System.Collections.Generic;
using System.IO ;
using UnityEngine;
using UnityEngine.UI;
using LockingPolicy = Thalmic.Myo.LockingPolicy;
using Pose = Thalmic.Myo.Pose;
using UnlockType = Thalmic.Myo.UnlockType;
using VibrationType = Thalmic.Myo.VibrationType;
public class EMG : MonoBehaviour {

    public Image[] EmgGraphs ;
    public Text[] EMGValueTexts ;
    public Text MYOStat ;
    private ThalmicMyo thalmicMyo ;
    private StreamWriter sw ;
    private float time ;


    private void Start()
    {
        thalmicMyo = GameObject.Find("Hub - 1 Myo").transform.Find("Myo").GetComponent<ThalmicMyo>() ;
        time = 0 ;
        sw = File.AppendText("MYORecord.txt") ;
        
        if (thalmicMyo.armSynced)
        {
            MYOStat.text = "Connected" ;
        }
    }


    private void Update()
    {
        time += Time.deltaTime ;
        for(int i=0; i <= EmgGraphs.Length - 1; i++)
        {
            if (thalmicMyo.emg[i] >= 0)
            {
                EmgGraphs[i].rectTransform.localScale = Vector3.forward+Vector3.up*thalmicMyo.emg[i]/100;
            }
            else
            {
                EmgGraphs[i].rectTransform.localScale = Vector3.forward ;
            }

            EMGValueTexts[i].text = thalmicMyo.emg[i].ToString() ;

        }
        sw.WriteLine("Time : "+time+" EMG1 "+thalmicMyo.emg[0]+" , "+"EMG2 "+thalmicMyo.emg[1]+" , "+"EMG3 "+thalmicMyo.emg[2]+" , "+"EMG4 "+thalmicMyo.emg[3]+" , "
                     +"EMG5 "+thalmicMyo.emg[4]+" , "+"EMG6 "+thalmicMyo.emg[5]+" , "+"EMG7 "+thalmicMyo.emg[6]+" , "+"EMG8 "+thalmicMyo.emg[7]);
        
    }

    public void CloseEMG()
    {
        sw.Flush();
        sw.Close();
    }
    

}
