using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestManager : MonoBehaviour
{
  [Header("Trial Times")]
  [Space(20)]
  public int Trials = 10;

  [Header("File Name")]
  [Space(20)]
  public string DirectoryName;

  public GameObject StartText;
  public GameObject StimulationTextObject;
  private Text StimulationText;
  public GameObject PerceivedText;
  public GameObject NotPerceivedText;
  public GameObject EndText;

  private int TrialID;
  private bool IsStimulating;
  private GameObject OlfactoryDisplaySerialHandlerObject;
  private OlfactoryDisplaySerialHandler olfactoryDisplaySerialHandler;
  private List<bool> Answers = new List<bool>();

  private const int LeftScentSlot = 4;
  private const int RightScentSlot = 2;
  private const int CenterSlot = 3;
  private const int LeftAirSlot = 5;
  private const int RightAirSlot = 1;

  private TestMode mode;

  private enum TestMode
  {
    Start,
    Waiting,
    Stimulation,
    Evaluating,
    End
  }

  void Awake()
  {
    DontDestroyOnLoad(this.gameObject);
    OlfactoryDisplaySerialHandlerObject = GameObject.Find("OlfactoryDisplaySerialHandler");
    olfactoryDisplaySerialHandler = OlfactoryDisplaySerialHandlerObject.GetComponent<OlfactoryDisplaySerialHandler>();
    StartText.SetActive(true);
    StimulationTextObject.SetActive(false);
    StimulationText = StimulationTextObject.GetComponent<Text>();
    PerceivedText.SetActive(false);
    NotPerceivedText.SetActive(false);
    EndText.SetActive(false);
    mode = TestMode.Start;
    StartCoroutine("Process");
  }


  private IEnumerator Process()
  {
    TrialID = 1;
    MakeResultCSV(DirectoryName);
    yield return new WaitUntil(() => OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch));
    StartText.SetActive(false);

    for (int i = 0; i < 10; i++)
    {
      StimulationTextObject.SetActive(true);
      PerceivedText.SetActive(false);
      NotPerceivedText.SetActive(false);
      mode = TestMode.Waiting;
      float time = 10.0f;
      StimulationText.text = "10秒後に匂いを噴射します";
      StartCoroutine("OlfactoryReset");
      while (time > 0)
      {
        int displayTime = (int)Math.Ceiling((double)time);
        StimulationText.text = displayTime.ToString() + "秒後に匂いを噴射します";
        time -= Time.deltaTime;
        yield return null;
      }

      mode = TestMode.Stimulation;
      StimulationText.text = "匂い提示中";
      olfactoryDisplaySerialHandler.SendSingleAirPumpOn(CenterSlot, 255, true, 2000, 4);
      yield return new WaitForSeconds(2.0f);
      olfactoryDisplaySerialHandler.SendAirPumpOff();

      mode = TestMode.Evaluating;
      StimulationTextObject.SetActive(false);
      PerceivedText.SetActive(true);
      NotPerceivedText.SetActive(true);
      yield return StartCoroutine("Evaluation");

      TrialID += 1;
    }
    mode = TestMode.End;
    PerceivedText.SetActive(false);
    NotPerceivedText.SetActive(false);
    EndText.SetActive(true);
  }

  public IEnumerator Evaluation()
  {
    bool getResult = false;
    bool answer = false;
    while (!getResult)
    {
      if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
      {
        answer = true;
        getResult = true;
      }
      else if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
      {
        answer = false;
        getResult = true;
      }
      yield return null;
    }
    SaveResult(answer);
  }

  public IEnumerator OlfactoryReset()
  {
    olfactoryDisplaySerialHandler.SendSingleAirPumpOn(LeftAirSlot, 255, false, 1500, 0);
    yield return new WaitForSeconds(1.50f);
    olfactoryDisplaySerialHandler.SendSingleAirPumpOn(RightAirSlot, 255, false, 1500, 0);
    yield return new WaitForSeconds(1.50f);
    olfactoryDisplaySerialHandler.SendAirPumpOff();
  }

  private void MakeResultCSV(string directoryName)
  {
    if (!Directory.Exists(directoryName))
    {
      Directory.CreateDirectory(directoryName);
    }

    StreamWriter sw;
    string fileName = directoryName + "/olfactoryTestResult.csv";

    sw = new StreamWriter(fileName, false, Encoding.GetEncoding("Shift_JIS"));
    string[] s1 = { "trialID", "Answer" };
    string s2 = string.Join(",", s1);
    sw.WriteLine(s2);
    sw.Close();
    Debug.Log("Result CSV Created");
  }

  public void SaveResult(bool answer)
  {
    StreamWriter sw;
    string fileName = DirectoryName + "/olfactoryTestResult.csv";

    sw = new StreamWriter(fileName, true, Encoding.GetEncoding("Shift_JIS"));

    string[] s1 = { TrialID.ToString(), answer.ToString() };
    string s2 = string.Join(",", s1);
    sw.WriteLine(s2);
    sw.Close();
  }
}
