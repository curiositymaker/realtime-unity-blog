using UnityEngine;
using WebSocketSharp;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;


public class WebSocketManager : MonoBehaviour
{
    private WebSocket ws;
    private const string SERVER_URL = "ws://localhost:3000";


    private Queue<Action> mainThreadActions = new Queue<Action>();



    [SerializeField]
    TextMeshProUGUI messageText;

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            mainThreadActions.Dequeue()?.Invoke();
        }
    }

    void ConnectToServer()
    {
        Debug.Log("Starting the connection");
        ws = new WebSocket($"{SERVER_URL}/socket.io/?EIO=4&transport=websocket");

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Connection opened");
        };

        ws.OnMessage += OnWebSocketMessage;

        ws.OnError += (sender, e) =>
        {
            Debug.LogError("Error: " + e.Message);
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Connection closed");
        };

        ws.Connect();
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }

    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        try
        {
            string data = e.Data;
            Debug.Log("Received Message: " + data);

            if (data.StartsWith("0"))
            {
                Debug.Log($"<color=yellow>Received handshake message: {data}</color>");
                ws.Send("40");
                ws.Send("42[\"connection\",\"\"]");
            }
            else if (data.StartsWith("42"))
            {
                // Remove the leading "42" and the enclosing square brackets
                string json = data.Substring(2).Trim('[', ']');

                // Split the JSON string into event name and message
                int commaIndex = json.IndexOf(',');
                if (commaIndex != -1)
                {
                    string eventName = json.Substring(0, commaIndex).Trim('"');
                    string message = json.Substring(commaIndex + 1).Trim().TrimStart('"').TrimEnd('"');

                    Debug.Log($"Received event: {eventName}, message: {message}");

                    // Update UI on the main thread
                    mainThreadActions.Enqueue(() =>
                    {
                        if (messageText != null)
                        {
                            messageText.text = message;
                            Debug.Log($"Updated UI text: {message}");
                        }
                        else
                        {
                            Debug.LogError("messageText is null");
                        }
                    });
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in OnWebSocketMessage: " + ex.Message);
        }
    }
    void SendMessageToServer(string message, string eventName)
    {
        try
        {
            if (ws != null && ws.IsAlive)
            {
                string payload = $"42[\"{eventName}\",\"{message}\"]";
                ws.Send(payload);
                Debug.Log($"Sent message: {payload}");
            }
            else
            {
                Debug.LogError("WebSocket connection is not established or alive.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in SendMessageToServer: " + ex.Message);
            Debug.LogError("Stack trace: " + ex.StackTrace);
        }
    }

    public void ButtonClick(string messageToSend)
    {
        Debug.Log("button clicked");
        SendMessageToServer(messageToSend, "messageFromUnity");
    }

}