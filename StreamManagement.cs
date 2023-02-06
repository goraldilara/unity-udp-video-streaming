using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class StreamManagement : MonoBehaviour
{
    //video streaming attributes
    public WebCamTexture webCamTexture = null;
    public RawImage rawImage;
    public Button streamButton;
    public Button watchButton;

    //socket attributes
    //server connection
    string ipAddress = "172.20.10.3";
    static int sendPort = 2020;
    static int receivePort = 2021;

    UdpClient sendClient;
    static private UdpClient receiveClient;

    private byte[] bufferData;
    IPEndPoint RemoteIpEndPoint;

    byte[] receiveBytes;

    private byte[] textureBytes;

    bool connected = false;
    bool isBroadcasting = false;
    bool isWatching = false;

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            ////text socket initialization
            sendClient = new UdpClient();
            receiveClient = new UdpClient(2021);

            ////text send constructor
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ipAddress), sendPort);
            sendClient.Connect(ep);

            bufferData = new byte[62024];
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), receivePort);

            connected = true;
        }
        catch (Exception)
        {
            Debug.Log("neden bilinmior yine");
            connected = false;
        }
        finally
        {
            //camera permission
#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }


            //link the connection between server
            LinkTheConnection();
#endif
            //start broadcast on click
            streamButton.onClick.AddListener(StartBroadcast);

            //start video streaming from server
            watchButton.onClick.AddListener(StartWatching);

        }
    }

    void LinkTheConnection()
    {
        connected = true;
        string firstMessage;

        if(connected == true)
        {
            firstMessage = "New client connect dilaragoral";
            byte[] data = Encoding.ASCII.GetBytes(firstMessage);
            sendClient.Send(data, data.Length);
        }
    }

    void StartBroadcast()
    {
        //if user is not broadcasting, start the broadcast
        if(isBroadcasting == false)
        {
            isBroadcasting = true;
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length != 0)
            {
                webCamTexture = new WebCamTexture();

                //set raw image component
                rawImage.texture = webCamTexture;
                rawImage.material.mainTexture = webCamTexture;

                rawImage.enabled = true;
                //start the streaming
                webCamTexture.Play();
            }
            else
            {
                GameObject.Find("StreamButton").GetComponentInChildren<TMP_Text>().text = "device bulunamadý";
                //plane.SetActive(false);
            }
        }
        //if user wants to stop broadcasting, stop the camera
        else
        {
            isBroadcasting = false;
            webCamTexture.Stop();
            rawImage.enabled = false;
        }

        
    }

    void StartWatching()
    {
        //if user is not watching, start the broadcast
        if (isWatching == false)
        {
            byte[] data = Encoding.ASCII.GetBytes("Start Watching");
            sendClient.Send(data, data.Length);
            isWatching = true;
            rawImage.enabled = true;
        }
        //if user wants to stop watching, stop the packet receiving
        else
        {
            isWatching = false;
            rawImage.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(isBroadcasting == true)
        {
            //convert webcam texture into byte array
            Texture2D tex = new Texture2D(webCamTexture.width, webCamTexture.height);
            tex.SetPixels(webCamTexture.GetPixels());
            tex.Apply();
            byte[] bytes = tex.EncodeToJPG();

            //push streaming into the packet frame by frame
            if (bytes.Length != 0)
            {
                //send
                sendClient.Send(bytes, bytes.Length);
            }
        }

        if(isWatching == true)
        {
            //get datagram packet from server
            if (receiveClient.Available > 0)
            {
                Byte[] receiveBytes = receiveClient.Receive(ref RemoteIpEndPoint);

                //packet checking
                //if (receiveBytes.Length != 0)
                //{
                //    GameObject.Find("WatchButton").GetComponentInChildren<TMP_Text>().text = "veri alýndý";
                //}

                //byte array to texture2d
                Texture2D receivedTex = new Texture2D(16, 16, TextureFormat.PVRTC_RGBA4, false);
                receivedTex.LoadImage(receiveBytes);
                rawImage.texture = receivedTex;
            }
        }
        
    }
}
