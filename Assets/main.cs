// #define ADJUST
// #define SHOWLOG

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Util;
using Network;
using Protocal;
using Detector;

public class main : HandController {

	#region Object members

	Boolean logFlag = false;
	// hand arguments: 3 position elements and 3 pasture elements
	double handX;
	double handY;
	double handZ;
	double handRoll;
	double handPitch;
	double handYaw;

	// The direction is expressed as a unit vector pointing in the same direction
	// as the directed line from the middle finger tip to the palm central 
	Vector handDirection;

	// The direction is expressed as a unit vector pointing in the same direction
	// as the palm normal (orthogonal to the palm and out of back of palm)
	Vector handNormal;	

	// confidence value of hand frame
	double handConfidence;

	// seleceted hand (will be used in control process)
	Hand fixHand;
	int fixHandId = 0;
	
	// fingers arguments: including 5 fingers direction vector
	Vector thumbProximalBoneDirection;
	Vector indexInterBoneDirection;
	Vector middleInterBoneDirection;
	Vector ringInterBoneDirection;
	Vector pinkyInterBoneDirection;

	// fingers turn angle from palm vector to finger
	double thumbBoneTurnAngle;		// fingers after amendment
	double indexBoneTurnAngle;
	double middleBoneTurnAngle;
	double ringBoneTurnAngle;
	double pinkyBoneTurnAngle;
	double thumbBoneTurnAngleRaw;	// fingers without amendment
	double indexBoneTurnAngleRaw;
	double middleBoneTurnAngleRaw;
	double ringBoneTurnAngleRaw;
	double pinkyBoneTurnAngleRaw;

#if (ADJUST)

	// amendment arguments solving
	static double addThumbAngle = 0;
	static double addIndexAngle = 0;
	static double addMiddleAngle = 0;
	static double addRingAngle = 0;
	static double addPinkyAngle = 0;
	static int countNum = 0;

#endif

	const double BiasOfThumbAngle = 43.916;
	const double BiasOfIndexAngle = 166.896;
	const double BiasOfMiddleAngle = 168.612;
	const double BiasOfRingAngle = 164.178;
	const double BiasOfPinkyAngle = 151.647;

	// communication arguments
	private udpSocket udp;
	private makeProtocal constructor;
	private string sendStringCmd;
	private byte[] sendBytesCmd;


	#endregion

	#region member functions
	/** Creates a new Leap Controller object. */
	// Will be automate called when the script running ( after MonoBehavior created)
	// it is used to create some system variances and objects
	void Awake() {

#if (ADJUST)
		Debug.Log("Open ADJUST task.");
#endif

#if (SHOWLOG && !ADJUST)
		logFlag = true;
		Debug.Log("LOG mode.");
#else
		logFlag = false;
#endif

		leap_controller_ = new Controller();

		// Initialize udp socket for communication
		udp = new udpSocket("192.168.1.134", 1234);
		// udp = new udpSocket("127.0.0.1", 8002);
	}

	/** Initalizes the hand and tool lists and recording, if enabled.*/
	// Will be called when the first update frame arrival ( after all of Awake running done, 
	// and before first Update running
	// it is used to assignment variance
	void Start() {

		// Initialize hand lookup tables.
		hand_graphics_ = new Dictionary<int, HandModel>();
		hand_physics_ = new Dictionary<int, HandModel>();

		tools_ = new Dictionary<int, ToolModel>();

		if (leap_controller_ == null) {
		Debug.LogWarning(
			"Cannot connect to controller. Make sure you have Leap Motion v2.0+ installed");
		}

		if (enableRecordPlayback && recordingAsset != null)
		recorder_.Load(recordingAsset);
	}
	
	/** Updates the graphics objects. */
	void Update() {
		if (leap_controller_ == null)
		return;
		
		UpdateRecorder();
		Frame frame = GetFrame();

		if (frame != null && !flag_initialized_)
		{
		InitializeFlags();
		}
		if (frame.Id != prev_graphics_id_)
		{
		UpdateHandModels(hand_graphics_, frame.Hands, leftGraphicsModel, rightGraphicsModel);
		prev_graphics_id_ = frame.Id;
		}
	}

