using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;

public class myPlayerUI : MonoBehaviour {

    private InputField playerInput;
    private Text ShowResult;
    private Text ShowInvest1;
    private Text ShowInvest2;
    private Text PuzzleScore;
    private Slider slider;
    private int time;
    private int myTempScore;
    private float timeOffset;

    string headFileName;
    string filename;
    private int puzzleNum;
   // [SyncVar(hook = "StartDaytraderGame")]private int DaytraderNum;
    private int tempVarL;
    private int tempVarR;
    private float tempVar;
    public int myInvestOnServer;
    public float myDTResultOnServer;
    private int myInvest;
    JPPuzzleController playerGame;
    string LogGameNameAndNum;
    public GameObject offsetPostion;
    public puzzlePiecesSpawner mySpawner;

    // Use this for initialization
    void Awake() {
        tempVar = 30.0f;
        myInvestOnServer = 0;   
        tempVarL = 0;
        tempVarR = 0;
        myInvest = 0;
     //   DaytraderNum = -1;
        puzzleNum = -1;// array start with 0;
        myTempScore = 0;

        headFileName = System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString() + System.DateTime.Now.Day.ToString() + "_"
      + System.DateTime.Now.Hour.ToString() + "-" + System.DateTime.Now.Minute.ToString() + "-" + System.DateTime.Now.Second.ToString() + "headpos.txt";
        File.AppendAllText(headFileName,
                            "HeadX\t" +
                            "HeadY\t" +
                            "HeadZ\t" +
                            "Timestamp\t" +
                            System.Environment.NewLine);

    }


    int interval = 1;
    float nextTime = 0;

    // Update is called once per frame, log the head position every 10 seconds.
    void Update() {
        //if (!isLocalPlayer)
        //    return;
        if (Time.time >= nextTime) {      
            string HeadMessage = offsetPostion.transform.position.x.ToString() + "\t" +
                                 offsetPostion.transform.position.y.ToString() + "\t" +
                                  offsetPostion.transform.position.z.ToString() + "\t" +
                                  Time.time.ToString() + "\t"+ System.Environment.NewLine;
            File.AppendAllText(headFileName, HeadMessage);
            nextTime += interval;

        }

    }

    public void OnStartLocalPlayer() {

        GameObject.Find("PuzzleButtom").GetComponent<Button>().onClick.RemoveAllListeners();
        GameObject.Find("PuzzleButtom").GetComponent<Button>().onClick.AddListener(changepuzzleNum);
        GameObject.Find("WinButton").GetComponent<Button>().onClick.RemoveAllListeners();
        GameObject.Find("WinButton").GetComponent<Button>().onClick.AddListener(myPuzzleOverallScoreIn);
        GameObject.Find("LostButton").GetComponent<Button>().onClick.RemoveAllListeners();
        GameObject.Find("LostButton").GetComponent<Button>().onClick.AddListener(myPuzzleOverallScoreDe);

     //   GameObject.Find("InvestmentButton").GetComponent<Button>().onClick.RemoveAllListeners();
     //   GameObject.Find("InvestmentButton").GetComponent<Button>().onClick.AddListener(myInvestment);
     //   GameObject.Find("ResultBotton").GetComponent<Button>().onClick.RemoveAllListeners();
     //   GameObject.Find("ResultBotton").GetComponent<Button>().onClick.AddListener(myCalculate);
    //    ShowInvest1 = GameObject.Find("ShowInvest1").GetComponent<Text>();
    //    GameObject.Find("DaytraderButton").GetComponent<Button>().onClick.RemoveAllListeners();
    //    GameObject.Find("DaytraderButton").GetComponent<Button>().onClick.AddListener(changeDTNum);


        filename = System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString() + System.DateTime.Now.Day.ToString() + "_"
            + System.DateTime.Now.Hour.ToString() + "-" + System.DateTime.Now.Minute.ToString() + "-" + System.DateTime.Now.Second.ToString() + ".txt";
        File.AppendAllText(filename,
                            "LogGameNameAndNum\t" +
                            "LogEachAroundAction\t" +
                            "LogResult\t" +
                            "TimeReal\t" +
                            System.Environment.NewLine);



    }
//both local player and server???
    public void changepuzzleNum() {
        puzzleNum++;    
    }

