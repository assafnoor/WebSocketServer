using System.Net.WebSockets;
using System.Text;

namespace WebSocketServer
{
    public class MiControladorDeWebSockets
    {
        // Constructor for the WebSocket controller
        private readonly RequestDelegate _next;

        public MiControladorDeWebSockets(RequestDelegate next)
        {
            _next = next;
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

            // Get a CancellationToken for this WebSocket request
            var ct = context.RequestAborted;

            // Accept the WebSocket connection
            using (var socket = await context.WebSockets.AcceptWebSocketAsync())
            {
                // Receive a message from the WebSocket
                var message = await ReceiveStringAsync(socket, ct);
                if (message == null) return;

                // Process different messages
                switch (message.ToLower())
                {
                    case "hola":
                        // Send a response message to the WebSocket
                        await SendStringAsync(socket, "Hola como estás, bienvenido", ct);
                        break;

                    case "adios":
                        // Close the WebSocket connection
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconectado", ct);
                        break;

                    default:
                        // Send a default response for unrecognized messages
                        await SendStringAsync(socket, "Lo siento, pero no entiendo ese mensaje", ct);
                        break;
                }

                // Process messages with parameters separated by '#'
                if (message.Contains('#'))
                {
                    string[] messageCompuesto = message.ToLower().Split('#');
                    switch (messageCompuesto[0])
                    {
                        case "hola":
                            // Send a response with user-specific greeting
                            await SendStringAsync(socket, "Hola usuario " + messageCompuesto[1], ct);
                            break;

                        default:
                            // Send a default response for unrecognized parameterized messages
                            await SendStringAsync(socket, "Lo siento, pero no entiendo ese mensaje", ct);
                            break;
                    }
                }

                return;
            }
        }

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
