// #define ADJUST
// #define SHOWLOG
// #define COMMUNICATE
// #define EXTERCONTROL

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
using System.Threading;

public class main : HandController {

	#region Object members

	HandDetector detector_;

	// if print log information
	Boolean logFlag = false;
	// readyToRun signal
	private static bool readyToRun = true;

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
    // Use to remember the previous hand normal vector and check hand turn too fast
	Vector preHandNormal = new Vector(0, 0, 0);
	double tooFastThreshold = 1.5;

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
	private udpClientSocket udp = null;
	private tcpClientSocket tcp = null;
	private tcpServerSocket tcpServer = null;
	private makeProtocal constructor;
	private string sendStringCmd;
	private byte[] sendBytesCmd;

	// abnormal states flag
	private int abnormalOutofrange = 0; // 0 is normal mode
	private int abnormalOutofspeed = 0; // 0 is normal mode
	private static int abnormalSendedOutofrange = 0;	// if abnormal is sended, set to true
	private static int abnormalSendedOutofspeed = 0;
	static int sendedNormal = 0;
	private enum AbnormalType {OutOfRange, OutOfSpeed, Normal};

	Thread runControlThread;

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
		Debug.Log("Normal node.");
#endif

		leap_controller_ = new Controller();

		detector_ = new HandDetector();

#if (COMMUNICATE)
		///Initialize udp socket for communication
		// udp = new udpClientSocket("192.168.1.134", 1235);
		udp = new udpClientSocket("127.0.0.1", 8001);

		///Initialize tcp socket for communication
		// tcp = new tcpClientSocket("192.168.1.134", 5557);
		tcp = new tcpClientSocket("127.0.0.1", 8088);
		
		// Initialize tcp server socket for start signal
		// tcpServer = new tcpServerSocket("127.0.0.1", 8089);
		tcpServer = new tcpServerSocket("192.168.1.125", 8889);
#endif

#if (EXTERCONTROL)
		runControlThread = new Thread(RunControl);
		runControlThread.Start();
#endif
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

		// if (enableRecordPlayback && recordingAsset != null)
		// recorder_.Load(recordingAsset);
	}
	
	/** Updates the graphics objects. */
	void Update() {
// #if (EXTERCONTROL)
// 		if (readyToRun == true)
// 		{
// #endif
// 			if (leap_controller_ == null)
// 			return;
			
// 			// UpdateRecorder();
// 			Frame frame = GetFrame();

// 			if (frame != null && !flag_initialized_)
// 			{
// 				InitializeFlags();
// 			}
// 			if (frame.Id != prev_graphics_id_)
// 			{
// 				prev_graphics_id_ = frame.Id;
// 			}
// #if (EXTERCONTROL)
// 		}
// 		RunControl();
// #endif
	}

	/** Updates the physics objects */
	void FixedUpdate() {
#if (EXTERCONTROL)
		if (readyToRun == true)
		{
#endif
			if (leap_controller_ == null)
				return;

			Frame frame = GetFrame();

			if (frame != null && !flag_initialized_)
			{
				InitializeFlags();
			}

			if (frame.Id != prev_physics_id_)
            {
                // UpdateToolModels(tools_, frame.Tools, toolModel);
                prev_physics_id_ = frame.Id;

                int readyDataFlag = ProcessHands(frame);

                // bool retAb = Abnormal(readyDataFlag);
				bool retAb = true;
				Abnormal(readyDataFlag);
				// TODO: 分清超速异常和越界异常，越界异常需要不更新图像模型，但超速异常还是需要更新。
                DestroyAllHands();
				if (readyDataFlag == 0)
				{
    	            UpdateHandModels(hand_physics_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
					if (retAb == true)
						UpdateHandModels(hand_graphics_, frame.Hands, leftGraphicsModel, rightGraphicsModel);
				}


				#if (COMMUNICATE)
                if (readyDataFlag == 0)
                {
                    SendInformation(ProType.String);
                }
				#endif

            }
#if (EXTERCONTROL)
        }
		RunControl();
#endif
	}


    void OnApplicationQuit()
    {
#if (COMMUNICATE)
		udp.SocketQuit();
		tcp.SocketQuit();
#endif
    }

	private void RunControl()
	{
		while(!readyToRun)
		{
			readyToRun = false;
			readyToRun = tcpServer.getStartSignal();
		}
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
				if (handNormal.AngleTo(preHandNormal) > tooFastThreshold)
				{
					return -3;
				}
				preHandNormal = handNormal;

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

	private void checkVelocity(Hand hand, float thresholdX = 1000, float thresholdY = 1000, float thresholdZ = 1000) {
		float speedOfX = hand.PalmVelocity.x;
		float speedOfY = hand.PalmVelocity.y;
		float speedOfZ = hand.PalmVelocity.z;

		if (Mathf.Abs(speedOfX) > thresholdX) 
		{
			// Debug.Log("Warning: hand x moving so fast! Speed: " + speedOfX);
			abnormalOutofspeed = 1;
		}else if(Mathf.Abs(speedOfY) > thresholdY)
		{
			// Debug.Log("Warning: hand y moving so fast! Speed: " + speedOfY);
			abnormalOutofspeed = 1; //2;
		}else if(Mathf.Abs(speedOfZ) > thresholdZ)
		{
			// Debug.Log("Warning: hand z moving so fast! Speed: " + speedOfZ);
			abnormalOutofspeed = 1; //3;
		}else // normal state
		{
			abnormalOutofspeed = 0;
		}

	}

	static int i = 0;
    private bool Abnormal(int ready)
    {
		i = i+1;
        /// abnormal exception Check
        abnormalOutofrange = HandDetector.abnormal;
        // if (abnormalOutofrange == 1 && abnormalSendedOutofrange == 0)
		if (abnormalOutofrange == 1)
		{
			///send abnormal exception to tcp port
			SendAbnormalOnce(AbnormalType.OutOfRange, true);
			abnormalSendedOutofrange = 1;
			Debug.Log("abnormal debug: " + i + ", range ab");	
			sendedNormal = 0;
		}

        // if (abnormalOutofspeed == 1 && abnormalSendedOutofspeed == 0)
		if (abnormalOutofspeed == 1)
		{
			///send abnormal exception to tcp port
			SendAbnormalOnce(AbnormalType.OutOfSpeed, true);
			abnormalSendedOutofspeed = 1;
			Debug.Log("abnormal debug: " + i + ", speed ab");
			sendedNormal = 0;
        }
		
        // if (abnormalOutofrange == 0 && abnormalOutofspeed == 0 && sendedNormal == 0)
		if (abnormalOutofrange == 0 && abnormalOutofspeed == 0 && ready == 0)
		{
			SendAbnormalOnce(AbnormalType.Normal, true);
			// Debug.Log("abnormal debug: " + i + ", normal");	
			sendedNormal = 1;
			abnormalSendedOutofrange = 0;
			abnormalSendedOutofspeed = 0;
			return true;
		}
		return false;
    }
    private void SendAbnormalOnce(AbnormalType type, bool trigger)
    {
	#if (COMMUNICATE)
		byte[] data;
        if (type == AbnormalType.OutOfSpeed)
        {
			data = new byte[] { 0x3c, 0x00 };
			tcp.SocketSend(data);
        }
        else if (type == AbnormalType.OutOfRange)
        {
			data = new byte[] { 0x3c, 0x01 };
			tcp.SocketSend(data);
        }
		else if (type == AbnormalType.Normal)
		{
			data = new byte[] { 0x3c, 0x02 };
			tcp.SocketSend(data);
		}
	#endif
    }

    #endregion

}
