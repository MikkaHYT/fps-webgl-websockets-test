using UnityEngine;

public class MenuScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnJoinButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Joining");
    }

    public void OnOptionsButtonClicked()
    {
        // Handle options button click here (e.g., show options menu)
        Debug.Log("Options button clicked!");
    }

    public void OnQuitButtonClicked()
    {
        // Handle quit button click here (e.g., exit the game)
        Debug.Log("Quit button clicked!");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the editor
        #endif
    }
}