  /*  public void changeDTNum() {
        DaytraderNum++;
    }*/

    public void StartPuzzleGame(int PNum) {
        slider = GameObject.Find("TimeSlider").GetComponent<Slider>();
        slider.GetComponentInChildren<Text>().text= "Puzzle no." + PNum.ToString();
        //    LogGameNameAndNum = slider.GetComponentInChildren<Text>().text + "\t";
        File.AppendAllText(headFileName, "Puzzle no." + PNum.ToString() + System.Environment.NewLine);
        StartCoroutine(TimerStart(300));


    }

    //drived by host player
  /*  public void StartDaytraderGame(int DNum) {
        if (DNum < 0)
            return;
        slider = GameObject.Find("TimeSlider").GetComponent<Slider>();
        slider.GetComponentInChildren<Text>().text = "Daytrader no. " + DNum.ToString();
        //   LogGameNameAndNum = slider.GetComponentInChildren<Text>().text + "\t";
        File.AppendAllText(headFileName, "Puzzle no." + DNum.ToString() + System.Environment.NewLine);
        StartCoroutine(TimerStart(60));
    }
*/
    public void puzzleGameOver()
    {
        //game over
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Piece");
        for (int i = 0; i < enemies.Length; i++)
        {
           // Destroy(enemies[i]);
           // print("my enemy" + enemies[i].transform.name);
           int myNumber = enemies[i].GetComponent<myEnemyMove>().MyCount;
            enemies[i].GetComponent<Renderer>().material.mainTexture = null;
        }

    }
    //decrease continiously the timer (= the slider), and if = 0 ==> gameover
    IEnumerator TimerStart(int time) { 
        slider.maxValue = time;
        slider.value = time;
        timeOffset = Time.realtimeSinceStartup;
        while (true) {

            // float timer = 0.01f;

            slider.value -= Time.deltaTime;
            //if the slider == 0 ===> game over
            if (slider.value == 0) {
                break;
            }

            yield return new WaitForSeconds(0.01f);
        }

        puzzleGameOver();

    //    CmdnewPuzzleOnServer();

    }

   /* [Command]
    void CmdnewPuzzleOnServer()
    {
        mySpawner = GameObject.Find("spawner").GetComponent<puzzlePiecesSpawner>();
        mySpawner.spawnNewPuzzle();
    }*/


    //drived by host player
    public void changeTexture(int PNum) {

        if (PNum <0 )
            return;
        playerGame = GameObject.Find("PuzzleJP").GetComponent<JPPuzzleController>();
        playerGame.newPuzzle(PNum);
        StartPuzzleGame(PNum);
    }

    public void myPuzzleOverallScoreIn() {
        //if (!isLocalPlayer)
        //    return;
        PuzzleScore = GameObject.Find("PuzzleScore").GetComponent<Text>();
        myTempScore++;
        PuzzleScore.text = "Overall Score: " + myTempScore.ToString();
        slider = GameObject.Find("TimeSlider").GetComponent<Slider>();
        LogGameNameAndNum = slider.GetComponentInChildren<Text>().text + "\t";
        string LogEachAroundAction = "win \t";
        string LogResult = myTempScore.ToString()+ "\t";
        string timestamp = (Time.realtimeSinceStartup - timeOffset).ToString() + "\t";
        string logmessage = LogGameNameAndNum + LogEachAroundAction + LogResult + timestamp + System.Environment.NewLine;
        Log(logmessage);

    }

