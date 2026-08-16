using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace TradeCopia.Protocol
{
    public static class EnginePipeName
    {
        public const string Prefix = "TradeCopia.Engine.v1.";

        public static string FromMaterial(string material)
        {
            if (string.IsNullOrEmpty(material))
            {
                throw new ArgumentException("Pipe name material is required.", nameof(material));
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(material));
                var hex = new StringBuilder(16);
                for (var i = 0; i < 8; i++)
                {
                    hex.Append(hash[i].ToString("x2"));
                }

                return Prefix + hex;
            }
        }

        public static string ForCurrentUser()
        {
            return FromMaterial(Environment.UserName + "|" + Environment.MachineName);
        }
    }

    public sealed class NamedPipeEngineHost : IDisposable
    {
        private readonly string _pipeName;
        private readonly ProtocolSession _session;
        private NamedPipeServerStream? _server;
        private Thread? _thread;
        private volatile bool _run;

        public NamedPipeEngineHost(string pipeName, ProtocolSession session)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("Pipe name is required.", nameof(pipeName));
            }

            _pipeName = pipeName;
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public string PipeName => _pipeName;
        public bool ClientConnected { get; private set; }
        public string LastRejectReason { get; private set; } = string.Empty;

        public void Start()
        {
            if (_thread != null)
            {
                throw new InvalidOperationException("Host already started.");
            }

            _run = true;
            _thread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "TradeCopia.NamedPipe.Engine"
            };
            _thread.Start();
        }

        public void Dispose()
        {
            _run = false;
            try
            {
                if (_server != null)
                {
                    _server.Dispose();
                }
            }
            catch (Exception)
            {
            }

            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join(1000);
            }
        }

        private void AcceptLoop()
        {
            while (_run)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    _server = server;
                    var connected = server.WaitForConnectionAsync();
                    while (_run && !connected.Wait(100))
                    {
                    }

                    if (!_run)
                    {
                        break;
                    }

                    ClientConnected = true;
                    Serve(server);
                }
                catch (Exception)
                {
                    ClientConnected = false;
                }
                finally
                {
                    ClientConnected = false;
                    if (server != null)
                    {
                        server.Dispose();
                    }
                }
            }
        }

        private void Serve(NamedPipeServerStream server)
        {
            var header = new byte[ProtocolLimits.HeaderBytes];
            while (_run && server.IsConnected)
            {
                if (!ReadExact(server, header, ProtocolLimits.HeaderBytes))
                {
                    return;
                }

                var length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
                if (length < 0 || length > ProtocolLimits.MaxMessageBytes)
                {
                    LastRejectReason = "invalid-frame-length";
                    return;
                }

                var payload = new byte[length];
                if (!ReadExact(server, payload, length))
                {
                    return;
                }

                var frame = new byte[ProtocolLimits.HeaderBytes + length];
                Buffer.BlockCopy(header, 0, frame, 0, ProtocolLimits.HeaderBytes);
                Buffer.BlockCopy(payload, 0, frame, ProtocolLimits.HeaderBytes, length);

                ProtocolEnvelope incoming;
                try
                {
                    incoming = ProtocolEnvelopeCodec.DecodeFrame(frame);
                }
                catch (Exception)
                {
                    LastRejectReason = "malformed-envelope";
                    return;
                }

                var result = _session.Handle(incoming);
                if (!result.Accepted)
                {
                    LastRejectReason = result.Reason;
                }

                var reply = ProtocolEnvelopeCodec.EncodeFrame(result.Reply);
                server.Write(reply, 0, reply.Length);
                server.Flush();
            }
        }

        private static bool ReadExact(Stream stream, byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    return false;
                }

                offset += read;
            }

            return true;
        }
    }

    public sealed class NamedPipeCompanionClient : IDisposable
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _client;

        public NamedPipeCompanionClient(string pipeName)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("Pipe name is required.", nameof(pipeName));
            }

            _pipeName = pipeName;
        }

        public bool IsConnected => _client != null && _client.IsConnected;

        public bool TryConnect(int timeoutMs)
        {
            DisposeClient();
            var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.None);
            try
            {
                client.Connect(timeoutMs);
                _client = client;
                return true;
            }
            catch (Exception)
            {
                client.Dispose();
                return false;
            }
        }

        public ProtocolValidationResult Send(ProtocolEnvelope envelope)
        {
            var client = _client;
            if (client == null || !client.IsConnected)
            {
                return new ProtocolValidationResult(false, "engine-disconnected", envelope);
            }

            var frame = ProtocolEnvelopeCodec.EncodeFrame(envelope);
            client.Write(frame, 0, frame.Length);
            client.Flush();

            var header = new byte[ProtocolLimits.HeaderBytes];
            if (!NamedPipeEngineHostRead.ReadExact(client, header, ProtocolLimits.HeaderBytes))
            {
                return new ProtocolValidationResult(false, "truncated-reply", envelope);
            }

            var length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
            if (length < 0 || length > ProtocolLimits.MaxMessageBytes)
            {
                return new ProtocolValidationResult(false, "invalid-reply-length", envelope);
            }

            var payload = new byte[length];
            if (!NamedPipeEngineHostRead.ReadExact(client, payload, length))
            {
                return new ProtocolValidationResult(false, "truncated-reply", envelope);
            }

            var replyFrame = new byte[ProtocolLimits.HeaderBytes + length];
            Buffer.BlockCopy(header, 0, replyFrame, 0, ProtocolLimits.HeaderBytes);
            Buffer.BlockCopy(payload, 0, replyFrame, ProtocolLimits.HeaderBytes, length);
            var reply = ProtocolEnvelopeCodec.DecodeFrame(replyFrame);
            var rejected = string.Equals(reply.MessageType, "Reject", StringComparison.Ordinal);
            return new ProtocolValidationResult(!rejected, rejected ? "rejected" : "ok", reply);
        }

        public void Dispose()
        {
            DisposeClient();
        }

        private void DisposeClient()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null!;
            }
        }
    }

    internal static class NamedPipeEngineHostRead
    {
        public static bool ReadExact(Stream stream, byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    return false;
                }

                offset += read;
            }

            return true;
        }
    }

    public static class ProtocolEnvelopeCodec
    {
        public static byte[] EncodeFrame(ProtocolEnvelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var json = EncodeJson(envelope);
            return ProtocolFraming.Encode(json);
        }

        public static ProtocolEnvelope DecodeFrame(byte[] frame)
        {
            var json = ProtocolFraming.Decode(frame);
            return DecodeJson(json);
        }

        public static string EncodeJson(ProtocolEnvelope envelope)
        {
            return "{\"protocolVersion\":" + envelope.ProtocolVersion
                + ",\"messageId\":" + Quote(envelope.MessageId)
                + ",\"messageType\":" + Quote(envelope.MessageType)
                + ",\"sentAtUtc\":" + Quote(envelope.SentAtUtc.ToString("o"))
                + ",\"sessionId\":" + Quote(envelope.SessionId)
                + ",\"payload\":" + (string.IsNullOrEmpty(envelope.PayloadJson) ? "{}" : envelope.PayloadJson)
                + "}";
        }

        public static ProtocolEnvelope DecodeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException("Empty envelope.");
            }

            return new ProtocolEnvelope(
                ReadInt(json, "protocolVersion"),
                ReadString(json, "messageId"),
                ReadString(json, "messageType"),
                DateTime.UtcNow,
                ReadString(json, "sessionId"),
                ExtractObject(json, "payload"));
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static int ReadInt(string json, string name)
        {
            var key = "\"" + name + "\":";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0)
            {
                throw new InvalidDataException("Missing " + name);
            }

            i += key.Length;
            var end = i;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            {
                end++;
            }

            return int.Parse(json.Substring(i, end - i));
        }

        private static string ReadString(string json, string name)
        {
            var key = "\"" + name + "\":\"";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0)
            {
                return string.Empty;
            }

            i += key.Length;
            var end = json.IndexOf('"', i);
            if (end < 0)
            {
                throw new InvalidDataException("Unterminated " + name);
            }

            return json.Substring(i, end - i);
        }

        private static string ExtractObject(string json, string name)
        {
            var key = "\"" + name + "\":";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0)
            {
                return "{}";
            }

            i += key.Length;
            if (i >= json.Length || json[i] != '{')
            {
                return "{}";
            }

            var depth = 0;
            for (var n = i; n < json.Length; n++)
            {
                if (json[n] == '{')
                {
                    depth++;
                }
                else if (json[n] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return json.Substring(i, n - i + 1);
                    }
                }
            }

            return "{}";
        }
    }
}
