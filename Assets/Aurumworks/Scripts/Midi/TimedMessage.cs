namespace Aurumworks.Aleatora
{
    /// <summary>
    /// A message as an input received it: what, when (seconds on the clock the input was given, which
    /// in the game is MidiPlayback's Stopwatch) and from which port.
    /// </summary>
    public readonly struct TimedMessage
    {
        /// <summary>The message, packed as MidiMessage packs it.</summary>
        public readonly int Message;

        /// <summary>When it arrived, in seconds on the input's clock.</summary>
        public readonly double Time;

        /// <summary>The name of the port it came from.</summary>
        public readonly string Port;

        public TimedMessage(int message, double time, string port)
        {
            Message = message;
            Time = time;
            Port = port;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Time:0.000} {Port}: {MidiMessage.DescribeWithChannel(Message)}";
    }
}
