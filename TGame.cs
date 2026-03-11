using EFTGame;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//add by ymj
using System.Net.Http;
using System.Text;
//using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UI;


public class TGame : GameBase
{
    public GameObject player;
    [SerializeField] public List<CategoryName> categories;
    [SerializeField] public List<FrameCategoryName> frameCategories;
    public List<ModelsCategory> modelsCategories = new List<ModelsCategory>();
    public GameObject correct, wrong;
    private float timeRemaining;
    public bool gameStarted = false; //可刪
    private int score = 0;
    public Transform spawnArea; // 生成區域
    private float spawnMinTime;  // 生成最小時間
    private float spawnMaxTime;  // 生成最大時間
    public float moveSpeed; // 移動速度 //關卡物件移動速度 by YMJ @20241127
    public GameObject framePrefab; // 外框
    private GameObject parentObject;
    private GameObject categoryConveyorBelt, frameConveyorBelt;
    private List<UnityEngine.Object> AllCubes = new List<UnityEngine.Object>();
    //private int stageCorrect, stageWrong, stageAccuracy;

    //add by ymj
    //private float beginTime, endTime; // 使用TimerManager的時間參數
    private DateTime beginTime, endTime; // 記錄遊戲開始與結束時間
    private LessonData lastSession;


    //add by alan
    private DCCSLevelManager dCCSLevelManager;
    public DCCSLevelConfig currentConfig;// 儲存當前關卡允許出現的模型種類（Category），相當於題庫
    private List<CategoryName> allowedCategories = new List<CategoryName>();// 儲存當前關卡允許出現的外框形狀種類（Frame），相當於題庫
    public List<FrameCategoryName> allowedFrames = new List<FrameCategoryName>();
    private List<GameObject> allowedModelList = new List<GameObject>();
    public List<GameObject> slotModelList = new List<GameObject>();
    // 控制 SpawnCubes() 迴圈的閘門；false 時阻止下一題生成（Both 模式下 Frame 先答後設為 false，等 Category 作答）
    public bool problemAnswered = true;
    private float TGameBeginTime;
    // 教學關卡的子步驟索引（0–5），目前多數教學邏輯已被注解掉
    private int tutorialSubStage = 0;
    // 各類型答錯計數，上傳至伺服器時附帶（frameWrong = 外框答錯，categoryWrong = 類別答錯，modelWrong = 單一模型答錯）
    private int frameWrongCount = 0;
    private int categoryWrongCount = 0;
    private int modelWrongCount = 0;
    // 上一次出題的索引，防止連續出現同一模型或外框
    private int lastModelIndex = -1;
    private int lastFrameIndex = -1;


    // 本場遊戲經過的關卡 ID 列表，上傳結果時附帶
    private List<int> playedLevelIds = new List<int>();
    //public GameObject MenuManager;


    public List<int> AnswerSequence { get; private set; } = new List<int>();
    public List<JunctionPlan> Plans { get; private set; } = new List<JunctionPlan>();



    void Start()
    {
        Debug.Log("TGame Start");
        //TGameLevelManager = TGameLevelManager.Instance;
        TimeToAnswer = 40f;
        spawnMinTime = 7.5f;
        spawnMaxTime = 8.5f;
        moveSpeed = 30f;
        Init();
        //TGameLevelManager.RefreshLevelSelection();
        //if (gameController != null)
        //{
        //    /*if (gameController == null)
        //    {
        //        Debug.LogWarning("gameController 為 null，嘗試從場景中獲取。");
        //        gameController = GameObject.FindObjectOfType<GameController>();
        //    }// add by YMJ @20241126
        //    if (gameSettings == null && gameController != null)
        //    {
        //        gameSettings = gameController.GetGameSettings(); // 假設 GameController 有這個方法
        //    }// add by YMJ @20241126
        //    */
        //    if (gameSettings.GameIndex > 0)
        //    {
        //        moveSpeed = gameSettings.TGame_3dSettings.MoveSpeed;
        //        spawnMinTime = gameSettings.TGame_3dSettings.SpawnMinTime;
        //        spawnMaxTime = gameSettings.TGame_3dSettings.SpawnMaxTime;
        //    }
        //    else  // debug用數據
        //    {
        //        gameSettings.TGame_3dSettings.MoveSpeed = moveSpeed;
        //        gameSettings.TGame_3dSettings.SpawnMinTime = spawnMinTime;
        //        gameSettings.TGame_3dSettings.SpawnMaxTime = spawnMaxTime;
        //    }
        //}

        //correct = GameObject.Find("Correct");
        //wrong = GameObject.Find("Wrong");
        //parentObject = GameObject.Find("ParentObject");
        //categoryConveyorBelt = GameObject.Find("CategoryConveyorBelt");
        //frameConveyorBelt = GameObject.Find("FrameConveyorBelt");
        correct.SetActive(false);
        wrong.SetActive(false);

        //UpdateScore();

        //foreach (CategoryName c in categories)
        //{
        //    ModelsCategory models = new ModelsCategory(c);
        //    modelsCategories.Add(models);
        //}
        GameUI.instance.StartFadeIn();

        //從教學功能開始
        //SkipTutorial = false;
        //if(DateManager.GetDayIndex()!=1) SkipTutorial = true;

        //if (!SkipTutorial) Invoke("ReadyToStartTutorial", 0.5f);
        //else Invoke("ReadyToStart", 0.5f);

        //Invoke("ReadyToStartTutorial", 0.5f);

        GameUI.instance.ClearPanel();

        StartStage1();
        //add by ymj
        //beginTime = timer.GetTime(); // 使用TimerManager記錄遊戲開始時間
        beginTime = DateTime.Now; // 記錄遊戲開始時間
        TGameBeginTime = Time.time;
        //StartCoroutine(InitializeAsync());

        //NumOfProblems = TGameLevelManager.Instance.GetLevelCount();
        //Debug.Log("總共 " + NumOfProblems + " 關");
    }
    private IEnumerator InitializeAsync()
    {
        var task = FetchLastSessionData();
        yield return new WaitUntil(() => task.IsCompleted);

        lastSession = task.Result;
        if (lastSession == null)
        {
            Debug.Log("Get Data Error!!!");
        }
    }

