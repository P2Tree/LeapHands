using UnityEngine;
using System.Collections;
using Leap;
using Network;

namespace Detector
{
  public class HandDetector : MonoBehaviour {

    public HandController leap_controller_;

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

    void OnTriggerEnter(Collider other)
    {
      HandModel hand_model = GetHand(other);
      if (hand_model != null)
      {
        // Debug.Log ("Detected: " + other.transform.parent.name + "/" + other.gameObject.name);
        Debug.Log(GetComponent<Collider>().name);
        
      }
    }
  }
}