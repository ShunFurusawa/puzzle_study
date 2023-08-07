using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleDirector : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (Input.anyKey)
        {
            Invoke("ChangeScene", 1.0f);// íxâÑé¿çs
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void ChangeScene()
    {
        SceneManager.LoadScene("PlayScene");
    }
}
