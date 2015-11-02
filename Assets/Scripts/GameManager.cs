﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum GameState {Pause, MainMenu, Intro, Playing, Cutscene, Notification, PitStop, InventoryMode, GameOver, Win};
public enum AgeState {YoungAdult, Adult, OldAdult, SeniorCitizen};

public class GameManager : MonoBehaviour {

	public GameState currentGameState = GameState.Playing;
	public static GameManager s_instance;

	void Awake(){
		if (s_instance==null) {
			s_instance = this;
		}
		else if (s_instance!=this) {
			Destroy(gameObject);
		}
	}

	// References
	PlayerController playerController;

	//switches
	bool switchToNotification;
	bool switchToInventory;
	bool switchToPitstop;
	bool switchToGame;
	bool userPressedStart = false;
	bool tutorialIsOver = false;
	bool switchToCutScene;
	//lerps
	float cameraRotateStartTime;
	float cameraRotateDuration = .5f;
	bool isCamRotateUp, isCamRotateDown;

	public GameObject tutorialCam;
	public GameObject pitStopGUI;
	public GameObject inGameGUI;
	public GameObject MainMenuGUI, MainMenuText;
	public GameObject faderObj;
	int textIterator = 0;
	float slideDuration = 3f;
	float slideTimer;

	public GameObject currentGUIseries;
	public GameState previousGameState;

	//TODO add attrition rate increases depending on if player gets wife or gf or not
	
	void Start () {
		Screen.orientation = ScreenOrientation.Portrait;
		Screen.autorotateToLandscapeLeft = false;
		Screen.autorotateToLandscapeRight = false;
		Screen.autorotateToPortraitUpsideDown = false;

		playerController = GameObject.FindObjectOfType<PlayerController>();
		if( playerController == null )
			Debug.LogError( "GameManager didn't find a reference to a PlayerController in the scene." );
	}



	//Swipe up to switchToInventory
	//Swipe down to switchToRoad

	// Update is called once per frame
	void Update () {
		print (currentGameState);
		switch (currentGameState) {

		case GameState.MainMenu : 
			if (userPressedStart) {
				userPressedStart = false;
				currentGameState = GameState.Intro;
			}
			break;

		case GameState.Intro :

			if(switchToGame) {
				switchToGame = false;
				currentGameState = GameState.Playing;
			}

			if (Input.GetKeyDown(KeyCode.Space)) {
				SwitchToGame ();

				EndTutorial();
			}
			else {
				RunCutSceneText();
			}
			break;

		case GameState.Playing :

			if (switchToCutScene) {
				switchToCutScene = false;
				currentGameState = GameState.Cutscene;
			}
			if (switchToPitstop) {
				switchToPitstop = false;
				currentGameState = GameState.PitStop;
			}

			break;

		case GameState.Cutscene : 
			RunCutSceneText();
			if (switchToGame) {
				switchToGame = false;
				currentGameState = GameState.Playing;
			}
			break;
		case GameState.PitStop : 
			if (switchToGame) {
				switchToGame = false;
				currentGameState = GameState.Playing;
			}
			if (switchToNotification) {
				switchToNotification = false;
				currentGameState = GameState.Notification;
			}
			break;
		case GameState.Notification : 
			if (switchToGame) {
				switchToGame = false;
				currentGameState = GameState.Playing;
			}
			if (switchToPitstop) {
				switchToPitstop = false;
				currentGameState = GameState.PitStop;
			}
			break;
		
	}
	
}


	public void StartGame () {
		userPressedStart = true;
		MainMenuGUI.SetActive (false);
		MainMenuText.SetActive (false);
		currentGUIseries.SetActive (true);
		Camera.main.transform.SetParent(GameObject.FindGameObjectWithTag("Player").transform);
		tutorialCam.GetComponent<Cinematographer> ().quaternions [0] = Camera.main.transform;
		tutorialCam.GetComponent<Cinematographer> ().RollCamera ();
	}

	public void EndTutorial () {
		currentGUIseries.SetActive (false);
		GameObject.FindGameObjectWithTag ("CamPos").GetComponent<Cinematographer> ().RollCamera ();
		GameObject.FindGameObjectWithTag ("Player").GetComponent<Animator> ().SetTrigger ("run");

	}

	void RunCutSceneText () {
		slideTimer += Time.deltaTime;
		if (slideTimer > slideDuration) {
			if (textIterator == currentGUIseries.transform.childCount-1) {
				slideTimer = 0;
				if (currentGameState == GameState.Intro) {
					EndTutorial();
				}
				else {
					currentGUIseries.SetActive (false);
				}
			}
			else if (textIterator < currentGUIseries.transform.childCount - 1) {
				currentGUIseries.transform.GetChild(textIterator).gameObject.SetActive(false);
				textIterator++;
				currentGUIseries.transform.GetChild(textIterator).gameObject.SetActive(true);
				slideTimer = 0;
			}
		}
	}

	public void SwitchToCutscene () {
		slideTimer = 0;
		inGameGUI.GetComponent<Animator> ().SetTrigger("hide");
		//inGameGUI.SetActive (false);
		textIterator = 0;
		switchToGame = false;
		switchToCutScene = true;
		Camera.main.GetComponent<Cinematographer> ().RollCamera();
		Camera.main.GetComponent<HoverFollowCam>().enabled = false;

	}

	public void SwitchToPitStop () {
		if (currentGameState == GameState.Playing) {
			switchToPitstop = true;
			inGameGUI.GetComponent<Animator> ().SetTrigger ("pitstop");
			pitStopGUI.GetComponent<Animator> ().SetTrigger ("pitstop");
		}

		if (currentGameState == GameState.Notification) {
			switchToPitstop = true;

		}
			
			
	}
	public void SwitchToGame () {
		if (currentGameState == GameState.Intro) {
			Camera.main.GetComponent<HoverFollowCam> ().enabled = true;
			currentGUIseries.SetActive (false);
//				inGameGUI.SetActive (true);
			inGameGUI.GetComponent<Animator> ().SetTrigger ("show");
			print ("SET TRIGGER SHOW");
			switchToGame = true;


		} else if (currentGameState == GameState.PitStop) {
			// UI
			inGameGUI.GetComponent<Animator> ().SetTrigger("pitstop");
			pitStopGUI.GetComponent<Animator>().SetTrigger("pitstop");
			// Player
			playerController.SetAtRespawnPos();
			switchToGame = true;
		}
	}

	public void SwitchToNotification() {
		switchToNotification = true;
		if( previousGameState != GameState.Notification )
			previousGameState = currentGameState;
	}

	public void PitstopFlashEnter() {
		StartCoroutine("FlashIn");
	}

	public void PitstopFlashExit() {
		StartCoroutine("FlashOut");
	}

	private IEnumerator FlashIn() {
		faderObj.GetComponent<Fader>().StartFadeIn();
		yield return new WaitForSeconds(1f);
		//switch camera
		faderObj.GetComponent<Fader>().StartFadeOut();
	}
	private IEnumerator FlashOut() {
		faderObj.GetComponent<Fader>().StartFadeIn();
		yield return new WaitForSeconds(1f);
		//switch camera
		faderObj.GetComponent<Fader>().StartFadeOut();
	}
}