    //add by ymj
    protected override async void ReadyToEndGame()
    {
        endTime = DateTime.Now; // 記錄遊戲結束時間
        int answeredCount = GetAnsweredCount();

        if (answeredCount > 0)
            Accuracy = (int)((float)CorrectCount / (CorrectCount + WrongCount) * 100);
        else
            Accuracy = 100;

        //Debug.Log("startTime:"+beginTime+", endTime: "+endTime+",correct:"+CorrectCount+"("+ stageCorrect+"), wrong: "+WrongCount+"("+ stageWrong + "), accuracy: "+ Accuracy+"("+ stageAccuracy + "), duration: "+((float)(int)(endTime - beginTime).TotalMilliseconds)+", stage: "+NumOfProblems);
        await GameResultSender.SendGameResult(gameName: "TGame", startTime: beginTime, endTime: endTime, correct: CorrectCount, wrong: WrongCount, accuracy: Accuracy, duration: ((float)(int)(endTime - beginTime).TotalMilliseconds), stage: answeredCount, unlockedFlags: new List<string>(), levelsPlayed: playedLevelIds, frameWrongCount: frameWrongCount, categoryWrongCount: categoryWrongCount, modelWrongCount: modelWrongCount);
        //endTime = timer.GetTime(); // 使用TimerManager記錄遊戲結束時間
        //SendGameDataToServer(); // 傳送遊戲數據\
        //MenuManager mm = MenuManager.GetComponent<MenuManager>();
        //GameUI.instance.InfoButton.onClick.AddListener(mm.ReturnToMenu);
        base.ReadyToEndGame();
    }

    protected override async void UploadAbortData()
    {
        endTime = DateTime.Now;

        int answeredCount = GetAnsweredCount();

        if (answeredCount > 0)
            Accuracy = (int)((float)CorrectCount / (CorrectCount + WrongCount) * 100);
        else
            Accuracy = 100;

        await GameResultSender.SendGameResult(
            gameName: "TGame",
            startTime: beginTime,
            endTime: endTime,
            correct: CorrectCount,
            wrong: WrongCount,
            accuracy: Accuracy,
            duration: (float)(int)(endTime - beginTime).TotalMilliseconds,
            stage: answeredCount,
            unlockedFlags: new List<string>(),
            levelsPlayed: playedLevelIds,
            frameWrongCount: frameWrongCount,
            categoryWrongCount: categoryWrongCount,
            modelWrongCount: modelWrongCount
        );
    }
    protected override int GetAnsweredCount()
    {
        return problemIndex; // 正確的作答數
    }



    //add by ymj
    private async void SendGameDataToServer()
    {
        //var lastSession = await FetchLastSessionData();
        int lastId;
        int patientId;
        if (lastSession == null)
        {
            lastId = 501;
            patientId = 501;

        }
        else
        {
            lastId = lastSession?.id ?? 0;
            patientId = lastSession?.patient ?? 0;
        }

        var gameData = new
        {
            sessionId = lastId,
            patient = patientId,
            device = SystemInfo.deviceName.ToString(),
            program = "TGame_精準分類",
            scenario = "TGame_精準分類_" + nowStage,
            begin = beginTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
            end = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
            //accuracy = stageAccuracy
        };

        string jsonData = JsonConvert.SerializeObject(gameData);
        Debug.Log("jsonData= " + jsonData);
        using (HttpClient client = new HttpClient())
        {
            try
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("http://57.182.94.235:8080/Game/api/create/GameScore", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("Data sent successfully!");
                }
                else
                {
                    Debug.LogError("Failed to send data: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception while sending data: " + ex.Message);
            }
        }
    }

