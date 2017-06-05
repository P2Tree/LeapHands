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

      string cname = null;
      void OnTriggerStay(Collider other)
      {
          HandModel hand_model = GetHand(other);

          if (hand_model != null)
          {
              // Debug.Log ("Detected: " + other.transform.parent.name + "/" + other.gameObject.name);
              // cname = GetComponent<Collider>().name;
              // Debug.Log(name + " Enter collider");
              // Debug.Log("Warning: hand out of range!");
              // ChangeColor(cname, Color.red);
              abnormal = 1;

              // if (cname == "detect_bottom")
              //     ChangeColor("wall_bottom", Color.red, 1);
              // else if(cname == "detect_top")
              //     ChangeColor("wall_top", Color.red, 1);
              // else if(cname == "detect_left")
              //     ChangeColor("wall_left", Color.red, 1);
              // else if(cname == "detect_right")
              //     ChangeColor("wall_right", Color.red, 1);
              // else if(cname == "detect_forward")
              //     ChangeColor("wall_forward", Color.red, 1);
              // else if(cname == "detect_backward")
              //     ChangeColor("wall_backward", Color.red, 1);
            }
            // Invoke("BlinkColor", 1);
        }

        void OnTriggerExit(Collider other)
        {
            // cname = GetComponent<Collider>().name;
            // print("collider out: " + cname);
            // Debug.Log(GetComponent<Collider>().name + " Out collider");
            // ChangeColor(name, Color.white);
            abnormal = 0;              
            // if (cname == "detect_bottom")
            //     ChangeColor("wall_bottom", Color.white);
            // else if(cname == "detect_top")
            //     ChangeColor("wall_top", Color.white);
            // else if(cname == "detect_left")
            //     ChangeColor("wall_left", Color.white);
            // else if(cname == "detect_right")
            //     ChangeColor("wall_right", Color.white);
            // else if(cname == "detect_forward")
            //     ChangeColor("wall_forward", Color.white);
            // else if(cname == "detect_backward")
            //     ChangeColor("wall_backward", Color.white);
        }
        Material preMaterial;
        private void ChangeColor(string obj_name, Color color, int flag)
        {
            Material material = new Material(Shader.Find(obj_name));
            GameObject obj = GameObject.Find(obj_name);
            Renderer render = obj.GetComponent<Renderer>();
            preMaterial = render.material;
            if (flag == 1)
            {
                render.material.color = color;
            }else if(flag == 0)
            {
              render.material = preMaterial;
            }
        }

        private void BlinkColor()
        {
            if (cname == "detect_bottom")
                ChangeColor("wall_bottom", Color.white, 0);
            else if(cname == "detect_top")
                ChangeColor("wall_top", Color.white, 0);
            else if(cname == "detect_left")
                ChangeColor("wall_left", Color.white, 0);
            else if(cname == "detect_right")
                ChangeColor("wall_right", Color.white, 0);
            else if(cname == "detect_forward")
                ChangeColor("wall_forward", Color.white, 0);
            else if(cname == "detect_backward")
                ChangeColor("wall_backward", Color.white, 0);
        }
    }
}