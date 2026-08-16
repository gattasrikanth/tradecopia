using System;
using System.IO;
using TradeCopia.Protocol;

namespace TradeCopia.Protocol.UnitTests
{
    public class ProtocolSessionTests
    {
        private static ProtocolEnvelope Envelope(string type, int version = 1, string payload = "{}")
        {
            return new ProtocolEnvelope(version, Guid.NewGuid().ToString("N"), type, DateTime.UtcNow, "s", payload);
        }

        [Fact]
        public void Snapshot_includes_live_trades_for_dashboard()
        {
            var session = new ProtocolSession();
            session.Handle(Envelope(ProtocolMessageTypes.Hello));
            session.ReplaceLiveActivity(
                new[]
                {
                    new LiveCopyRecord(
                        "t1",
                        "MNQ 09-26",
                        "Buy",
                        "Limit",
                        "Provider31|3",
                        "12",
                        1,
                        1,
                        "Filled",
                        "30190",
                        "",
                        DateTime.UtcNow,
                        new[] { new LiveFollowerRecord("Simulator|2", 1, 1, "Filled", "30190", "TC:abc") })
                },
                new[] { new LiveDivergenceRecord("ok", "none") });
            var snap = session.Handle(Envelope(ProtocolMessageTypes.RequestSnapshot));
            Assert.Contains("\"liveTrades\":", snap.Reply.PayloadJson);
            var parsed = LiveCopyRecord.ParseArray(snap.Reply.PayloadJson);
            Assert.Single(parsed);
            Assert.Equal("MNQ 09-26", parsed[0].Instrument);
            Assert.Single(parsed[0].Followers);
            Assert.Equal("Simulator|2", parsed[0].Followers[0].Account);
            Assert.Equal(1, parsed[0].Followers[0].Qty);
        }

        [Fact]
        public void Handshake_then_snapshot_reports_copying_disabled()
        {
            var session = new ProtocolSession();
            var hello = session.Handle(Envelope(ProtocolMessageTypes.Hello));
            Assert.True(hello.Accepted);
            Assert.Equal(ProtocolMessageTypes.EngineHello, hello.Reply.MessageType);
            Assert.Contains("Disabled", hello.Reply.PayloadJson);

            var snap = session.Handle(Envelope(ProtocolMessageTypes.RequestSnapshot));
            Assert.True(snap.Accepted);
            Assert.Contains("\"copyingEnabled\":false", snap.Reply.PayloadJson);
        }

        [Fact]
        public void ExecuteOrder_is_rejected_and_not_in_catalog()
        {
            Assert.True(ProtocolCatalog.IsForbiddenDiscretionaryOrder("ExecuteOrder"));
            Assert.False(ProtocolCatalog.IsAllowed("ExecuteOrder"));
            var session = new ProtocolSession();
            session.Handle(Envelope(ProtocolMessageTypes.Hello));
            var result = session.Handle(Envelope("ExecuteOrder"));
            Assert.False(result.Accepted);
            Assert.Equal("forbidden-execute-order", result.Reason);
        }

        [Fact]
        public void Incompatible_version_fails_closed()
        {
            var session = new ProtocolSession();
            var result = session.Handle(Envelope(ProtocolMessageTypes.Hello, version: 99));
            Assert.False(result.Accepted);
            Assert.Equal("incompatible-version", result.Reason);
        }

        [Fact]
        public void Malformed_short_frame_is_rejected()
        {
            Assert.Throws<InvalidDataException>(() => ProtocolFraming.Decode(new byte[] { 0, 0, 0 }));
        }

        [Fact]
        public void Messages_before_handshake_are_rejected()
        {
            var session = new ProtocolSession();
            var result = session.Handle(Envelope(ProtocolMessageTypes.RequestSnapshot));
            Assert.False(result.Accepted);
            Assert.Equal("handshake-required", result.Reason);
        }

        [Fact]
        public void Disconnect_then_reconnect_requires_new_handshake()
        {
            var session = new ProtocolSession();
            session.Handle(Envelope(ProtocolMessageTypes.Hello));
            var first = session.SessionId;
            session.Disconnect();
            var whileDown = session.Handle(Envelope(ProtocolMessageTypes.Heartbeat));
            Assert.False(whileDown.Accepted);
            session.Reconnect();
            Assert.NotEqual(first, session.SessionId);
            var after = session.Handle(Envelope(ProtocolMessageTypes.Heartbeat));
            Assert.False(after.Accepted);
            Assert.Equal("handshake-required", after.Reason);
        }

        [Fact]
        public void Unknown_type_is_rejected()
        {
            var session = new ProtocolSession();
            session.Handle(Envelope(ProtocolMessageTypes.Hello));
            var result = session.Handle(Envelope("NotARealMessage"));
            Assert.False(result.Accepted);
            Assert.Equal("unknown-message-type", result.Reason);
        }

        [Fact]
        public void Pause_and_disable_change_observable_snapshot()
        {
            var session = new ProtocolSession();
            session.Handle(Envelope(ProtocolMessageTypes.Hello));
            Assert.Equal("Disabled", session.EngineState);
            Assert.False(session.CopyingEnabled);

            var paused = session.Handle(Envelope(ProtocolMessageTypes.PauseNewEntries));
            Assert.True(paused.Accepted);
            Assert.Equal(ProtocolMessageTypes.EngineStateSnapshot, paused.Reply.MessageType);
            Assert.Equal("PausedNewEntries", session.EngineState);
            Assert.False(session.CopyingEnabled);
            Assert.Contains("\"engineState\":\"PausedNewEntries\"", paused.Reply.PayloadJson);
            Assert.Contains("\"copyingEnabled\":false", paused.Reply.PayloadJson);

            var snap = session.Handle(Envelope(ProtocolMessageTypes.RequestSnapshot));
            Assert.Contains("\"engineState\":\"PausedNewEntries\"", snap.Reply.PayloadJson);

            var disabled = session.Handle(Envelope(ProtocolMessageTypes.DisableGroup));
            Assert.Equal("Disabled", session.EngineState);
            Assert.Contains("\"engineState\":\"Disabled\"", disabled.Reply.PayloadJson);
        }

        [Fact]
        public void Resume_from_pause_enables_copying_in_snapshot()
        {
            var session = new ProtocolSession();
            session.Handle(Envelope(ProtocolMessageTypes.Hello));
            session.Handle(Envelope(ProtocolMessageTypes.PauseNewEntries));
            var resume = session.Handle(Envelope(ProtocolMessageTypes.ResumeNewEntries));
            Assert.True(resume.Accepted);
            Assert.True(session.CopyingEnabled);
            Assert.Equal("Enabled", session.EngineState);
            Assert.Contains("\"copyingEnabled\":true", resume.Reply.PayloadJson);

            session.Handle(Envelope(ProtocolMessageTypes.DisableGroup));
            var bad = session.Handle(Envelope(ProtocolMessageTypes.ResumeNewEntries));
            Assert.False(bad.Accepted);
            Assert.False(session.CopyingEnabled);
        }
    }
}
