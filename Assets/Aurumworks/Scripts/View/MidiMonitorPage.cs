using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The MIDI monitor (IN-3; the MIDI in plan's decision 8), a dialog opened from the MIDI page's
    /// Monitor. It shows the clock ("Following Han: 120.0 BPM, playing, beat 17", or "Aleatora: 120
    /// BPM"), the notes held on the inputs as names ("C4 E4 G4", sustained notes muted), and the
    /// last 16 messages, newest first, each with its port, its channel, what it is ("Note on E4 96",
    /// "CC 64 127", "Clock: Start") and its time in seconds. Clock ticks and Active Sensing aren't
    /// listed (MidiPlayback.DrainReceived leaves them out). Each frame it takes what came in; it is
    /// the only reader of that queue, so opening it shows what came since it was last open.
    /// </summary>
    public class MidiMonitorPage : MonoBehaviour
    {
        /// <summary>How many messages it lists.</summary>
        public const int Lines = 16;

        private const float LineSize = 20f;

        private readonly List<TimedMessage> _drained = new List<TimedMessage>();
        private readonly List<TimedMessage> _messages = new List<TimedMessage>(); // newest first, at most Lines
        private readonly List<HeldNotes.Held> _held = new List<HeldNotes.Held>();
        private MidiPlayback _midi;
        private TextMeshProUGUI _clockText;
        private TextMeshProUGUI _heldText;
        private TextMeshProUGUI _messagesText;
        private int _heldVersion = -1;
        private string _muted;

        /// <summary>The clock as shown.</summary>
        public string ClockLine { get; private set; } = string.Empty;

        /// <summary>The held notes as shown, without colour: "C4 E4 G4 (sustained)"; "None" with none.</summary>
        public string HeldLine { get; private set; } = string.Empty;

        /// <summary>The messages listed, newest first, as plain lines: "Han · ch 1 · Note on E4 96 · 12.345".</summary>
        public IReadOnlyList<string> MessageLines => _plain;

        private readonly List<string> _plain = new List<string>();

        /// <summary>Opens the monitor over whatever is open. Null without a MidiPlayback.</summary>
        public static MidiMonitorPage Open(MenuStack menus, MidiPlayback midi)
        {
            if (menus == null || midi == null)
            {
                return null;
            }

            TextMeshProUGUI clock = null;
            TextMeshProUGUI held = null;
            TextMeshProUGUI messages = null;
            float width = menus.DialogWidth;
            menus.DialogWidth = 0.5f;
            RectTransform panel;
            try
            {
                panel = menus.OpenDialog("Monitor", column =>
                {
                    UiTheme theme = UiKit.Theme;
                    UiKit.Title(column, "Monitor", 52f, theme.Accent, TextAlignmentOptions.Center);
                    UiKit.Label(column, "Clock");
                    clock = UiKit.Text(column, string.Empty, 24f);
                    UiKit.Label(column, "Held");
                    held = UiKit.Text(column, string.Empty, 24f);
                    UiKit.Label(column, "Messages");
                    messages = UiKit.Text(column, string.Empty, LineSize);
                    messages.richText = true;
                    var size = messages.gameObject.AddComponent<LayoutElement>();
                    size.preferredHeight = size.minHeight = Lines * LineSize * 1.35f;
                    UiKit.Spacer(column, 6f);
                    UiKit.Button(column, "Back", menus.Close, 56f);
                });
            }
            finally
            {
                menus.DialogWidth = width;
            }

            var page = panel.gameObject.AddComponent<MidiMonitorPage>();
            page._midi = midi;
            page._clockText = clock;
            page._heldText = held;
            page._messagesText = messages;
            page._muted = ColorUtility.ToHtmlStringRGBA(UiKit.Theme.Muted);
            page.Refresh();
            return page;
        }

        /// <summary>A message as the monitor lists it, without colour: its port, channel (none for a system message), what it is and its time.</summary>
        public static string Describe(in TimedMessage message)
        {
            int bytes = message.Message;
            string channel = MidiMessage.IsSystem(bytes) ? string.Empty : " · ch " + MidiMessage.Channel(bytes).ToString(CultureInfo.InvariantCulture);
            return $"{message.Port}{channel} · {MidiMessage.Describe(bytes)} · {Time(message.Time)}";
        }

        //----------

        private void Update()
        {
            Refresh();
        }

        // Takes what came in, then shows anything that changed.
        private void Refresh()
        {
            if (_midi == null)
            {
                return;
            }

            _drained.Clear();
            if (_midi.DrainReceived(_drained) > 0)
            {
                int from = Math.Max(0, _drained.Count - Lines); // oldest first: only the newest few can show
                for (int i = from; i < _drained.Count; i++)
                {
                    _messages.Insert(0, _drained[i]);
                }

                if (_messages.Count > Lines)
                {
                    _messages.RemoveRange(Lines, _messages.Count - Lines);
                }

                ShowMessages();
            }

            int version = _midi.Held.Version;
            if (version != _heldVersion)
            {
                _heldVersion = version;
                ShowHeld();
            }

            string clock = ClockText(_midi);
            if (clock != ClockLine)
            {
                ClockLine = clock;
                _clockText.text = clock;
            }
        }

        private void ShowMessages()
        {
            _plain.Clear();
            var text = new StringBuilder();
            foreach (TimedMessage message in _messages)
            {
                _plain.Add(Describe(message));
                int bytes = message.Message;
                string channel = MidiMessage.IsSystem(bytes) ? string.Empty : "ch " + MidiMessage.Channel(bytes).ToString(CultureInfo.InvariantCulture);
                text.Append("<noparse>").Append(message.Port).Append("</noparse>")
                    .Append("<pos=34%>").Append(channel)
                    .Append("<pos=45%>").Append(MidiMessage.Describe(bytes))
                    .Append("<pos=84%><color=#").Append(_muted).Append('>').Append(Time(message.Time)).Append("</color>\n");
            }

            _messagesText.text = text.ToString();
        }

        private void ShowHeld()
        {
            _held.Clear();
            _midi.Held.CopyTo(_held);
            if (_held.Count == 0)
            {
                HeldLine = "None";
                _heldText.text = $"<color=#{_muted}>None</color>";
                return;
            }

            var plain = new StringBuilder();
            var shown = new StringBuilder();
            foreach (HeldNotes.Held note in _held)
            {
                if (plain.Length > 0)
                {
                    plain.Append(' ');
                    shown.Append(' ');
                }

                plain.Append(note.ToString());
                string name = MidiMessage.NoteName(note.Note);
                shown.Append(note.IsSustained ? $"<color=#{_muted}>{name}</color>" : name);
            }

            HeldLine = plain.ToString();
            _heldText.text = shown.ToString();
        }

        /// <summary>
        /// The clock as the monitor shows it: "Following Han: 120.0 BPM, playing, beat 17" (beats
        /// counted from 1 at the song's start, as Reaper counts), "Following Han: no clock yet" before
        /// a tempo is measured, or "Aleatora: 120 BPM", with ", sending clock" or "(Han isn't open)".
        /// </summary>
        public static string ClockText(MidiPlayback midi)
        {
            string followed = midi.Followed;
            if (followed != null)
            {
                ClockFollower follower = midi.Follower;
                double tempo = follower.Tempo;
                if (tempo <= 0d)
                {
                    return $"Following {followed}: no clock yet";
                }

                long beat = Math.Max(0L, (long)Math.Floor(follower.BeatAt(midi.Now))) + 1;
                return string.Format(CultureInfo.InvariantCulture, "Following {0}: {1:0.0} BPM, {2}, beat {3}", followed, tempo,
                    follower.IsRunning ? "playing" : "stopped", beat);
            }

            string own = "Aleatora: " + midi.Bpm.ToString(CultureInfo.InvariantCulture) + " BPM";
            string chosen = midi.ClockName;
            if (chosen.Length > 0)
            {
                return own + $" ({chosen} isn't open)";
            }

            return midi.SendClock ? own + ", sending clock" : own;
        }

        private static string Time(double seconds) => seconds.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
