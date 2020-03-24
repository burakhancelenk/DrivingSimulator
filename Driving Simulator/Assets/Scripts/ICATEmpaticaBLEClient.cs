/* -- ICAT's Empatica Bluetooth Low Energy(BLE) Comm Client -- *
 * ----------------------------------------------------------- *
 * 0. Attach this to main camera or any empty game object
 * 1. On launch, it tries to connect to the localhost/port20 
 * 	  (You have to change it to your own ip/port combination).
 * 2. Enter the Device ID and connect to device.
 * 3. Select the data streams to log and hit "Log Data"
 * 4. Hit Ctrl+Shift+Z to disconnect at anytime.
 * 
 * Written By: Deba Saha (dpsaha@vt.edu)
 * Virginia Tech, USA.  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System; 
using System.IO;
using System.Diagnostics;
using EmpaticaBLEClient ;
using UnityEngine.UI ;
using Debug = UnityEngine.Debug;

public class ICATEmpaticaBLEClient : MonoBehaviour
{
      //variables	
      private TCPConnection myTCP;	
      private string streamSelected;

      private float time ;
      //public string msgToServer;
      //public string connectToServer;
      private string deviceId ;
      public Text BVPText ;
      public Text TMPText ;
      public Text IBIText ;
      public Text EmpaticaStatText ;
      
      private string savefilename = "EmpaticaRecord.txt";
      private StreamWriter sw ;
  
      //flag to indicate device conection status
      private bool deviceConnected = false;
  
      //flag to indicate if data to be logged to file
      private bool logToFile = false;
  
      void Awake() {		
          //add a copy of TCPConnection to this game object		
          myTCP = gameObject.AddComponent<TCPConnection>();		
      }
      
      void Start () {		
          //DisplayTimerProperties ();
          if (myTCP.socketReady == false) {			
              Debug.Log("Attempting to connect..");
              //Establish TCP connection to server
              myTCP.setupSocket();

              if (myTCP.socketReady && !deviceConnected)
              {
                  SendToServer("device_list");
                  SocketResponse();
                  SendToServer("device_connect "+deviceId);
                  SocketResponse();
                  if (deviceConnected)
                  {
                      SendToServer("device_subscribe bvp ON");
                      SendToServer("device_subscribe tmp ON");
                      SendToServer("device_subscribe ibi ON");
                      logToFile = true ;
                      sw = File.AppendText(savefilename) ;
                      // inform side screen panel...
                      EmpaticaStatText.text = "Connected" ;
                  }
              }
          }
      }
  
      void Update () {		
          //keep checking the server for messages, if a message is received from server, 
          //it gets logged in the Debug console (see function below)	
          time += Time.deltaTime ;
          SocketResponse ();
      }


    public void DisconnectEnpatica()
    {
        if (deviceConnected)
        {
            SendToServer("device_disconnect "+deviceId);
            myTCP.closeSocket();
            sw.Flush();
            sw.Close();
        }
    }
  
      //socket reading script	
      void SocketResponse() {		
          string serverSays = myTCP.readSocket();
          
          if (serverSays != "") {
              if (!deviceConnected && myTCP.socketReady)
              {
                  deviceId = serverSays.Substring(18,6) ;
              }
              
              if (myTCP.socketReady == true && deviceConnected == true && logToFile == true){
                  //inform side screen and write values to file
                  if (serverSays.Substring(3 , 3) == "Bvp")
                  {
                      BVPText.text = serverSays.Split(' ')[2].Trim() ;
                      sw.WriteLine("Time : "+time+" BVP "+BVPText.text);
                  }
                  else if (serverSays.Substring(3 , 3) == "Tmp")
                  {
                      TMPText.text = serverSays.Split(' ')[2].Trim() ;
                      sw.WriteLine("Time : "+time+" TMP "+TMPText.text);
                  }
                  else if (serverSays.Substring(3 , 3) == "Ibi")
                  {
                      IBIText.text = serverSays.Split(' ')[2].Trim() ;
                      sw.WriteLine("Time : "+time+" IBI "+IBIText.text);
                  }
                     
                  sw.WriteLine(serverSays);
                  
              }else{
                  Debug.Log("[SERVER]" + serverSays);
                  string serverConnectOK = @"R device_connect OK";
                  //Check if server response was device_connect OK
                  if (string.CompareOrdinal(Regex.Replace(serverConnectOK,@"\s",""),Regex.Replace(serverSays.Substring(0,serverConnectOK.Length),@"\s","")) == 0){
                      deviceConnected = true;
                  }
              }
          } 
      }
  
      //send message to the server	
      public void SendToServer(string str) {		 
          myTCP.writeSocket(str);		
          Debug.Log ("[CLIENT] " + str);		
      }
    
      
}