	/** Updates the physics objects */
	void FixedUpdate() {
		if (leap_controller_ == null)
		return;

		Frame frame = GetFrame();

		
		if (frame.Id != prev_physics_id_)
		{
			UpdateHandModels(hand_physics_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
			UpdateToolModels(tools_, frame.Tools, toolModel);
			prev_physics_id_ = frame.Id;

			int ret = ProcessHands(frame);

			if (ret == 0) {
				SendInformation(ProType.String);
			}
		}
	}

    void OnApplicationQuit()
    {
        udp.SocketQuit();
    }
	private int ProcessHands(Frame frame) {
		// Debug.Log("Frame id: " + frame.Id + ", " + 
		// 		"Timestamp: " + frame.Timestamp + ", " +
		// 			"Hands amount: " + frame.Hands.Count);
		
        if (frame.Hands.Count > 0)
        {

            /// find the suitable hand
            fixHand = frame.Hand(fixHandId);

            /// hand with fixHandId is not containing in current frame
            if (!fixHand.IsValid)
            {
                foreach (Hand hand in frame.Hands)
                {
                    if (hand.IsRight)
                    {   // check only for right hand
                        fixHand = hand;
                        fixHandId = fixHand.Id;
                        break;
                    }
                    else
                    {
                        return -1; // can not find right hand
                    }
                }
            }

            /// waiting for confidence allowed
            handConfidence = fixHand.Confidence;
            if (handConfidence < 0.99)
            {
                // Debug.Log(fixHand.Id + " , Hand confidence: " + handConfidence + ", break");
                return -1; // do not confident too much
            }

			// monitor palm moving speed in the space
			checkVelocity(fixHand);

            /// Palm position and posture
			{
				handDirection = fixHand.Direction;
				handNormal = fixHand.PalmNormal;

				handX = fixHand.StabilizedPalmPosition.x;
				handY = fixHand.StabilizedPalmPosition.y;
				handZ = fixHand.StabilizedPalmPosition.z;
				handRoll = handNormal.Roll * 180.0f / Mathf.PI;
				handPitch = handDirection.Pitch * 180.0f / Mathf.PI;
				handYaw = handDirection.Yaw * 180.0f / (float)Mathf.PI;
			}

			if (logFlag) {
				Debug.Log("handX: " + handX.ToString());
				Debug.Log("handY: " + handY.ToString());
				Debug.Log("handZ: " + handZ.ToString());
				Debug.Log("handRoll: " + handRoll.ToString());
				Debug.Log("handPitch: " + handPitch.ToString());
				Debug.Log("handYaw: " + handYaw.ToString());
			}

            getFingers(handDirection, handNormal);

            /// use to find the zero point of each finger, only when needed open
			#if (ADJUST)
            checkAverageAngle(1000);
			#endif

            amendFingersAngle(logFlag);

            /// prepare for communication
            constructeCmd(ProType.String);

            // grab
            // float grabStrength = fixHand.GrabStrength;
            // Debug.Log(fixHand.Id + ", hand grab strength: " + grabStrength);
            // 另外还有一个grabAngle的方法，在3.0及以上版本中包含
            return 0;

        }
        return -2;
    }

    private void constructeCmd(ProType type = ProType.Bytes)
    {
        Int16[] sendVal = new Int16[] {(Int16)handX, (Int16)handY, (Int16)handZ,
                                        (Int16)handRoll, (Int16)handPitch, (Int16)handYaw,
                                        (Int16)thumbBoneTurnAngle, (Int16)indexBoneTurnAngle,
                                        (Int16)middleBoneTurnAngle, (Int16)ringBoneTurnAngle,
                                        (Int16)pinkyBoneTurnAngle};
        constructor = new makeProtocal(sendVal, 11, type);
		if (type == ProType.String) {
			sendStringCmd = constructor.getStringCmd();
		}
		else if(type == ProType.Bytes) {
			sendBytesCmd = constructor.getBytesCmd();
		}
    }

    private void amendFingersAngle(Boolean angleShow = false) {
		thumbBoneTurnAngle =  thumbBoneTurnAngleRaw - BiasOfThumbAngle;
		indexBoneTurnAngle =  BiasOfIndexAngle - indexBoneTurnAngleRaw;
		middleBoneTurnAngle =  BiasOfMiddleAngle - middleBoneTurnAngleRaw;
		ringBoneTurnAngle =  BiasOfRingAngle - ringBoneTurnAngleRaw;
		pinkyBoneTurnAngle =  BiasOfPinkyAngle - pinkyBoneTurnAngleRaw;

		if (angleShow == true) {
			Debug.Log("thumb angle: " + thumbBoneTurnAngle);
			Debug.Log("index angle: " + indexBoneTurnAngle);
			Debug.Log("middle angle: " + middleBoneTurnAngle);
			Debug.Log("ring angle: " + ringBoneTurnAngle);
			Debug.Log("pinky angle: " + pinkyBoneTurnAngle);
		}
	}

	#if (ADJUST)
    private void checkAverageAngle(int iter = 1000)
    {
		Debug.Log("Waiting ...");
        addThumbAngle += thumbBoneTurnAngleRaw;
		addIndexAngle += indexBoneTurnAngleRaw;
		addMiddleAngle += middleBoneTurnAngleRaw;
		addRingAngle += ringBoneTurnAngleRaw;
		addPinkyAngle += pinkyBoneTurnAngleRaw;
        countNum += 1;
        if (countNum >= iter)
        {
			// show average angles
            Debug.Log("average thumb angle is: " + addThumbAngle / (double)countNum);
            Debug.Log("average index angle is: " + addIndexAngle / (double)countNum);
            Debug.Log("average middle angle is: " + addMiddleAngle / (double)countNum);
            Debug.Log("average ring angle is: " + addRingAngle / (double)countNum);
            Debug.Log("average pinky angle is: " + addPinkyAngle / (double)countNum);
            countNum = 0;
            addThumbAngle = 0;
			addIndexAngle = 0;
			addMiddleAngle = 0;
			addRingAngle = 0;
			addPinkyAngle = 0;
        }
    }
	#endif

    private void getFingers(Vector handDirection, Vector handNormal)
    {
        /* fingers position */
        foreach (Finger finger in fixHand.Fingers)
        {
            getBoneDirection(finger);
        }

		Vector handCrossDirection = handNormal.Cross(handDirection);

		/// Angle difference between finger vector and hand normal vector
		/// then radias to angle
		/// TODO: 对于大拇指，选择骨头向量到handCrossDirection的夹角，还是骨头向量投影到handNormal与handCrossDirection所在平面后，再与handCrossDirection的夹角
        thumbBoneTurnAngleRaw = thumbProximalBoneDirection.AngleTo(handCrossDirection) * 180.0f / (float)Mathf.PI;
        indexBoneTurnAngleRaw = indexInterBoneDirection.AngleTo(handDirection) * 180.0f / (float)Mathf.PI;
        middleBoneTurnAngleRaw = middleInterBoneDirection.AngleTo(handDirection) * 180.0f / (float)Mathf.PI;
        ringBoneTurnAngleRaw = ringInterBoneDirection.AngleTo(handDirection) * 180.0f / (float)Mathf.PI;
        pinkyBoneTurnAngleRaw = pinkyInterBoneDirection.AngleTo(handDirection) * 180.0f / (float)Mathf.PI;

		// Debug.Log("thumb raw: " + thumbBoneTurnAngleRaw);
		// Debug.Log("index raw: " + indexBoneTurnAngleRaw);
		// Debug.Log("middle raw: " + middleBoneTurnAngleRaw);
		// Debug.Log("ring raw: " + ringBoneTurnAngleRaw);
		// Debug.Log("pinky raw: " + pinkyBoneTurnAngleRaw);

    }

    private void getBoneDirection(Finger finger)
    {
		if (finger.Type == Finger.FingerType.TYPE_THUMB) {
			// thumb finger do not have a intermediate bone, so here use proximal bone
			thumbProximalBoneDirection = finger.Bone(Bone.BoneType.TYPE_PROXIMAL).Direction;
		}
		else if (finger.Type == Finger.FingerType.TYPE_INDEX) {
			indexInterBoneDirection = finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE).Direction;
		}
		else if(finger.Type == Finger.FingerType.TYPE_MIDDLE) {
			middleInterBoneDirection = finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE).Direction;
		}
		else if(finger.Type == Finger.FingerType.TYPE_RING) {
			ringInterBoneDirection = finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE).Direction;
		}
		else if(finger.Type == Finger.FingerType.TYPE_PINKY) {
			pinkyInterBoneDirection = finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE).Direction;
		}
    }

    private void SendInformation(Protocal.ProType type = ProType.Bytes) {
		if (type == ProType.String) {
			udp.SocketSend(sendStringCmd);
		}
		else if(type == ProType.Bytes) {
			udp.SocketSend(sendBytesCmd);
		}
	}

	private void checkVelocity(Hand hand, float thresholdX = 700, float thresholdY = 700, float thresholdZ = 700) {
		float speedOfX = hand.PalmVelocity.x;
		float speedOfY = hand.PalmVelocity.y;
		float speedOfZ = hand.PalmVelocity.z;

		if (Mathf.Abs(speedOfX) > thresholdX) 
		{
			Debug.Log("Warning: hand x moving so fast! " + speedOfX);
		}else if(Mathf.Abs(speedOfY) > thresholdY)
		{
			Debug.Log("Warning: hand y moving so fast! " + speedOfY);
		}else if(Mathf.Abs(speedOfZ) > thresholdZ)
		{
			Debug.Log("Warning: hand z moving so fast! " + speedOfZ);
		}
	}

	#endregion

}
