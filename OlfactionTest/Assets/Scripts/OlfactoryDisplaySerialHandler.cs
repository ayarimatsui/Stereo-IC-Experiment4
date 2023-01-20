using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System;

public class OlfactoryDisplaySerialHandler : MonoBehaviour
{
    public delegate void SerialDataReceivedEventHandler(string message);
    public event SerialDataReceivedEventHandler OnDataReceived;

    public string portName = "COM4";  //変更する必要あり
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
        SendAirPumpOff();
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
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }
    }

    public void SendParameters(byte[] buffer)
    {
        try
        {
            serialPort_.Write(buffer, 0, buffer.Length);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    public void SendDoubleAirPumpOn(int slot1, int slot2, int intensity, int milliduration)
    {
        SendAirPumpOn(slot1, slot2, intensity, false, milliduration, 5);
    }

    public void SendSingleAirPumpOn(int slot1, int intensity, bool pulseMode, int milliduration, int duty)
    {
        SendAirPumpOn(slot1, 0, intensity, pulseMode, milliduration, duty);
    }

    // slots:1-5, intensity:0-255, pulseMode:bool, milliduration:0-65535, duty:0-10
    public void SendAirPumpOn(int slot1, int slot2, int intensity, bool pulseMode, int milliduration, int duty)
    {
        byte[] buffer = new byte[8];
        int[] Send1 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send2 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send3 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send4 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int i;

        // 1byte目の最初3bitがスロット
        for (i = 5; i <= 7; i++)
        {
            Send1[i] = slot1 % 2;
            slot1 = slot1 / 2;
        }

        // 1byte目の残り5bitと2byte目の最初3bitがintensity
        for (i = 5; i <= 7; i++)
        {
            Send2[i] = intensity % 2;
            intensity = intensity / 2;
        }
        for (i = 0; i <= 4; i++)
        {
            Send1[i] = intensity % 2;
            intensity = intensity / 2;
        }

        // 2byte目の3bitが2つ目のスロット
        for (i = 2; i <= 4; i++)
        {
            Send2[i] = slot2 % 2;
            slot2 = slot2 / 2;
        }

        // 2byte目の内の1bitがPulseMode
        if (pulseMode)
        {
            Send2[1] = 1;
        }
        else
        {
            Send2[1] = 0;
        }

        // 2byte目の残り1bitと3byte目、4byte目の最初4bitがduration
        for (i = 4; i <= 7; i++)
        {
            Send4[i] = milliduration % 2;
            milliduration = milliduration / 2;
        }
        for (i = 0; i <= 7; i++)
        {
            Send3[i] = milliduration % 2;
            milliduration = milliduration / 2;
        }
        Send2[0] = milliduration % 2;

        // 4byte目の残り4bitがduty
        for (i = 0; i <= 3; i++)
        {
            Send4[i] = duty % 2;
            duty = duty / 2;
        }

        // 転送データの作成
        buffer[0] = Convert.ToByte(61);
        buffer[1] = Convert.ToByte(Send1[7] * 128 + Send1[6] * 64 + Send1[5] * 32 + Send1[4] * 16 + Send1[3] * 8 + Send1[2] * 4 + Send1[1] * 2 + Send1[0]);
        buffer[2] = Convert.ToByte(Send2[7] * 128 + Send2[6] * 64 + Send2[5] * 32 + Send2[4] * 16 + Send2[3] * 8 + Send2[2] * 4 + Send2[1] * 2 + Send2[0]);
        buffer[3] = Convert.ToByte(Send3[7] * 128 + Send3[6] * 64 + Send3[5] * 32 + Send3[4] * 16 + Send3[3] * 8 + Send3[2] * 4 + Send3[1] * 2 + Send3[0]);
        buffer[4] = Convert.ToByte(Send4[7] * 128 + Send4[6] * 64 + Send4[5] * 32 + Send4[4] * 16 + Send4[3] * 8 + Send4[2] * 4 + Send4[1] * 2 + Send4[0]);

        SendParameters(buffer);
        Task.Delay(1);
    }

    public void SendAirPumpOff()
    {
        byte[] buffer = new byte[8];
        int[] Send1 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send2 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send3 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send4 = { 0, 0, 0, 0, 0, 0, 0, 0 };

        // 転送データの作成
        buffer[0] = Convert.ToByte(61);
        buffer[1] = Convert.ToByte(Send1[7] * 128 + Send1[6] * 64 + Send1[5] * 32 + Send1[4] * 16 + Send1[3] * 8 + Send1[2] * 4 + Send1[1] * 2 + Send1[0]);
        buffer[2] = Convert.ToByte(Send2[7] * 128 + Send2[6] * 64 + Send2[5] * 32 + Send2[4] * 16 + Send2[3] * 8 + Send2[2] * 4 + Send2[1] * 2 + Send2[0]);
        buffer[3] = Convert.ToByte(Send3[7] * 128 + Send3[6] * 64 + Send3[5] * 32 + Send3[4] * 16 + Send3[3] * 8 + Send3[2] * 4 + Send3[1] * 2 + Send3[0]);
        buffer[4] = Convert.ToByte(Send4[7] * 128 + Send4[6] * 64 + Send4[5] * 32 + Send4[4] * 16 + Send4[3] * 8 + Send4[2] * 4 + Send4[1] * 2 + Send4[0]);

        SendParameters(buffer);
        Task.Delay(1);
    }
}