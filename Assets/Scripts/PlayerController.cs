﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	// Movement and bounds
	private float strafeSpeed = 4.5f, moveSpeed = 8f;
	private float playerBounds = 4f;
	private float pitStopBoundsOffset = 4.5f;
	private bool pitstopEntranceAvailable = false;
	private bool isOnHorizontalRoad = false;
	private Animator myAnimator;
	public Transform currentRoadSection;
	public Vector2 moveDirVector;				// x component being world x; y component being world z (topdown view)
	public Transform respawnPos;

	// Guy Cart
	private Vector3 lerpToStartPos;
	private Vector3 lerpToEndPos;
	private float lerpTimer;
	private float lerpDuration;
	private bool isLerpingToGuyCart = false;
	private bool isOnCart = false;
	private Animator currentCartAnimator;
	private LerpToGuyCart.CartDirection cartDirection;


	Vector3 projV = Vector3.zero;
	// Rotation
//	private bool isRotateLerp;
//	private float rotateLerpTimer;
//	private float rotateLerpDuration = 0.8f;
//	private Quaternion startLerp, endLerp;

	//TODO add attrition rate increases depending on if player gets wife or gf or not
	void Start(){
		myAnimator = GetComponent<Animator> ();
		moveDirVector = new Vector2( 0f, 1f );
	}

