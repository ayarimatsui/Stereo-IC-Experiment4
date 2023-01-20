using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Text;
using System;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

public class TestManager : MonoBehaviour
{
    [Header("Electrical Stimulation")]
    [Space(20)]
    public int Current = 1800;
    public int Frequency;
    public WaveForms WaveForm;
    public int Duty;
    public int TransitionDuration;
    public TransitionForms TransitionForm;
    public int Duration = 2;


    public enum WaveForms
    {
        square_bipole,
        square_positive,
        square_negative,
        direct_positive,
        direct_negative,
        sin,
        trapezoidal_positive,
        trapezoidal_negative
    }

    public enum TransitionForms
    {
        constant,
        linear,
        smooth,
        lineardecay,
        smoothdecay
    }

    private GameObject SeeduinoSerialHandlerObject;
    private SeeduinoSerialHandler seeduinoSerialHandler;
    private GameObject ElectricalSwitchingSerialHandlerObject;
    private ElectricalSwitchingSerialHandler electricalSwitchingSerialHandler;


    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        SeeduinoSerialHandlerObject = GameObject.Find("SeeduinoSerialHandler");
        seeduinoSerialHandler = SeeduinoSerialHandlerObject.GetComponent<SeeduinoSerialHandler>();
        ElectricalSwitchingSerialHandlerObject = GameObject.Find("ElectricalSwitchingSerialHandler");
        electricalSwitchingSerialHandler = ElectricalSwitchingSerialHandlerObject.GetComponent<ElectricalSwitchingSerialHandler>();
    }

    public void LeftGNSClicked()
    {
        StartCoroutine(Stimulate(true));
    }

    public void RightGNSClicked()
    {
        StartCoroutine(Stimulate(false));
    }

    public IEnumerator Stimulate(bool leftGNS)
    {
        int waveForm;
        if (WaveForm == WaveForms.trapezoidal_negative)
        {
            waveForm = (int)WaveForms.direct_negative;
        }
        else if (WaveForm == WaveForms.trapezoidal_positive)
        {
            waveForm = (int)WaveForms.direct_positive;
        }
        else
        {
            waveForm = (int)WaveForm;
        }

        if (leftGNS)
        {
            electricalSwitchingSerialHandler.SendTurnOnLeft();
        }
        else
        {
            electricalSwitchingSerialHandler.SendTurnOnRight();
        }

        SendElectricalStimulation(Current, Frequency, waveForm, Duty, TransitionDuration, (int)TransitionForm);
        Debug.Log("STRAT");
        yield return new WaitForSeconds(Duration);
        Debug.Log("STOP");
        if (WaveForm == WaveForms.trapezoidal_negative)
        {
            SendElectricalStimulation(Current, Frequency, (int)WaveForms.direct_negative, Duty, TransitionDuration, (int)TransitionForms.lineardecay);
            yield return new WaitForSeconds(1.0f * TransitionDuration / Frequency);
            StopElectricalStimulation();
        }
        else if (WaveForm == WaveForms.trapezoidal_positive)
        {
            SendElectricalStimulation(Current, Frequency, (int)WaveForms.direct_positive, Duty, 5, (int)TransitionForms.lineardecay);
            yield return new WaitForSeconds(1.0f * 5 / Frequency);
            StopElectricalStimulation();
        }
        else
        {
            StopElectricalStimulation();
        }
        electricalSwitchingSerialHandler.SendTurnOff();
    }

    private void SendElectricalStimulation(int current, int frequency, int wave_form, int duty, int transition_duration, int transition_form)
    {
        char channel = '0';

        byte[] buffer = new byte[8];
        int[] Send1 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send2 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send3 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send4 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] Send5 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        int i;

        // 最初の3bitがチャンネル
        if (channel == '1') { Send1[7] = 0; Send1[6] = 0; Send1[5] = 1; }
        else if (channel == '2') { Send1[7] = 0; Send1[6] = 1; Send1[5] = 0; }
        else if (channel == '3') { Send1[7] = 0; Send1[6] = 1; Send1[5] = 1; }
        else if (channel == '0') { Send1[7] = 0; Send1[6] = 0; Send1[5] = 0; }
        else if (channel == '4') { Send1[7] = 1; Send1[6] = 0; Send1[5] = 0; }
        else if (channel == '5') { Send1[7] = 1; Send1[6] = 0; Send1[5] = 1; }
        else if (channel == '6') { Send1[7] = 1; Send1[6] = 1; Send1[5] = 0; }

        // 電流値(正)を送る;1byte目の残り5bitと2byte目の7bit
        for (i = 0; i <= 6; i++)
        {
            Send2[i] = current % 2;
            current = current / 2;
        }
        for (i = 0; i <= 4; i++)
        {
            Send1[i] = current % 2;
            current = current / 2;
        }

        // 周波数を送る;2byte目の残り1bitと3byte目と4byte目の1bit
        Send4[0] = frequency % 2;
        frequency = frequency / 2;
        for (i = 0; i <= 7; i++)
        {
            Send3[i] = frequency % 2;
            frequency = frequency / 2;
        }
        Send2[7] = frequency % 2;
        frequency = frequency / 2;

        // 波形情報を送る; 4byte目の残り3bit
        for (i = 4; i <= 6; i++)
        {
            Send4[i] = wave_form % 2;
            wave_form = wave_form / 2;
        }

        // duty比を送る; 4byte目の残り4bit
        for (i = 0; i <= 3; i++)
        {
            Send4[i] = duty % 2;
            duty = duty / 2;
        }

        // 立ち上げ時間を送る; 5byte目の4bit
        for (i = 4; i <= 7; i++)
        {
            Send5[i] = transition_duration % 2;
            transition_duration = transition_duration / 2;
        }

        // 立ち上げ手法を送る; 5byte目の残り3bit
        for (i = 1; i <= 3; i++)
        {
            Send5[i] = transition_form % 2;
            transition_form = transition_form / 2;
        }

        // 転送データの作成
        buffer[0] = Convert.ToByte(71);
        buffer[1] = Convert.ToByte(Send1[7] * 128 + Send1[6] * 64 + Send1[5] * 32 + Send1[4] * 16 + Send1[3] * 8 + Send1[2] * 4 + Send1[1] * 2 + Send1[0]);
        buffer[2] = Convert.ToByte(Send2[7] * 128 + Send2[6] * 64 + Send2[5] * 32 + Send2[4] * 16 + Send2[3] * 8 + Send2[2] * 4 + Send2[1] * 2 + Send2[0]);
        buffer[3] = Convert.ToByte(Send3[7] * 128 + Send3[6] * 64 + Send3[5] * 32 + Send3[4] * 16 + Send3[3] * 8 + Send3[2] * 4 + Send3[1] * 2 + Send3[0]);
        buffer[4] = Convert.ToByte(Send4[7] * 128 + Send4[6] * 64 + Send4[5] * 32 + Send4[4] * 16 + Send4[3] * 8 + Send4[2] * 4 + Send4[1] * 2 + Send4[0]);
        buffer[5] = Convert.ToByte(Send5[7] * 128 + Send5[6] * 64 + Send5[5] * 32 + Send5[4] * 16 + Send5[3] * 8 + Send5[2] * 4 + Send5[1] * 2 + Send5[0]);

        seeduinoSerialHandler.SendParameters(buffer);
        Task.Delay(1);
    }

    private void StopElectricalStimulation()
    {
        SendElectricalStimulation(0, 0, 0, 0, 0, 0);
    }
}
