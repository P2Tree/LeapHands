using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Protocal {
    public enum ProType { String, Bytes};

    public class makeProtocal {

        string dataString;
        byte [] dataBytes;

        public makeProtocal(Int16[] inputVal, int len, Protocal.ProType type = ProType.Bytes) {
            if (type == ProType.String) {
                dataString = inputVal[0].ToString();
                for (int i = 1; i < len; i++) {
                    dataString += ",";
                    dataString += inputVal[i].ToString();
                }
            }
            else if (type == ProType.Bytes) {
                dataBytes = new byte[len * 2];
                byte[] tmp = null;
                for (int i = 0; i< len; i++) {
                    tmp = (BitConverter.GetBytes(inputVal[i]));
                    Array.Copy(tmp, 0, dataBytes, 2*i, tmp.Length);
                }
            }
        }

        public string getStringCmd() {
            return dataString;
        }

        public byte[] getBytesCmd() {
            return dataBytes;
        }
    }
}