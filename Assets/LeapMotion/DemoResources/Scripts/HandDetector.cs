using UnityEngine;
using System.Collections;
using Leap;
using Network;

namespace Detector
{
  public class HandDetector : MonoBehaviour {

    public HandController leap_controller_;

    public static int abnormal = 0;

    HandModel GetHand(Collider other)
    {
      HandModel hand_model = null;
      // Navigate a maximum of 3 levels to find the HandModel component.
      int level = 1;
      Transform parent = other.transform.parent;
      while (parent != null && level < 3) {
        hand_model = parent.GetComponent<HandModel>();
        if (hand_model != null) {
          break;
        }
        parent = parent.parent;
      }

      return hand_model;
    }

    // Finds the first instance (by depth-firstrecursion)
    // of a child with the specified name
    Transform FindPart(Transform parent, string name) {
      if (parent == null) {
        return parent;
      }
      if (parent.name == name) {
        return parent;
      }
      for (int c = 0; c < parent.childCount; c++) {
        Transform part = FindPart(parent.GetChild(c), name);
        if (part != null) {
          return part;
        }
      }
      return null;
    }

    void OnTriggerStay(Collider other)
    {
      HandModel hand_model = GetHand(other);
      if (hand_model != null)
      {
        // Debug.Log ("Detected: " + other.transform.parent.name + "/" + other.gameObject.name);
        string name = GetComponent<Collider>().name;
        // Debug.Log(name + " Enter collider");
        // Debug.Log("Warning: hand out of range!");
        if (name == "detect_bottom")
          abnormal = 1;
        else if(name == "detect_top")
          abnormal = 1; //2;
        else if(name == "detect_left")
          abnormal = 1; //3;
        else if(name == "detect_right")
          abnormal = 1; //4;
        else if(name == "detect_forward")
          abnormal = 1; //5;
        else if(name == "detect_backward")
          abnormal = 1; //6;
        
      }
    }

    void OnTriggerExit(Collider other)
    {
        // Debug.Log(GetComponent<Collider>().name + " Out collider");
        abnormal = 0;
    }
  }
}