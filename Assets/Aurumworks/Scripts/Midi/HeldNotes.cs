using System;
using System.Collections.Generic;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The keys held on the keyboards coming in (the MIDI in plan's decision 7), kept per input port
    /// and channel, every channel listened to: the keys down with their velocities, and the sustain
    /// pedal (CC 64, down from 64) kept apart. A key let go while the pedal is down stays held,
    /// sustained, until the pedal comes up; struck again, it is down again. A note on at velocity 0
    /// is a note off. All Notes Off (CC 123) lets go of every key on its channel (the pedal still
    /// holds them); All Sound Off (CC 120) ends everything on its channel, sustained notes too. Held
    /// means down or sustained. Nothing else is read, and nothing in Aleatora responds to it yet.
    /// Thread-safe: the clock thread feeds it while the main thread reads.
    /// </summary>
    public sealed class HeldNotes
    {
        /// <summary>One held note.</summary>
        public readonly struct Held
        {
            /// <summary>The port it came from.</summary>
            public readonly string Port;

            /// <summary>Its channel, 1–16.</summary>
            public readonly int Channel;

            /// <summary>The note, 0–127.</summary>
            public readonly int Note;

            /// <summary>The velocity it was struck with.</summary>
            public readonly int Velocity;

            /// <summary>Whether only the pedal holds it: the key is up.</summary>
            public readonly bool IsSustained;

            public Held(string port, int channel, int note, int velocity, bool isSustained)
            {
                Port = port;
                Channel = channel;
                Note = note;
                Velocity = velocity;
                IsSustained = isSustained;
            }

            /// <inheritdoc/>
            public override string ToString() => MidiMessage.NoteName(Note) + (IsSustained ? " (sustained)" : string.Empty);
        }

        // One port's channel: what each key is doing.
        private sealed class Keys
        {
            public readonly bool[] Down = new bool[MidiMessage.HighestData + 1];
            public readonly bool[] Sustained = new bool[MidiMessage.HighestData + 1];
            public readonly int[] Velocity = new int[MidiMessage.HighestData + 1];
            public bool Pedal;
        }

        private readonly object _lock = new object();
        private readonly Dictionary<(string port, int channel), Keys> _keys = new Dictionary<(string, int), Keys>();
        private int _version;

        /// <summary>Goes up by one with every change to what is held or to a pedal, so a reader can tell when to look again.</summary>
        public int Version
        {
            get
            {
                lock (_lock)
                {
                    return _version;
                }
            }
        }

        /// <summary>How many notes are held, on every port and channel.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    int count = 0;
                    foreach (Keys keys in _keys.Values)
                    {
                        for (int note = 0; note <= MidiMessage.HighestData; note++)
                        {
                            count += keys.Down[note] || keys.Sustained[note] ? 1 : 0;
                        }
                    }

                    return count;
                }
            }
        }

        /// <summary>Reads a message that came in: notes, the pedal, All Notes Off and All Sound Off; anything else is let by.</summary>
        public void Receive(in TimedMessage message)
        {
            Receive(message.Port, message.Message);
        }

        /// <summary>Reads a message from the port named: notes, the pedal, All Notes Off and All Sound Off; anything else is let by.</summary>
        public void Receive(string port, int message)
        {
            if (MidiMessage.IsSystem(message))
            {
                return;
            }

            int kind = MidiMessage.Kind(message);
            int data1 = MidiMessage.Data1(message);
            int data2 = MidiMessage.Data2(message);
            bool noteOn = MidiMessage.IsNoteOn(message);
            bool noteOff = MidiMessage.IsNoteOff(message);
            if (!noteOn && !noteOff && kind != MidiMessage.ControlChangeStatus)
            {
                return;
            }

            lock (_lock)
            {
                Keys keys = For(port ?? string.Empty, MidiMessage.Channel(message));
                if (noteOn)
                {
                    keys.Down[data1] = true;
                    keys.Sustained[data1] = false;
                    keys.Velocity[data1] = data2;
                }
                else if (noteOff)
                {
                    LetGo(keys, data1);
                }
                else if (data1 == MidiMessage.SustainController)
                {
                    keys.Pedal = data2 >= 64;
                    if (!keys.Pedal)
                    {
                        Array.Clear(keys.Sustained, 0, keys.Sustained.Length);
                    }
                }
                else if (data1 == MidiMessage.AllNotesOffController)
                {
                    for (int note = 0; note <= MidiMessage.HighestData; note++)
                    {
                        LetGo(keys, note);
                    }
                }
                else if (data1 == MidiMessage.AllSoundOffController)
                {
                    Array.Clear(keys.Down, 0, keys.Down.Length);
                    Array.Clear(keys.Sustained, 0, keys.Sustained.Length);
                }
                else
                {
                    return;
                }

                _version++;
            }
        }

        /// <summary>Whether the key is down.</summary>
        public bool IsDown(string port, int channel, int note)
        {
            lock (_lock)
            {
                return TryGet(port, channel, note, out Keys keys) && keys.Down[note];
            }
        }

        /// <summary>Whether the note is held: the key down, or the pedal holding it.</summary>
        public bool IsHeld(string port, int channel, int note)
        {
            lock (_lock)
            {
                return TryGet(port, channel, note, out Keys keys) && (keys.Down[note] || keys.Sustained[note]);
            }
        }

        /// <summary>Whether only the pedal holds the note: the key is up.</summary>
        public bool IsSustained(string port, int channel, int note)
        {
            lock (_lock)
            {
                return TryGet(port, channel, note, out Keys keys) && keys.Sustained[note] && !keys.Down[note];
            }
        }

        /// <summary>The velocity a held note was struck with, or 0 if it isn't held.</summary>
        public int Velocity(string port, int channel, int note)
        {
            lock (_lock)
            {
                return TryGet(port, channel, note, out Keys keys) && (keys.Down[note] || keys.Sustained[note]) ? keys.Velocity[note] : 0;
            }
        }

        /// <summary>Whether the sustain pedal is down on the port's channel.</summary>
        public bool IsPedalDown(string port, int channel)
        {
            lock (_lock)
            {
                return _keys.TryGetValue((port ?? string.Empty, channel), out Keys keys) && keys.Pedal;
            }
        }

        /// <summary>Puts the held notes into the list given (cleared first), lowest note first, then by port and channel.</summary>
        public void CopyTo(List<Held> into)
        {
            into.Clear();
            lock (_lock)
            {
                foreach (KeyValuePair<(string port, int channel), Keys> pair in _keys)
                {
                    Keys keys = pair.Value;
                    for (int note = 0; note <= MidiMessage.HighestData; note++)
                    {
                        if (keys.Down[note] || keys.Sustained[note])
                        {
                            into.Add(new Held(pair.Key.port, pair.Key.channel, note, keys.Velocity[note], !keys.Down[note]));
                        }
                    }
                }
            }

            into.Sort((a, b) =>
            {
                int order = a.Note.CompareTo(b.Note);
                order = order != 0 ? order : string.CompareOrdinal(a.Port, b.Port);
                return order != 0 ? order : a.Channel.CompareTo(b.Channel);
            });
        }

        /// <summary>Forgets everything from a port: its notes and its pedals (it closed).</summary>
        public void Clear(string port)
        {
            lock (_lock)
            {
                var gone = new List<(string, int)>();
                foreach ((string, int) key in _keys.Keys)
                {
                    if (key.Item1 == (port ?? string.Empty))
                    {
                        gone.Add(key);
                    }
                }

                foreach ((string, int) key in gone)
                {
                    _keys.Remove(key);
                }

                _version++;
            }
        }

        /// <summary>Forgets everything.</summary>
        public void Clear()
        {
            lock (_lock)
            {
                _keys.Clear();
                _version++;
            }
        }

        private static void LetGo(Keys keys, int note)
        {
            if (keys.Down[note])
            {
                keys.Down[note] = false;
                keys.Sustained[note] = keys.Pedal;
            }
        }

        private Keys For(string port, int channel)
        {
            if (!_keys.TryGetValue((port, channel), out Keys keys))
            {
                keys = new Keys();
                _keys.Add((port, channel), keys);
            }

            return keys;
        }

        private bool TryGet(string port, int channel, int note, out Keys keys)
        {
            keys = null;
            return note >= 0 && note <= MidiMessage.HighestData && _keys.TryGetValue((port ?? string.Empty, channel), out keys);
        }
    }
}
