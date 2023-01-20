using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

public class ElectricalSwitchingSerialHandler : MonoBehaviour
{
    public string portName = "COM3";  //変更する必要あり
    public int baudRate = 9600;  // ボーレート(Arduinoに記述したものに合わせる)

    private SerialPort serialPort_;
    private Thread thread_;
    private bool isRunning_ = false;

    private string message_;
    private bool isNewMessageReceived_ = false;

    void Awake()
    {
        Open();
        DontDestroyOnLoad(this.gameObject);
    }

    //void Update()
    //{
    //    if (isNewMessageReceived_)
    //    {
    //        OnDataReceived(message_);
    //    }
    //    isNewMessageReceived_ = false;
    //}

    void OnDestroy()
    {
        SendTurnOff();
        Close();
    }

    private void Open()
    {
        serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        serialPort_.Open();

        isRunning_ = true;

        thread_ = new Thread(Read);
        thread_.Start();
    }

    private void Close()
    {
        isNewMessageReceived_ = false;
        isRunning_ = false;

        if (thread_ != null && thread_.IsAlive)
        {
            thread_.Join();
        }

        if (serialPort_ != null && serialPort_.IsOpen)
        {
            serialPort_.Close();
            serialPort_.Dispose();
        }
    }

    private void Read()
    {
        while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                message_ = serialPort_.ReadLine();
                isNewMessageReceived_ = true;
                Debug.Log(message_);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }
    }

    public void SendTurnOnLeft()
    {
        try
        {
            Debug.Log("左スイッチ");
            serialPort_.Write("Left");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    public void SendTurnOnRight()
    {
        try
        {
            Debug.Log("右スイッチ");
            serialPort_.Write("Right");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    public void SendTurnOff()
    {
        try
        {
            serialPort_.Write("off");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}