    public void myPuzzleOverallScoreDe() {
        //if (!isLocalPlayer)
            //return;
        PuzzleScore = GameObject.Find("PuzzleScore").GetComponent<Text>();
        myTempScore--;
        PuzzleScore.text = "Overall Score: " +  myTempScore.ToString();
        slider = GameObject.Find("TimeSlider").GetComponent<Slider>();
        LogGameNameAndNum = slider.GetComponentInChildren<Text>().text + "\t";
        string LogEachAroundAction = "lost \t";
        string LogResult = myTempScore.ToString() + "\t";
        string timestamp = (Time.realtimeSinceStartup - timeOffset).ToString() + "\t";
        string logmessage = LogGameNameAndNum + LogEachAroundAction + LogResult + timestamp + System.Environment.NewLine;
        Log(logmessage);
    }

    /// <summary>
    /// //////////////////////////////////////////////////////
    /// </summary>
 /*   public void Investment() {
        if (!isLocalPlayer)
            return;
            playerInput = GameObject.Find("PlayerInvest").GetComponent<InputField>();
            myInvest = int.Parse(playerInput.text);
        ShowInvest1.text = gameObject.name + ": " + myInvest.ToString();
        CmdSendtoServer(myInvest);
        
    }

   public void myInvestment() {

        Investment();
      //  CmdInvestment();
    }

    [Command]
    void CmdSendtoServer(int myIn) {
        myInvestOnServer = myIn;
      //  print("sent C" + myIn.ToString());
    }

    [Command]
    void CmdSendDTResulttoServer(float myDTResult) {
        myDTResultOnServer = myDTResult;
        //  print("sent C" + myIn.ToString());
    }
    public void myCalculate() {
        Calculate();
    }

    public void Calculate() {
        if (!isLocalPlayer)
            return;
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++) {
            // check if this player is remote player or not
            int testPlayer = int.Parse(players[i].GetComponent<NetworkIdentity>().netId.ToString());
            int thisPlayer = int.Parse(gameObject.GetComponent<NetworkIdentity>().netId.ToString());
            if (thisPlayer != testPlayer) {
                print("before calculate" + tempVar);
                tempVarL = myInvestOnServer;
                tempVarR = players[i].GetComponent<myPlayerUI>().myInvestOnServer;
                ShowInvest2 = GameObject.Find("ShowInvest2").GetComponent<Text>();
                ShowInvest2.text = players[i].name + ": " + tempVarR.ToString();
                tempVar = (tempVar-tempVarL) * 2 + (tempVarL + tempVarR) * 3 / 2;
                print("after calculate" + tempVar);
                CmdSendDTResulttoServer(tempVar);
                if (DaytraderNum % 5 == 4) {

                    if (myDTResultOnServer > players[i].GetComponent<myPlayerUI>().myDTResultOnServer) {
                        tempVar = tempVar + 300;
                    }
                    if (myDTResultOnServer == players[i].GetComponent<myPlayerUI>().myDTResultOnServer) {
                        tempVar = tempVar + 150;
                    }
                    CmdSendDTResulttoServer(tempVar);
                }

                // print(players[i].GetComponent<myPlayerUI>().myInvestOnServer);
            }

        }
        ShowResult = GameObject.Find("ShowResult").GetComponent<Text>();
        ShowResult.text = tempVar.ToString();

        //Making a log for local palyer
        slider = GameObject.Find("TimeSlider").GetComponent<Slider>();
        LogGameNameAndNum = slider.GetComponentInChildren<Text>().text + "\t";
        //string LogGameNameAndNum = "Daytrader no." + DaytraderNum.ToString() + "\t";
        string LogEachAroundAction = myInvestOnServer.ToString() + "\t";
        string LogResult = tempVar.ToString() + "\t";
        string timestamp = (Time.realtimeSinceStartup - timeOffset).ToString() + "\t";
        string logmessage = LogGameNameAndNum + LogEachAroundAction + LogResult + timestamp + System.Environment.NewLine;
        Log(logmessage);
        //rest everything after 5 of them
        if (DaytraderNum % 5 == 4) {
            tempVar = 30.0f;
            myInvestOnServer = 0;
            tempVarL = 0;
            tempVarR = 0;
            myInvest = 0;
        }
        }

    */


    void Log(string Message) {
        File.AppendAllText(filename, Message);
    }

}