//	void StartRotateLerp() {
//		startLerp = transform.rotation;
//		rotateLerpTimer = Time.time;
//		isRotateLerp = true;
//	}

	void Update () {
		if( isLerpingToGuyCart ) {
			float t = lerpTimer / lerpDuration;
			transform.position = Vector3.Lerp( lerpToStartPos, lerpToEndPos, t );
			if (t > 0.99f) {
				transform.position = lerpToEndPos;
				isLerpingToGuyCart = false;
				lerpTimer = 0f;

				// Activate cart
				if( cartDirection == global::LerpToGuyCart.CartDirection.LEFT )
					currentCartAnimator.SetTrigger( "left" );
				else
					currentCartAnimator.SetTrigger( "right" );

				isOnCart = true;
				return;
			} else {
				lerpTimer += Time.deltaTime;
			}
		}
		if( isOnCart ) {
			AnimatorStateInfo info = currentCartAnimator.GetCurrentAnimatorStateInfo(0);
			// Exit cart if we are more than 99% into animation
			if( info.normalizedTime >= 0.99f && !info.IsName( "New State" ) ) {
				isOnCart = false;
				CheckGroundOrientation();
				return;
			}  else {
				transform.forward = currentCartAnimator.transform.right;
				transform.position = new Vector3( currentCartAnimator.transform.position.x, transform.position.y, currentCartAnimator.transform.position.z );
			}
		}

//		if (isRotateLerp) {
//			float perc = (Time.time - rotateLerpTimer) / rotateLerpDuration;
//			transform.rotation = Quaternion.Lerp(startLerp, endLerp, perc);
//			if (perc > .99) {
//				perc = 1;
//				Quaternion.Lerp(startLerp, endLerp,perc);
//				isRotateLerp = false;
//			}
//		}
		if (GameManager.s_instance.currentGameState == GameState.Cutscene) {
			if( !isOnCart )
				transform.Translate (Vector3.forward*moveSpeed*Time.deltaTime);
		}
		if (GameManager.s_instance.currentGameState == GameState.Playing) {
			float horizontal = Input.GetAxis ("Horizontal");
	
			if (Input.touchCount > 0) {
				Touch touch = Input.GetTouch (0);
				if (touch.position.x > Screen.width / 2) {
					//go right
					horizontal = 1f;
				} else if (touch.position.x < Screen.width / 2) {
					horizontal = -1f;
				}
			}

			myAnimator.SetFloat ("Turn", horizontal * .6f);
			transform.Translate (Vector3.forward*moveSpeed*Time.deltaTime);

			// Calculate if we are entering a pitstop
			float tempPitstopBoundsOffset = 0f;
			if( pitstopEntranceAvailable ) {
				tempPitstopBoundsOffset = pitStopBoundsOffset;
			}

			// Find distance from center of road
			Vector2 currentRoadPos = new Vector2( currentRoadSection.position.x, currentRoadSection.position.z );
			Vector2 playerPos = new Vector2( transform.position.x, transform.position.z );

			Vector2 roadToPlayerVector = playerPos - currentRoadPos;
			float moveDirVectorLength = roadToPlayerVector.magnitude;
			Vector2 projVector = ( Vector2.Dot( moveDirVector*moveDirVectorLength ,roadToPlayerVector )/( moveDirVectorLength*moveDirVectorLength ) ) * (moveDirVector*moveDirVectorLength) ;
			projVector += currentRoadPos;
			projV = new Vector3( projVector.x, 0f, projVector.y );
			float distanceFromCenterOfRoad = ( playerPos - new Vector2( currentRoadPos.x + projVector.x, currentRoadPos.y + projVector.y )).magnitude;
			Debug.Log( "Distance: " + distanceFromCenterOfRoad );

			// Bound Player
			if ( Mathf.Abs(distanceFromCenterOfRoad) < playerBounds + tempPitstopBoundsOffset ) {
				transform.Translate (horizontal * strafeSpeed*Time.deltaTime, 0, 0);
			} else {
				Debug.Log( "Out of bounds." );
			}
		} else {
			myAnimator.SetFloat ("Turn", 0);
		}
	}
	
	void OnTriggerEnter(Collider other) {
		if (other.gameObject.tag == "powerUp") {
			PowerUp temp = other.gameObject.GetComponent<PowerUp> ();
			if (temp.cost != 0f) {
				PlayerStats.s_instance.money += temp.cost;
			}
			if (temp.burnRate != 0f) {
				PlayerStats.s_instance.cashFlow += temp.burnRate;
			}
			if (temp.happiness != 0f) {
				PlayerStats.s_instance.happiness += temp.happiness / 100f;
			}
			Destroy (other.gameObject);
		} else if (other.tag == "branch") {
			GameManager.s_instance.currentGUIseries = other.GetComponent<RoadBranch> ().GUIObject;
			GameManager.s_instance.currentGUIseries.SetActive (true);
			GameManager.s_instance.SwitchToCutscene ();
			print ("HIT BRANCH");
		} else if (other.tag == "pitstop") {
			GameManager.s_instance.SwitchToPitStop ();
			pitstopEntranceAvailable = false;
			respawnPos = other.GetComponent<PitStopRespawn> ().respawnPosition;
		} else if (other.tag == "pitstopRoad") {
			pitstopEntranceAvailable = true;
		} else if (other.tag == "tutorial") {
			GUIManager.s_instance.DisplayTutorial(other.name);
		}
	}

	void OnTriggerExit(Collider other) {
		if (other.tag == "branch") {
			//rotating player 90 degrees depending on what it says
//			endLerp = transform.rotation * Quaternion.Euler(0,other.GetComponent<RoadBranch>().degreeToTurnBy,0);
//			StartRotateLerp();
//			currentRoadSection = other.GetComponent<RoadBranch>().nextRoadBranch.transform;
			isOnHorizontalRoad = !isOnHorizontalRoad;
			if(other.GetComponent<RoadBranch>().GUIObject.name == "Retire"){
				myAnimator.SetTrigger ("Retired");
				transform.Translate(0,0,0);
			}
		} else if( other.tag == "pitstopRoad" ) {
			pitstopEntranceAvailable = false;
		}
	}

	public void SetAtRespawnPos() {
		if( respawnPos == null ) {
			Debug.LogError( "No respawn position set for this trigger." );
			return;
		}

		transform.position = respawnPos.position;
	}

	public void LerpToGuyCart( Transform newLerpPos, Animator cart, LerpToGuyCart.CartDirection newDir ) {
		lerpToStartPos = transform.position;
		lerpToEndPos = newLerpPos.position;
		lerpDuration = Vector3.Distance( lerpToStartPos, lerpToEndPos ) / moveSpeed;
		GameManager.s_instance.SwitchToCutscene();
		isLerpingToGuyCart = true;
		currentCartAnimator = cart;
		cartDirection = newDir;
	}

	public void CheckGroundOrientation() {
		Ray ray = new Ray( transform.position, Vector3.down * 1.5f );
		RaycastHit hitInfo;
		if( Physics.Raycast( ray, out hitInfo ) ) {
			float roadRotation = hitInfo.transform.rotation.eulerAngles.y;
			moveDirVector = new Vector2( Mathf.Cos( roadRotation ), Mathf.Sin( roadRotation ) );
			currentRoadSection = hitInfo.transform;
			Debug.LogWarning( "Current road piece: " + hitInfo.transform.name );
		} else {
			// This won't work for the first ground piece. Set the ground piece directly under player
			Debug.LogError( "PlayerContoller's CheckGroundOrientation didn't detect any gound under player." );
		}
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere( currentRoadSection.position, 1f );
		Gizmos.color = Color.red;
		Gizmos.DrawSphere( transform.position, 2f );
		Gizmos.color = Color.green;
		Gizmos.DrawSphere( projV, 1f );
		Gizmos.color = Color.cyan ;
		Gizmos.DrawSphere( currentRoadSection.position + new Vector3(moveDirVector.x, 0f, moveDirVector.y ), 0.5f );
	}
}
