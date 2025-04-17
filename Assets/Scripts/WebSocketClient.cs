using NativeWebSocket;
using System;
using System.Collections;
using UnityEngine;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket webSocket;

    private async void Start()
    {
        webSocket = new WebSocket("ws://ws.814850.xyz");

        await webSocket.Connect();
        Debug.Log("WebSocket connected!");

        StartCoroutine(ReceiveMessages());
    }

    private IEnumerator ReceiveMessages()
{
    while (true)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            // Use a callback-based approach instead of await
            webSocket.OnMessage += (bytes) =>
            {
                string message = System.Text.Encoding.UTF8.GetString(bytes);
                HandleMessage(message);
            };
        }
        yield return null;
    }
}

    private void HandleMessage(string message)
    {
        Debug.Log("Received message: " + message);
        // Handle incoming messages here
    }

    public async void SendMessage(string message)
{
    if (webSocket.State == WebSocketState.Open)
    {
        await webSocket.SendText(message); // Use SendText for sending strings
        Debug.Log("Sent message: " + message);
    }
}

    private async void OnApplicationQuit()
    {
        await webSocket.Close();
        Debug.Log("WebSocket closed!");
    }
}