using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGGPO;

public class TestUnityGGPO : MonoBehaviour {

    private void Start() {
        Debug.Log(GGPO.BuildNumber);

        Debug.Log(GGPO.Version);

        Debug.Log(GGPO.UggTimeGetTime());
        GGPO.UggSleep(10);
    }
}