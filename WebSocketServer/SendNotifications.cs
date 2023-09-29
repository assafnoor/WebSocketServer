using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebSocketServer
{
    public class SendNotifications
    {
        private readonly RequestDelegate _next;
        private readonly Timer _notificationTimer;
        private readonly CancellationTokenSource _cts;
        private WebSocket _currentWebSocket;

        // Constructor for the WebSocket controller
        public SendNotifications(RequestDelegate next)
        {
            _next = next;
            _cts = new CancellationTokenSource();

            // Initialize and start the notification timer
            _notificationTimer = new Timer(SendNotification, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        // This method is called for each WebSocket request
        public async Task Invoke(HttpContext context)
        {
            // Check if the request is a WebSocket request; if not, pass it to the next middleware
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
                return;
            }

            // Accept the WebSocket connection
            _currentWebSocket = await context.WebSockets.AcceptWebSocketAsync();
            try
            {
                // Process WebSocket messages until cancellation is requested
                while (!_cts.Token.IsCancellationRequested)
                {
                    // Receive a message from the WebSocket
                    var message = await ReceiveStringAsync(_currentWebSocket, _cts.Token);
                    if (message == null) break;

                    // Handle incoming messages (e.g., chat messages)
                    HandleMessage(message);
                }
            }
            finally
            {
                // Dispose of the WebSocket when finished
                _currentWebSocket?.Dispose();
            }
        }

        // Handle incoming messages (e.g., chat messages)
        private void HandleMessage(string message)
        {
            // Handle incoming messages (e.g., chat messages)

            // For example, broadcast received messages to all clients
            //BroadcastMessage(message);
        }

        // Send a notification message to the current WebSocket client
        private async void SendNotification(object state)
        {
            try
            {
                // Check if the WebSocket is open
                if (_currentWebSocket != null && _currentWebSocket.State == WebSocketState.Open)
                {
                    // Create a notification message
                    var notificationMessage = "This is a notification message sent every 2 minutes.";

                    // Send the notification message to the WebSocket
                    await SendStringAsync(_currentWebSocket, notificationMessage, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during notification sending
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }

        }

        // Rest of the code (ReceiveStringAsync, SendStringAsync, etc.) remains the same
    


        // Helper method to receive a string message from the WebSocket
        private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    ct.ThrowIfCancellationRequested();

                    // Receive data from the WebSocket into the buffer
                    result = await socket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                // Reset the position of the MemoryStream for reading
                ms.Seek(0, SeekOrigin.Begin);

                // Check if the received message is of type text
                if (result.MessageType != WebSocketMessageType.Text)
                    throw new Exception("Mensaje inesperado");

                // Decode the binary data in the MemoryStream as UTF-8 text
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    // Read the decoded text and return it as a string
                    return await reader.ReadToEndAsync();
                }
            }
        }
        // Helper method to send a string message to the WebSocket
        private static Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default)
        {
            // Convert the string to bytes using UTF-8 encoding
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);

            // Send the string as a WebSocket text message
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }

    }
}