    private async Task<LessonData> FetchLastSessionData()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("http://57.182.94.235:8080/Adhd/api/list/Lesson/patient/5");

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                    if (apiResponse?.data != null && apiResponse.data.Count > 0)
                    {
                        return apiResponse.data.Last();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception while fetching lesson data: " + ex.Message);
            }
        }
        return null;
    }
    //void Update()
    //{
    //    if (gameStarted)
    //    {
    //        timeRemaining -= Time.deltaTime;
    //        if (timeRemaining <= 0)
    //        {
    //            problemIndex++;
    //            problemAnswered = true;
    //            EndStage();
    //        }
    //        if (Time.time-TGameBeginTime >= 600)
    //        {
    //            Debug.Log("TGame已進行10分鐘，Time:"+ (Time.time - TGameBeginTime));
    //            stageAccuracy = (int)((float)stageCorrect / (stageCorrect + stageWrong) * 100);
    //            TimelineLogger.Log("stage " + nowStage + " Accuracy:" + stageAccuracy + " ,correct:" + stageCorrect + " ,wrong:" + stageWrong);
    //            EndGame();
    //        }
    //    }
    //    //Debug.Log($"gameSettings: {gameSettings}, TGame_3dSettings: {gameSettings?.TGame_3dSettings}"); //add by YMJ @20241126
    //}
    public override void StartTutorial()
    {
        Debug.Log("[StartTutorial]");
        Reset();
        //GameUI.instance.SetCounter(0);
        //StartCurrentStage();//開始教學關卡
        StartTutorialStage(tutorialSubStage);
    }

    //public override void ReadyToStart()
    //{
    //    //base.ReadyToStart();
    //    nowStage++;
    //    GamePanel gamePanel = gamePanels.Find(p => p.state == GamePanelState.ReadyToStart);
    //    GameUI.instance.StartPanel(gamePanel.title, gamePanel.info, true);
    //    GameUI.instance.InfoButton.onClick.AddListener(StartCurrentStage);
    //    Reset();
    //    base.PlayVoice(gamePanel.voice);
    //    UpdateUI();
    //}
    public override void StartStage1() //Frame
    {
        nowStage++;
        Reset();
        UpdateUI();
        StartCurrentStage();
        //if(gameController != null)moveSpeedInit();
        //else moveSpeed = 50f;
        //categoryConveyorBelt.SetActive(false);
        //timeRemaining = TimeToAnswer;
        //StartStage(1, 0);
    }
    public override void StartStage2() // category
    {
        moveSpeed = 80f;
        categoryConveyorBelt.SetActive(true);
        frameConveyorBelt.SetActive(false);
        timeRemaining = TimeToAnswer;
        StartStage(2, 0);
    }
    public void StartStage3() //All
    {
        frameConveyorBelt.SetActive(true);
        // 第三階段frame位置更新
        Vector3 newPosition = frameConveyorBelt.gameObject.transform.position;
        newPosition.z += 35f;
        frameConveyorBelt.gameObject.transform.position = newPosition;
        timeRemaining = TimeToAnswer * 4;
        StartStage(3, 0);
    }


    private void StartTutorialStage(int index)
    {
        //switch (index)
        //{
        //    case 0:
        //        currentConfig = TGameLevelManager.Instance.CreateLevelFromConfig(
        //            new int[] { 0, 3, 0, 0, 0, 1, 1, 2 }, -5
        //        );
        //        Debug.Log("教學階段 1：外框模式");
        //        StartCurrentStage();
        //        break;

        //    case 1:
        //        currentConfig = TGameLevelManager.Instance.CreateLevelFromConfig(
        //            new int[] { 0, 0, 3, 0, 0, 2, 1, 2 }, -4
        //        );
        //        Debug.Log("教學階段 2：分類模式");
        //        StartCurrentStage();
        //        break;

        //    case 2:
        //        currentConfig = TGameLevelManager.Instance.CreateLevelFromConfig(
        //            new int[] { 0, 0, 0, 3, 2, 2, 1, 2 }, -3
        //        );
        //        Debug.Log("教學階段 3：單一比對");
        //        StartCurrentStage();
        //        break;

        //    case 3:
        //        currentConfig = TGameLevelManager.Instance.CreateLevelFromConfig(
        //            new int[] { 0, 3, 3, 0, 0, 3, 1, 2 }, -2
        //        );
        //        Debug.Log("教學階段 4：外框加分類");
        //        StartCurrentStage();
        //        break;

        //    case 4:
        //        currentConfig = TGameLevelManager.Instance.CreateLevelFromConfig(
        //            new int[] { 0, 3, 0, 3, 6, 3, 1, 2 }, -1
        //        );
        //        Debug.Log("教學階段 5：外框加物件比對");
        //        StartCurrentStage();
        //        break;


        //    case 5:
        //        Debug.Log("教學結束，顯示提示面板");
        //        FinishTutorial();

        //        GameUI.instance.InfoButton.onClick.RemoveAllListeners();
        //        GameUI.instance.InfoButton.onClick.AddListener(() =>
        //        {
        //            beginTime = DateTime.Now;
        //            Debug.Log("進入第一關");
        //            nowStage = 1;
        //            StartCurrentStage();

        //        });
        //        break;
        //}
    }




    //protected override int GetTotalStagesCount()
    //{
    //return TGameLevelManager.Instance.GetLevelCount();
    //}

    public void BuildTestPlans()
    {
        BuildPlans(count: 10); // 例：先做10個路口
                               // 找到場上的 TJunctionLoop 並注入計畫
        var loop = FindObjectOfType<TGameJunctionLoop>();
        if (loop)
        {
            loop.tgame = this;
            loop.plans = this.Plans; // 或 loop.Init(this.Plans);
        }
        Debug.Log("BuildPlans Success");
    }

    private void StartCurrentStage()
    {
        ////Debug.Log($"nowStage: {nowStage}-GetLevelCount: {TGameLevelManager.Instance.GetLevelCount()}");
        //// ClearStageHint();
        //if (nowStage == 0)
        //{
        //    // 教學狀態下只執行這關
        //    Debug.Log("目前為教學關卡 tutorialSubStage=" + tutorialSubStage);
        //}
        //else
        //{
        //    int levelIndex = nowStage - 1;

        //    if (levelIndex >= TGameLevelManager.Instance.GetLevelCount())
        //    {
        //        Debug.Log("所有關卡完成，結束遊戲");
        //        EndGame();
        //        return;
        //    }
        //    currentConfig = TGameLevelManager.Instance.GetLevel(levelIndex);
        //    Debug.Log($"levelIndex:={levelIndex},nowStage: {nowStage}, frameCount: {currentConfig.frameCount}, categoryCount: {currentConfig.categoryCount}, modelsCount: {currentConfig.modelsCount}");

        //    int levelId = ExtractStageNumber(currentConfig.stageName);
        //    if (levelId > 0 && !playedLevelIds.Contains(levelId))
        //        playedLevelIds.Add(levelId);
        //}


        //// 先選擇當關的類別與形狀（每關都獨立）
        //allowedCategories = categories
        //    .Take(currentConfig.categoryCount)
        //    .ToList();

        //allowedFrames = frameCategories
        //    .Take(currentConfig.frameCount)
        //    .ToList();

        //InitializeAllowedModelList();

        //lastModelIndex = -1;
        //lastFrameIndex = -1;

        //// 改為只包含 allowedCategories 的模型
        //modelsCategories.Clear();
        //foreach (CategoryName c in allowedCategories)
        //{
        //    ModelsCategory models = new ModelsCategory(c);
        //    modelsCategories.Add(models);
        //}

        //UpdateConveyorVisibility(currentConfig.spawnMode);
        //moveSpeed = currentConfig.GetMoveSpeed();
        //spawnMinTime = 7.5f;
        //spawnMaxTime = 8.5f;
        //timeRemaining = TimeToAnswer;

        //Debug.Log($"[StartCurrentStage] level {currentConfig.level},stageName {currentConfig.stageName},frameCount {currentConfig.frameCount},categoryCount{currentConfig.categoryCount},modelsCount{currentConfig.modelsCount},modelIndex{currentConfig.modelIndex},numberOfProblems{currentConfig.numberOfProblems}");
        //Debug.Log($"開始關卡 {nowStage} - {currentConfig.stageName}，問題數量:{currentConfig.numberOfProblems}");
        //StartStage(nowStage, currentConfig.numberOfProblems + 1);


        //Debug.Log("本關的形狀：" + string.Join(", ", allowedFrames));
        //Debug.Log("本關的類別：" + string.Join(", ", allowedCategories));
        //Debug.Log("本關的模型名稱：" + string.Join(", ", allowedModelList));
        //correct.SetActive(false);
        //wrong.SetActive(false);

        //// string hintString="";
        //// bool isCategories = currentConfig.modelIndex == 0;

        //// int modelIndex = currentConfig.modelIndex;
        //// string categoryName = ModelCategoryNameMapper.ToText(modelIndex);


        //// switch (currentConfig.spawnMode)
        //// {
        ////     case SpawnMode.FrameOnly: hintString = "選擇<b>相同的形狀</b>吧!"; break;
        ////     case SpawnMode.ModelOnly: hintString = isCategories ? "分類到<b>相同的類別</b>吧!" : $"選擇<b>一模一樣的{categoryName}</b>吧!"; break;
        ////     case SpawnMode.Both: hintString = isCategories ? "選擇<b>相同的形狀外框</b>\n並分類到<b>相同的類別</b>吧!" : $"選擇<b>相同的形狀外框</b>\n並且選擇<b>一模一樣的{categoryName}</b>吧!"; break;
        //// }

        //// ShowStageHint(hintString);

    }
    private void InitializeAllowedModelList()
    {
        if (currentConfig.modelIndex == 0) return; // 非單一類別模式不用載入

        var modelCategory = (ModelCategoryName)currentConfig.modelIndex;
        var modelList = ResourcesModelLoader.LoadModels(modelCategory);

        allowedModelList = modelList.Take(currentConfig.modelsCount).ToList();
    }

    protected override IEnumerator Problem()
    {
        //Debug.Log($"Problem1 - this={this}, gameObject.activeSelf={gameObject.activeSelf}, enabled={enabled}");
        yield return null;
        //Debug.Log("Problem2 - coroutine繼續執行");
        gameStarted = true;
        RecordAnswerStartTime();
        //Debug.Log("Problem3 - 準備 SpawnCubes");
        StartCoroutine(SpawnCubes());
    }


    public SpawnMode GetSpawnMode()
    {
        return currentConfig.spawnMode;
    }

    //防止關卡設定數量超出數量，導致設定位置時出問題
    //add by alan
    public int GetActiveFrameCount()
    {
        if (frameConveyorBelt == null) return 0;

        int count = 0;
        for (int i = 0; i < frameConveyorBelt.transform.childCount; i++)
        {
            if (frameConveyorBelt.transform.GetChild(i).gameObject.activeSelf)
                count++;
        }
        return count;
    }
    public int GetActiveCategoryCount()
    {
        if (categoryConveyorBelt == null) return 0;

        int count = 0;
        for (int i = 0; i < categoryConveyorBelt.transform.childCount; i++)
        {
            if (categoryConveyorBelt.transform.GetChild(i).gameObject.activeSelf)
                count++;
        }
        return count;
    }

    private int GetNonRepeatingRandomIndex(int lastIndex, int count)
    {
        int randomIndex = 0;
        int attempt = 0;

        do
        {
            randomIndex = UnityEngine.Random.Range(0, count);
            attempt++;
        } while (count > 1 && randomIndex == lastIndex && attempt < 10);

        if (count > 1 && randomIndex == lastIndex)
        {
            randomIndex = (lastIndex + 1) % count;
        }

        return randomIndex;
    }

    IEnumerator SpawnCubes()
    {
        //Debug.Log($"gameStarted:{gameStarted} ");
        while (gameStarted)
        {
            if (!problemAnswered)
            {
                yield return null;
                continue;
            }

            Vector3 spawnPosition = new Vector3(0, 3, spawnArea.position.z + 200);
            framePrefab.GetComponent<BoxCollider>().enabled = true;

            if (StageFinish())
            {
                Debug.Log($"問題{problemIndex} isStageFinish");
                yield break;
            }

            Debug.Log($"問題{problemIndex}開始生成");

            problemAnswered = false;
            if (currentConfig.spawnMode == SpawnMode.FrameOnly)
            {
                Debug.Log("生成Frame");
                GameObject frameCube = Instantiate(framePrefab, spawnPosition, Quaternion.identity);
                AllCubes.Add(frameCube);
                Frame frameScript = frameCube.GetComponent<Frame>();
                if (frameScript != null)
                {
                    frameScript.SetRandomFrame(allowedFrames, ref lastFrameIndex);
                }
                GetModelCategory(frameCube);
                StartCoroutine(DestroyMoveCube(frameCube));
            }
            else
            {

                // 一般類別出題模式：modelIndex == 0
                List<GameObject> selectedModels = new List<GameObject>();
                if (currentConfig.modelIndex == 0)
                {
                    foreach (var category in modelsCategories)
                    {
                        if (!allowedCategories.Contains(category.Category)) continue;
                        selectedModels.AddRange(category.Models);
                    }
                }
                // 單一類別出題模式：modelIndex ≠ 0
                else
                {
                    selectedModels = slotModelList;
                    //var modelCategoryEnum = (ModelCategoryName)(currentConfig.modelIndex);
                    //Debug.Log($"[SpawnCubes] 嘗試載入模型類別: {modelCategoryEnum}");
                    //var modelList = ResourcesModelLoader.LoadModels((ModelCategoryName)modelCategoryEnum);
                    //Debug.Log($"[SpawnCubes] 載入模型數量: {modelList?.Count}");

                    //if (modelList != null && modelList.Count > 0)
                    //{
                    //    selectedModels = modelList.OrderBy(x => UnityEngine.Random.value)
                    //      .Take(currentConfig.modelsCount)
                    //      .ToList();

                    //}
                }

                if (selectedModels.Count == 0)
                {
                    Debug.LogWarning("[SpawnCubes] 找不到可出題模型");
                    yield return new WaitForSeconds(UnityEngine.Random.Range(spawnMinTime, spawnMaxTime));
                    continue;
                }
                Debug.Log("生成Model");
                int randomIndex = GetNonRepeatingRandomIndex(lastModelIndex, selectedModels.Count);
                lastModelIndex = randomIndex;

                //int randomIndex = UnityEngine.Random.Range(0, selectedModels.Count);
                GameObject cube = Instantiate(selectedModels[randomIndex], spawnPosition, selectedModels[randomIndex].transform.rotation, parentObject.transform);

                BoxCollider collider = cube.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.isTrigger = true; //啟用觸發器模式
                }


                AllCubes.Add(cube);

                if (currentConfig.spawnMode == SpawnMode.Both)
                {
                    Debug.Log("生成Frame到Model子物件");
                    framePrefab.GetComponent<BoxCollider>().enabled = false;
                    GameObject frame = Instantiate(framePrefab, cube.transform.position, cube.transform.rotation, parentObject.transform);
                    Frame frameScript = frame.GetComponent<Frame>();
                    if (frameScript != null)
                    {
                        frameScript.SetRandomFrame(allowedFrames, ref lastFrameIndex);
                        frameScript.SetParentAndPosition(cube.transform);
                    }
                }

                GetModelCategory(cube);
                StartCoroutine(DestroyMoveCube(cube));
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(spawnMinTime, spawnMaxTime));
        }
    }




    private void UpdateConveyorVisibility(SpawnMode mode)
    {

        bool useSingleCategory = currentConfig.modelIndex != 0;
        Debug.Log("[UpdateConveyorVisibility]開始關卡:" + currentConfig.stageName + "  modelIndex:" + currentConfig.modelIndex + "  是否為單一類別模式:" + useSingleCategory);
        // 控制總輸送帶開關
        if (frameConveyorBelt != null)
            frameConveyorBelt.SetActive(mode == SpawnMode.FrameOnly || mode == SpawnMode.Both);

        if (categoryConveyorBelt != null)
            categoryConveyorBelt.SetActive(mode == SpawnMode.ModelOnly || mode == SpawnMode.Both);

        // 若為單一類別模式，刷新模型內容
        if (useSingleCategory)
        {
            Debug.Log("[UpdateConveyorVisibility]單一類別模式，刷新模型內容");
            var categoryScript = categoryConveyorBelt.GetComponent<CategoryConveyorBelt>();
            if (categoryScript != null)
            {
                categoryScript.RefreshSlotModels();
            }
        }

        // 控制 Frame 子物件啟用數量
        if (frameConveyorBelt != null && (mode == SpawnMode.FrameOnly || mode == SpawnMode.Both))
        {
            for (int i = 0; i < frameConveyorBelt.transform.childCount; i++)
            {
                frameConveyorBelt.transform.GetChild(i).gameObject.SetActive(i < currentConfig.frameCount);
            }
        }


        // 控制 Category 子物件與 slot 內容（Sprite vs Model）
        if (categoryConveyorBelt != null && (mode == SpawnMode.ModelOnly || mode == SpawnMode.Both))
        {
            int activeCount = useSingleCategory ? currentConfig.modelsCount : currentConfig.categoryCount;

            for (int i = 0; i < categoryConveyorBelt.transform.childCount; i++)
            {
                var slot = categoryConveyorBelt.transform.GetChild(i).gameObject;
                bool shouldBeActive = i < activeCount;
                slot.SetActive(shouldBeActive);

                if (!shouldBeActive) continue;

                foreach (Transform child in slot.transform)
                {
                    // 決定顯示哪種物件（模型或圖片）
                    bool isModel = child.GetComponent<ModelTag>() != null;
                    bool isSprite = child.GetComponent<SpriteRenderer>() != null;

                    child.gameObject.SetActive(
                        (useSingleCategory && isModel) ||
                        (!useSingleCategory && isSprite)
                    );
                }

                // 更新 Sprite 名稱（僅在分類模式）
                if (!useSingleCategory)
                {
                    var sprite = slot.GetComponentInChildren<SpriteRenderer>();
                    if (sprite != null && i < categories.Count)
                    {
                        sprite.name = categories[i].ToString();
                    }
                }
            }


        }

        // 重算位置
        frameConveyorBelt?.GetComponent<FrameConveyorBelt>()?.RecalculatePositions();
        categoryConveyorBelt?.GetComponent<CategoryConveyorBelt>()?.RecalculatePositions();
    }
    protected override void EndProblem()
    {
        if (nowStage == 0) return; // Tutorial不須紀錄

        Accuracy = (int)((float)CorrectCount / (CorrectCount + WrongCount) * 100);
        //Process = (int)((float)nowStage / NumOfProblems * 100);
        Process = (int)((float)(nowStage) / GetTotalStagesCount() * 100);
        Debug.Log("Process" + Process + "CorrectCount:" + CorrectCount + ",WrongCount:" + WrongCount + ",答題數:" + nowStage + ",總題數:" + NumOfProblems);
        //GameUI.instance.SetLineWithProcess(Process);
        
        // 注意注意 註解註解
        // RecordAnswerEndTime();
        // 注意注意 註解註解

        /* 進行更新與紀錄 */
        //UpdateLineWithAccuracy();
        //UpdateLineWithProcess();
    }


    IEnumerator DestroyMoveCube(GameObject cube)
    {
        float currentSpeed = moveSpeed; // 初始移動速度
        while (cube != null && cube.transform.position.z > player.transform.position.z)
        {
            if (currentSpeed > 20f)
            {
                currentSpeed -= Time.deltaTime * 5f; // 每秒減少速度 (調整這個數值可以控制速度變慢的速率)
                currentSpeed = Mathf.Max(currentSpeed, 30f); // 不低於 20f
            }
            cube.transform.Translate(Vector3.back * currentSpeed * Time.deltaTime);
            yield return null;
        }

        if (cube != null)
        {
            Debug.Log(cube.name + "超出邊界");
            Wrong();
            Destroy(cube);
        }
    }
    //protected override bool StageFinish()
    //{
    //    Debug.Log("[StageFinish]problemIndex"+problemIndex+"problemNum - 1"+(problemNum - 1));
    //    if (problemIndex < problemNum - 1)
    //    {
    //        return false;
    //    }
    //    else
    //    {
    //        Invoke("EndStage", 0.5f);
    //        return true;
    //    }
    //}



    protected override void EndStage()
    {
        //Debug.Log("[EndStage]");
        ////Debug.Log("EndStage() 觸發");
        //base.EndStage();
        //// 清空場上模型
        //foreach (GameObject cube in AllCubes)
        //{
        //    if (cube != null)
        //    {
        //        Destroy(cube);
        //    }
        //}
        //AllCubes.Clear(); // 清空列表
        ////stageAccuracy = (int)((float)stageCorrect / (stageCorrect + stageWrong) * 100);
        ////TimelineLogger.Log("stage " + nowStage + " Accuracy:" + stageAccuracy+" ,correct:" + stageCorrect + " ,wrong:" + stageWrong);


        //if (nowStage == 0)
        //{
        //    tutorialSubStage++;
        //    StartTutorialStage(tutorialSubStage);
        //    if (tutorialSubStage == 5)
        //        Debug.Log("
        //
        //        EndStage
        //
        //
        //        教學階段結束，準備開始第一關");
        //}
        //else
        //{
        //    //endTime = DateTime.Now;
        //    //SendGameDataToServer();
        //    //beginTime = DateTime.Now;

        //    // 下一關
        //    nowStage++;
        //    if (nowStage <= TGameLevelManager.Instance.GetLevelCount())
        //    {
        //        //Reset(); // 重置關卡參數
        //        Debug.Log("關卡" + (nowStage - 1) + "結束，重置關卡參數");
        //        StartCurrentStage();
        //    }
        //    else
        //    {
        //        Debug.Log("所有關卡完成");
        //        EndGame();
        //        ReadyToEndGame();
        //    }
        //}
    }
    public override void Correct()
    {
        //base.Correct();
        CorrectCount++;
        Debug.Log("Problem" + problemIndex + " correct");
        if (SoundManager.Instance) SoundManager.Instance.PlaySE("Correct");
        score++;
        UpdateScore();
        wrong.SetActive(false);
        correct.SetActive(true);
        moveSpeed += 3f;        // 答對一次加速，以此作為難度遞增機制（無上限）
        spawnMinTime -= 0.1f;   // 縮短生成間隔，加快出題節奏
        problemAnswered = true;
        EndProblem();
        UpdateUI();
    }
    public override void Wrong()
    {
        //base.Wrong();
        WrongCount++;
        Debug.Log("Problem" + problemIndex + " wrong");
        if (SoundManager.Instance) SoundManager.Instance.PlaySE("Wrong");
        UpdateScore();
        correct.SetActive(false);
        wrong.SetActive(true);
        // 每答錯兩次才降速，避免連錯時速度驟降（設計上的緩衝機制）
        if (WrongCount % 2 == 0)
        {
            moveSpeed -= 3f;
            spawnMinTime += 0.3f;
        }
        problemAnswered = true;
        EndProblem();
        UpdateUI();
    }
    public void Correct(AnswerSource source)
    {
        //base.Correct();
        CorrectCount++;
        Debug.Log("Problem" + problemIndex + " correct");
        if (SoundManager.Instance) SoundManager.Instance.PlaySE("Correct");
        score++;
        //stageCorrect++;
        UpdateScore();
        wrong.SetActive(false);
        correct.SetActive(true);
        moveSpeed += 3f;
        spawnMinTime -= 0.1f;
        HandleAnswerProgress(source);
        UpdateUI();
    }
    public void Wrong(AnswerSource source)
    {
        //base.Wrong();
        WrongCount++;

        switch (source)
        {
            case AnswerSource.Frame: frameWrongCount++; break;
            case AnswerSource.Category:
                if (currentConfig.modelIndex != 0)
                    modelWrongCount++;
                else
                    categoryWrongCount++;
                break;
        }

        Debug.Log("Problem" + problemIndex + " wrong");
        if (SoundManager.Instance) SoundManager.Instance.PlaySE("Wrong");
        //stageWrong++;
        UpdateScore();
        correct.SetActive(false);
        wrong.SetActive(true);
        if (WrongCount % 2 == 0)
        {
            moveSpeed -= 3f;
            spawnMinTime += 0.3f;
        }
        HandleAnswerProgress(source);
        UpdateUI();
    }

    /// <summary>
    /// 處理作答進度邏輯，根據 SpawnMode 決定何時才算「完成一題」。
    ///
    /// FrameOnly / ModelOnly 模式：任一次作答即完成一題，直接設 problemAnswered = true。
    /// Both 模式（雙重作答）：
    ///   - Frame 先答：設 problemAnswered = false，阻止 SpawnCubes() 生成下一題，等待 Category 作答
    ///   - Category 後答：設 problemAnswered = true，允許生成下一題，計入作答進度
    /// </summary>
    private void HandleAnswerProgress(AnswerSource source)
    {
        var mode = currentConfig.spawnMode;
        if (mode == SpawnMode.FrameOnly || mode == SpawnMode.ModelOnly)
        {
            EndProblem();
            problemAnswered = true;
        }
        else if (mode == SpawnMode.Both)
        {
            if (source == AnswerSource.Frame)
            {
                // Both 模式：Frame 答完後暫停生成，等待玩家繼續作答 Category
                problemAnswered = false;
            }
            else
            {
                // Category 作答完成，本題結束，允許生成下一題
                EndProblem();
                problemAnswered = true;
            }
        }
    }



    public override void EndGame()
    {
        base.EndGame();
        gameStarted = false;
        // 清空場上模型
        foreach (GameObject cube in AllCubes)
        {
            if (cube != null)
            {
                Destroy(cube);
            }
        }
        AllCubes.Clear(); // 清空列表
        try
        {
            gameController = GameObject.Find("GameController").GetComponent<GameController>();
            //StartStage1();
        }
        catch (System.NullReferenceException)
        {

        }
    }


    public override void Reset()
    {
        base.Reset();
        UpdateUI();
        problemAnswered = true;
        tutorialSubStage = 0;
        frameWrongCount = 0;
        categoryWrongCount = 0;
        modelWrongCount = 0;
    }
    void UpdateScore()
    {
        //problemIndex++;
    }
    // 根據建模物件判斷在哪個陣列
    public CategoryName GetModelCategory(GameObject model)
    {
        string modelName = model.name.Replace("(Clone)", "").Trim();
        foreach (ModelsCategory m in modelsCategories)
        {
            if (m.ModelNames.Contains(modelName)) return m.Category;
        }
        return CategoryName.None;
    }


    public static int ExtractStageNumber(string stageName)
    {
        if (stageName.StartsWith("Stage "))
        {
            string numberPart = stageName.Substring(6); // 從第 6 字元開始
            if (int.TryParse(numberPart, out int result))
                return result;
        }
        return -1;
    }




    /// <summary>
    /// 建立 count 個路口題目的規劃列表（Plans 與 AnswerSequence）。
    /// 障礙物以 obstacleChance 機率隨機生成，時間參數在設計範圍內隨機取值。
    /// </summary>
    public void BuildPlans(int count)
    {
        AnswerSequence.Clear();
        Plans.Clear();
        for (int i = 0; i < count; i++)
        {
            int dir = UnityEngine.Random.Range(0, 2); // 0 = 左、1 = 右
            AnswerSequence.Add(dir);

            var plan = new JunctionPlan
            {
                correctDir  = dir,
                goodOnLeft  = (dir == 0), // 預設好提示放在正解方向（可被 TGameLevelManager mirror 邏輯覆蓋）
                displayMode = (ClueMode)UnityEngine.Random.Range(0, 3),
                goodIndex   = 0,
                badIndex    = 0
            };

            // 70% 機率生成障礙物（暫定開發值，正式設計請依關卡難度調整）
            float obstacleChance = 0.7f;

            if (UnityEngine.Random.value < obstacleChance)
            {
                // Range(1, 3) 上限不含 3，故實際取值為 1（GateUp）或 2（GateDown）
                // 若要包含 Spike(3)/Dog(4) 請改為 Range(1, 5)
                plan.obstacleType = (ObstacleType)UnityEngine.Random.Range(1, 3);

                plan.hiddenSeconds = UnityEngine.Random.Range(3f, 4f); // 隱藏倒數階段（玩家看不到計時）
                plan.showSeconds   = UnityEngine.Random.Range(2f, 3f); // 顯示倒數階段（玩家可預測開門）
                plan.openSeconds   = UnityEngine.Random.Range(1f, 3f); // 開放通行階段
                plan.phaseOffset   = UnityEngine.Random.value;

                Debug.Log($"[BuildPlans] {i} 有障礙: type={plan.obstacleType}, hidden={plan.hiddenSeconds}, show={plan.showSeconds}, open={plan.openSeconds}");
            }
            else
            {
                plan.obstacleType = ObstacleType.None;
                Debug.Log($"[BuildPlans] {i} 沒有障礙");
            }

            Plans.Add(plan);
        }
    }


    private void UpdateUI()
    {
        //UpdateLineWithAccuracy();
        UpdateLineWithProcess();
        Debug.Log("Process " + Process);
        UpdateLineWithCorrect();
    }
    //add by ymj
    [Serializable]
    public class ApiResponse
    {
        public int retCode;
        public string retMsg;
        public List<LessonData> data;
    }

    [Serializable]
    public class LessonData
    {
        public int id;
        public int patient;
        public int order;
        public int category;
        public double hours;
        public string name;
        public string note;
        public string trainTime;
        public bool status;
        public int creator;
        public string createTime;
        public int modifier;
        public string modifyTime;
        public bool deleteMark;
    }


    /// <summary>
    /// 每題的提示顯示模式：
    ///   Both     — 同時顯示好提示和壞提示（玩家需判斷哪個是「好」的）
    ///   OnlyGood — 只顯示正確方向的好提示（較簡單，提示明確）
    ///   OnlyBad  — 只顯示錯誤方向的壞提示（玩家需反向判斷，較難）
    /// </summary>
    public enum ClueMode { Both, OnlyGood, OnlyBad }

    [Serializable]
    public class TGameLevelConfig
    {
        public int numberOfJunctions = 6;     // 幾個路口 = 幾題
        public ClueMode clueMode = ClueMode.Both;
        public float mirrorProbability = 0.5f; // 將好/壞左右對調的機率（決定「解在左或右」的空間）
    }

    /// <summary>
    /// 障礙物種類列舉。
    /// 對應 TJunction.obstacleRoot 下的子物件索引：index = (int)obstacleType - 1
    ///   None    = 0（此路口無障礙，不佔子物件位置）
    ///   GateUp  = 1（→ index 0）
    ///   GateDown = 2（→ index 1）
    ///   Spike   = 3（→ index 2）
    ///   Dog     = 4（→ index 3）
    /// </summary>
    public enum ObstacleType { None = 0, GateUp = 1, GateDown = 2, Spike = 3, Dog = 4 }

    /// <summary>
    /// 單題路口的完整規劃資料，由 TGame.BuildPlans() 建立，注入 TGameJunctionLoop.plans。
    /// </summary>
    [Serializable]
    public class JunctionPlan
    {
        // 正解方向：0 = 左、1 = 右（對應 AnswerSequence 中的每一項）
        public int correctDir;
        // 好提示是否在左側（false 則在右側，壞提示自然在另一側）
        public bool goodOnLeft;
        // 好/壞提示圖示的難度索引（Tier），對應 TJunction 掛載點下的子物件索引
        public int goodIndex;
        public int badIndex;
        // 此題實際顯示的提示模式（Both/OnlyGood/OnlyBad）
        public ClueMode displayMode;

        // ── 障礙物規劃 ──
        public ObstacleType obstacleType = ObstacleType.None;
        // 對應 TimedObstacle 的三個時間階段（詳見 ObstacleConfig 與 IObstacle 說明）
        public float hiddenSeconds; // 隱藏倒數計時的秒數
        public float showSeconds;   // 顯示倒數計時的秒數（對應 TimedObstacle.visibleSeconds）
        public float openSeconds;   // 開放通行的秒數
        public float phaseOffset;   // 相位偏移（讓不同路口不完全同步）
    }
    public void ResetPositions(TGameJunctionLoop loop)
    {
        Debug.Log("[TGame] ResetPositions()");

        // 1. 玩家回到 active startPoint
        var targetJ = loop.CurrentActive;
        if (targetJ == null) targetJ = loop.A;

        if (player != null && targetJ.startPoint != null)
        {
            player.transform.SetPositionAndRotation(
                targetJ.startPoint.position,
                targetJ.startPoint.rotation);

            var pInput = player.GetComponent<TPlayerInput>();
            if (pInput) pInput.ResetToIdle();

            Debug.Log("[TGame] 玩家已重置到 startPoint");
        }

        // 2. 重置兩個 Chunk 的位置
        if (loop.A && loop.A.startPoint)
        {
            loop.A.transform.SetPositionAndRotation(
                loop.A.startPoint.position,
                loop.A.startPoint.rotation);
        }
        if (loop.B && loop.B.startPoint)
        {
            loop.B.transform.SetPositionAndRotation(
                loop.B.startPoint.position,
                loop.B.startPoint.rotation);
        }
    }

}
